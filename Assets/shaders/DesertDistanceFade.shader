Shader "Custom/DesertSolidFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.9, 0.8, 0.6, 1)
        _FogStart ("Fog Start Distance", Float) = 50
        _FogEnd ("Fog End Distance", Float) = 200
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // Vertex colors from LowPolyDesertChunk
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD0;
            };

            float4 _FogColor;
            float _FogStart;
            float _FogEnd;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Distance from camera to pixel
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);
                
                // Calculate Fog Factor (0 = No Fog, 1 = Full Fog)
                float fogFactor = saturate((dist - _FogStart) / (_FogEnd - _FogStart));
                
                // Mix Terrain Color with Fog Color
                // Lerp(Terrain, Fog, factor)
                fixed4 col = lerp(i.color, _FogColor, fogFactor);
                col.a = 1.0;
                
                return col;
            }
            ENDCG
        }
    }
}
