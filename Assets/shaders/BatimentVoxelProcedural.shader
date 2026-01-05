Shader "Custom/BatimentVoxelProcedural"
{
    Properties
    {
        [MainColor] _BaseColor("Couleur Principale", Color) = (0.8, 0.4, 0.3, 1)
        [MainTexture] _MainTex ("Texture Albedo (Crepi)", 2D) = "white" {}
        _TextureScale ("Echelle Texture (Monde)", Float) = 0.2
        _ColorVariation ("Couleur Secondaire (Variation)", Color) = (0.7, 0.3, 0.2, 1)
        
        [Header(Reglages Usure)]
        _NoiseScale ("Echelle du Bruit", Float) = 1.5
        _NoiseThreshold ("Seuil d'Apparition (Eparpillement)", Range(0, 0.9)) = 0.5
        _NoiseSoftness ("Flou des Bords", Range(0.01, 1)) = 0.2
        _NoiseStrength ("Opacite des Taches", Range(0, 1)) = 0.8
        
        [Header(Micro Details)]
        _GrainScale ("Echelle Grain", Float) = 200.0
        _GrainStrength ("Force Grain substractif", Range(0, 1)) = 0.05

        [Header(Surface)]
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Force de la Normal Map", Float) = 1.0

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.1
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ColorVariation;
                float _NoiseScale;
                float _NoiseThreshold;
                float _NoiseSoftness;
                float _NoiseStrength;
                float _GrainScale;
                float _GrainStrength;
                float _BumpScale;
                float _Smoothness;
                float _Metallic;
                float4 _BumpMap_ST;
                float4 _MainTex_ST; 
                float _TextureScale; // 🆕 World Scale
            CBUFFER_END

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ... (Noise functions unchanged) ...
            float hash(float3 p) {
                p = frac(p * 0.3183099 + .1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 x) {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                                 lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                                 lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            float fbm(float3 x) {
                float v = 0.0;
                float a = 0.5;
                float3 shift = float3(100, 100, 100);
                for (int i = 0; i < 3; ++i) {
                    v += a * noise(x);
                    x = x * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            // 🆕 Triplanar Mapping Function
            half4 TriplanarSample(TEXTURE2D(tex), SAMPLER(samp), float3 posWD, float3 normalWD, float scale)
            {
                float3 blend = abs(normalWD);
                blend /= (blend.x + blend.y + blend.z);
                
                // UV Projection
                float2 uvX = posWD.zy * scale;
                float2 uvY = posWD.xz * scale;
                float2 uvZ = posWD.xy * scale;

                half4 colX = SAMPLE_TEXTURE2D(tex, samp, uvX);
                half4 colY = SAMPLE_TEXTURE2D(tex, samp, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(tex, samp, uvZ);

                return colX * blend.x + colY * blend.y + colZ * blend.z;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                // FBM Masking (Procedural Sand/Wear)
                float n = fbm(input.positionWS * _NoiseScale);
                float mask = smoothstep(_NoiseThreshold, _NoiseThreshold + _NoiseSoftness, n);
                half4 proceduralColor = lerp(_BaseColor, _ColorVariation, mask * _NoiseStrength);

                // 🆕 Triplanar Texture Sampling
                // Replaces standard UV sampling to support Voxel/Generated Meshes
                float scale = 0.2; // Hardcoded default or use _TextureScale
                if (_TextureScale > 0.0) scale = _TextureScale;
                
                half4 textureColor = TriplanarSample(_MainTex, sampler_MainTex, input.positionWS, normalWS, scale);
                
                // Blend: Tint procedural color with texture
                half4 baseAlbedo = proceduralColor * textureColor;

                // Grain (Subtractive)
                float g = hash(input.positionWS * _GrainScale);
                baseAlbedo.rgb -= (g * _GrainStrength * 0.5);

                // Normal & Lighting
                float4 tangentWS = input.tangentWS;
                float3 bitangentWS = cross(normalWS, tangentWS.xyz) * tangentWS.w;
                half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);
                float3x3 TBN = float3x3(tangentWS.xyz, bitangentWS, normalWS);
                
                // Note: Triplanar Normal mapping is expensive/complex, skipping for now
                // Keeping original UV-based normal map for micro-detail if UVs exist, 
                // otherwise it might look flat but acceptable for "plaster"
                float3 lightingNormal = normalize(mul(normalTS, TBN)); 

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lightColor = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half NdotL = saturate(dot(lightingNormal, mainLight.direction));
                half3 ambient = SampleSH(lightingNormal);

                half3 diffuse = baseAlbedo.rgb * (lightColor * NdotL + ambient);

                return half4(diffuse, 1.0);
            }
            ENDHLSL
        }
        Pass { Name "ShadowCaster" Tags { "LightMode" = "ShadowCaster" } ColorMask 0 HLSLPROGRAM #pragma vertex vert #pragma fragment frag #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; }; struct Varyings { float4 positionCS : SV_POSITION; }; Varyings vert(Attributes input) { Varyings output; VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz); output.positionCS = vertexInput.positionCS; return output; } half4 frag(Varyings input) : SV_Target { return 0; } ENDHLSL }
    }
}
