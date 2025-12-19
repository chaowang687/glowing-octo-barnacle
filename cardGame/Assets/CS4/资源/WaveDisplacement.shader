Shader "Custom/ParallaxWaveShaderWithLighting"
{
    Properties
    {
        _MainTex ("海浪贴图", 2D) = "white" {}
        _WaveColor ("叠加颜色", Color) = (1,1,1,1)
        
        [Header(Wave Animation)]
        _WaveSpeed ("波动速度", Float) = 2.0
        _WaveCount ("圆环波浪总数", Float) = 10.0
        _WaveAmplitude ("波动振幅", Float) = 0.1
        
        [Header(UV Scroll)]
        _ScrollSpeed ("贴图滚动速度", Float) = 0.5
        
        [Header(Lighting)]
        _DayBrightness ("白天亮度", Range(0, 2)) = 1.0
        _NightBrightness ("夜间亮度", Range(0, 1)) = 0.2
        _SkyTransition ("天空过渡", Range(0, 1)) = 0.5
        _LightBoost ("灯光增强", Range(0, 10)) = 5.0
        
        [HideInInspector] _CurrentAngle ("当前角度", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _GlobalLightMap;
            float4 _MainTex_ST;
            float4 _WaveColor;
            float _WaveSpeed;
            float _WaveCount;
            float _WaveAmplitude;
            float _ScrollSpeed;
            float _CurrentAngle;
            float _DayBrightness;
            float _NightBrightness;
            float _SkyTransition;
            float _LightBoost;

            v2f vert (appdata v)
            {
                v2f o;
                
                float rad = _CurrentAngle * (3.1415926 / 180.0);
                float wave = sin(rad * _WaveCount + _Time.y * _WaveSpeed) * _WaveAmplitude;
                v.vertex.y += wave * v.uv.y;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.x += _Time.y * _ScrollSpeed;
                o.color = v.color * _WaveColor;
                o.screenPos = ComputeScreenPos(o.vertex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // 获取屏幕空间UV
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                fixed4 lightMap = tex2D(_GlobalLightMap, screenUV);
                
                // 新的光照计算：
                // 1. 基础环境光（白天到夜晚的过渡）
                float ambient = lerp(_DayBrightness, _NightBrightness, _SkyTransition);
                
                // 2. 灯光区域（只在有灯光的地方增强）
                // 使用指数衰减，让灯光区域更集中
                float3 lightIntensity = lightMap.rgb * lightMap.rgb; // 平方，让亮部更亮
                float3 litArea = lightIntensity * _LightBoost;
                
                // 3. 最终光照 = 环境光 + 灯光区域
                // 使用max确保不会低于环境光
                float3 finalLight = max(ambient, litArea);
                
                // 4. 可选：夜晚时全局降低饱和度或添加蓝色调
                if (_SkyTransition > 0.5) {
                    // 夜晚时添加蓝色调
                    float nightFactor = (_SkyTransition - 0.5) * 2.0; // 0到1
                    finalLight = lerp(finalLight, finalLight * float3(0.7, 0.8, 1.0), nightFactor * 0.3);
                }
                
                col.rgb *= finalLight;
                col.rgb *= col.a;
                
                return col;
            }
            ENDCG
        }
    }
}