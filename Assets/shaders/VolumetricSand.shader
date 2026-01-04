Shader "Custom/VolumetricSand"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.76, 0.69, 0.5, 1)
        _Density ("Density", Range(0, 5)) = 1.0
        _StepSize ("Step Size", Range(0.01, 1)) = 0.05
        _Absorption ("Light Absorption", Range(0, 5)) = 0.5
        _NoiseScale ("Noise Scale", Float) = 0.1
        _Speed ("Speed", Vector) = (1, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        ZWrite Off
        Cull Off
        ZTest Always // Render BEHIND and IN FRONT of everything, we handle depth manually
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0; // Screen UV (xy/w)
                float3 objPos : TEXCOORD1;
                float3 rayDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Density;
            float _StepSize;
            float _Absorption;
            float _NoiseScale;
            float4 _WindOffset;
            
            // Depth Texture for Soft Particles / Occlusion
            sampler2D _CameraDepthTexture;

            // Pseudo-random
            float rand(float3 co)
            {
                return frac(sin(dot(co, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objPos = v.vertex.xyz; 
                
                // Calculate View Direction in Object Space
                // Transform Camera Position to Object Space
                float3 camObjPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                
                o.rayDir = v.vertex.xyz - camObjPos;
                
                // Pass GrabPos for Depth processing
                o.uv = ComputeScreenPos(o.vertex);
                
                return o;
            }

            // AABB Intersection (Axis Aligned Bounding Box)
            // Box is -0.5 to 0.5 in Object Space
            float2 RayBoxIntersection(float3 rayOrigin, float3 rayDir)
            {
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3(0.5, 0.5, 0.5);
                
                float3 tMin = (boxMin - rayOrigin) / rayDir;
                float3 tMax = (boxMax - rayOrigin) / rayDir;
                
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                
                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);
                
                return float2(tNear, tFar);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Reconstruct Ray in Object Space
                float3 camObjPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 rayDir = normalize(i.rayDir);
                
                // Calculate Intersection
                float2 t = RayBoxIntersection(camObjPos, rayDir);
                float tNear = t.x;
                float tFar = t.y;

                if(tNear > tFar || tFar < 0.0) return fixed4(0,0,0,0);

                // --- DEPTH TEST ---
                // 1. Get Scene Depth (Linear Eye Depth)
                float2 screenUV = i.uv.xy / i.uv.w;
                float rawDepth = tex2D(_CameraDepthTexture, screenUV).r;
                float sceneDepth = LinearEyeDepth(rawDepth);
                
                // 2. Convert Scene Depth to Object Space Distance
                // We assume uniform scaling for simplicity, or average scale
                float3 scale = float3(
                    length(unity_ObjectToWorld[0].xyz),
                    length(unity_ObjectToWorld[1].xyz),
                    length(unity_ObjectToWorld[2].xyz)
                );
                float avgScale = (scale.x + scale.y + scale.z) / 3.0;
                
                // Distance to geometry in Object Space units
                // Note: LinearEyeDepth is distance along Forward axis, not Ray axis.
                // We need distance along Ray. 
                // RayDist = EyeDepth / dot(ViewDir, CameraForward)
                // Accessing Camera vectors is tricky here without passing them.
                // Approximation: sceneDepth is "close enough" to ray distance for narrow FOV, 
                // but for wide FOV it distorts. 
                // Better: float3 viewPos = i.rayDir * t... no.
                
                // Simple approx: Divide by scale. 
                float sceneDistObj = sceneDepth / avgScale;
                
                // Clamp Ray End to Scene Geometry
                // If scene is closer than tNear (we are heavily occluded), return clear.
                if (sceneDistObj < tNear) return fixed4(0,0,0,0);
                
                // Clamp tFar to scene geometry
                tFar = min(tFar, sceneDistObj);
                
                // Recalculate if still valid
                if(tNear > tFar) return fixed4(0,0,0,0);
                // ------------------
                
                // Clamp start to 0 if we are inside the box (tNear < 0)
                tNear = max(tNear, 0.0);
                
                // Marching Setup
                float distTraveled = 0.0;
                float totalDist = tFar - tNear;
                float3 currentPos = camObjPos + rayDir * tNear;
                
                float accumDensity = 0.0;
                float transmittance = 1.0;
                
                // Dither
                float dither = rand(float3(i.objPos.xy * 10.0, 0));
                currentPos += rayDir * _StepSize * dither;
                
                // March Loop
                [loop]
                for(int j=0; j<64; j++)
                {
                    if(accumDensity >= 1.0 || distTraveled >= totalDist) break;

                    // Height Falloff removed to cover the sky
                    // float heightFactor = smoothstep(0.5, -0.4, currentPos.y); 
                    float heightFactor = 1.0;
                    
                    if (heightFactor > 0.01)
                    {
                        // Sample Noise
                        // Use _WindOffset driven by script
                        float3 noisePos = currentPos * _NoiseScale + _WindOffset.xyz;
                        
                        float n1 = tex2D(_MainTex, noisePos.xz).r;
                        float n2 = tex2D(_MainTex, noisePos.xy * 0.5).r; 
                        float noise = (n1 + n2) * 0.5;
                        
                        // Thresholding
                        float localDensity = max(0, noise - 0.6) * _Density * heightFactor;
                        
                        if(localDensity > 0.001)
                        {
                            float stepAbsorb = exp(-localDensity * _StepSize * 10 * _Absorption); 
                            // Note: Scaled up absorption because StepSize in Object Space is small relative to World
                            // Wait, StepSize depends on Object Scale?
                            // If we scale the object by 1000, unity_ObjectToWorld scales it.
                            // Raymarching in Object Space means StepSize 0.05 is 5% of the cube size.
                            // That's acceptable.
                            
                            float stepDensity = (1.0 - stepAbsorb);
                            accumDensity += stepDensity * transmittance;
                            transmittance *= stepAbsorb;
                        }
                    }
                    
                    currentPos += rayDir * _StepSize;
                    distTraveled += _StepSize;
                }
                
                return fixed4(_Color.rgb, accumDensity);
            }
            ENDCG
        }
    }
}
