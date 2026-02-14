Shader "Custom/WorldSegmentHunger_Final_Colored_Fixed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Main Texture (Ground)", 2D) = "white" {}
        _MinNightBrightness ("Min Night Brightness", Range(0, 1)) = 0.2
        _LightIntensity ("Light Boost", Range(1, 10)) = 2.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 screenPos : TEXCOORD1; 
            };

            sampler2D _MainTex;
            sampler2D _GlobalLightMap;
            float _SkyTransition, _MinNightBrightness, _LightIntensity;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
            fixed4 texColor = tex2D(_MainTex, i.texcoord);
            float2 uv = i.screenPos.xy / i.screenPos.w;
            
            // 1. 采样 RT (这就是你的白色光圈图)
            fixed4 lightMap = tex2D(_GlobalLightMap, uv);

            // 2. 基础环境亮度 (白天 1.0, 深夜 _MinNightBrightness)
            float ambient = lerp(1.0, _MinNightBrightness, _SkyTransition);
            
            // 3. 计算灯光 (这里给个超大系数 5.0，如果亮了再调小)
            // 确保使用 lightMap.r 或者 lightMap.rgb，因为白色贴图的 R 通道就是 1
            float3 litArea = lightMap.rgb * 5.0 * _SkyTransition* 2.0;
            
            // 4. 重点：使用 + 或者 max
            // 如果 ambient 是 0.1，litArea 是 1.0，结果就是 1.1（亮过白天）
            float3 finalLight = ambient + litArea;

            texColor.rgb *= finalLight;
            texColor.rgb *= texColor.a; // 保持透明度正常
            return texColor;
        }
            ENDCG
        }
    }
}