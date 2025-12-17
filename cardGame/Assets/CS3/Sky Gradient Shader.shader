Shader "Custom/SkyDayNightFull"
{
    Properties
    {
        [Header(Day Colors)]
        _DayTopColor ("Day Top", Color) = (0.4, 0.42, 0.87, 1)
        _DayBottomColor ("Day Bottom", Color) = (0.28, 0.2, 0.83, 1)

        [Header(Night Colors)]
        _NightTopColor ("Night Top", Color) = (0, 0, 0, 1)
        _NightBottomColor ("Night Bottom", Color) = (0.1, 0.16, 0.33, 1)

        [Header(Horizon Glow)]
        _GlowColor ("Horizon Glow", Color) = (1, 0.8, 0.6, 1)
        _GlowPower ("Glow Power", Range(1, 10)) = 3.0

        [Header(Stars)]
        _StarTex ("Stars Texture (Tileable)", 2D) = "black" {}
        _StarTiling ("Star Tiling", Float) = 5.0
        _StarIntensity ("Star Intensity", Range(0, 2)) = 1.0

        [HideInInspector] _Transition ("Transition (0=Day, 1=Night)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
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
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // 属性声明
            fixed4 _DayTopColor, _DayBottomColor, _NightTopColor, _NightBottomColor, _GlowColor;
            float _GlowPower, _Transition, _StarTiling, _StarIntensity;
            sampler2D _StarTex;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 1. 基础梯度计算 (垂直方向 uv.y)
                fixed4 daySky = lerp(_DayBottomColor, _DayTopColor, i.uv.y);
                fixed4 nightSky = lerp(_NightBottomColor, _NightTopColor, i.uv.y);
                
                // 2. 昼夜插值
                fixed4 finalSky = lerp(daySky, nightSky, _Transition);

                // 3. 地平线光晕 (Horizon Glow)
                // uv.y 越低越亮，模拟地平线的大气散射
                float glowMask = pow(1.0 - i.uv.y, _GlowPower);
                fixed4 glow = _GlowColor * glowMask * (1.0 - _Transition * 0.8); // 夜间光晕减弱
                finalSky += glow;

                // 4. 星空层计算
                // 只有当 _Transition 较大时（进入黑夜）才显示
                float2 starUV = i.uv * _StarTiling;
                fixed4 starColor = tex2D(_StarTex, starUV);
                
                // 星星出现的阈值控制：在 Transition 0.6 之后开始浮现，1.0 时最亮
                float starVisibility = smoothstep(0.5, 1.0, _Transition);
                finalSky += starColor * _StarIntensity * starVisibility;

                return finalSky;
            }
            ENDCG
        }
    }
}