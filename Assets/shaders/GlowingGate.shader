Shader "Custom/GlowingGate"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.1, 0.1, 0.1, 1)
        _EmissionColor ("Emission Color", Color) = (1, 0.8, 0, 1)
        _EmissionPower ("Emission Power", Range(0, 10)) = 3.0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _NoiseScale ("Noise Scale", Float) = 20.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionPower;
                float _PulseSpeed;
                float _NoiseScale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Simple pseudo-random noise
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Base Dark Stone
                half3 color = _BaseColor.rgb;

                // 2. Procedural Pattern (Vertical Flow)
                float2 noiseUV = IN.positionWS.xz * 0.1 + float2(0, _Time.y * 0.2); // World space noise
                float pattern = noise(noiseUV * _NoiseScale);
                
                // 3. Pulse Breathing
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5; // 0 to 1
                
                // Combine for Emission
                // The pattern defines "cracks" or "runes" where light escapes
                float emissionMask = smoothstep(0.4, 0.6, pattern); 
                half3 emission = _EmissionColor.rgb * _EmissionPower * emissionMask * (0.8 + 0.2 * pulse);

                color += emission;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
