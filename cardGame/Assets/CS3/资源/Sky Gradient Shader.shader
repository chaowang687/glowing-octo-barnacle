Shader "Custom/SkyDayNightRadial_Fixed"
{
    Properties
    {
        [Header(Day Colors)]
        _DayTopColor ("Day Top (Outer)", Color) = (0.4, 0.42, 0.87, 1)
        _DayBottomColor ("Day Bottom (Inner)", Color) = (0.28, 0.2, 0.83, 1)

        [Header(Night Colors)]
        _NightTopColor ("Night Top (Outer)", Color) = (0, 0, 0, 1)
        _NightBottomColor ("Night Bottom (Inner)", Color) = (0.1, 0.16, 0.33, 1)

        [Header(Radial Settings)]
        _Center ("Gradient Center (UV Space)", Vector) = (0.5, -0.5, 0, 0) 
        _RadiusScale ("Radius Scale", Range(0.1, 5)) = 1.0 

        [Header(Horizon Glow)]
        _GlowColor ("Horizon Glow Color", Color) = (1, 1, 1, 1) // 建议设为白色或淡蓝色
        _GlowPower ("Glow Power (Sharpness)", Range(1, 20)) = 5.0
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.5

        [Header(Stars)]
        _StarTex ("Stars Texture", 2D) = "black" {}
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

            fixed4 _DayTopColor, _DayBottomColor, _NightTopColor, _NightBottomColor, _GlowColor;
            float _GlowPower, _GlowIntensity, _Transition, _StarTiling, _StarIntensity, _RadiusScale;
            float4 _Center;
            sampler2D _StarTex;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 1. 径向距离计算
                // 计算 UV 空间中点到圆心的距离
                float dist = distance(i.uv, _Center.xy);
                // 归一化因子
                float radialFactor = saturate(dist / _RadiusScale);

                // 2. 基础天空颜色计算
                fixed4 daySky = lerp(_DayBottomColor, _DayTopColor, radialFactor);
                fixed4 nightSky = lerp(_NightBottomColor, _NightTopColor, radialFactor);
                fixed4 finalSky = lerp(daySky, nightSky, _Transition);

                // 3. 【修复发黄的关键】插值光晕逻辑
                // 使用 pow 控制光晕的衰减，saturate 确保数值不溢出
                // 我们不再用 +=，而是用 lerp，让地平线颜色平滑地混合进来
                float glowMask = pow(1.0 - radialFactor, _GlowPower);
                
                // 只有在白天或黎明时光晕明显，深夜光晕自动减弱
                float currentGlowEffect = glowMask * _GlowIntensity * (1.0 - _Transition * 0.7);
                finalSky = lerp(finalSky, _GlowColor, currentGlowEffect);

                // 4. 星空层
                // 星星只出现在远离圆心的“高空”区域 (radialFactor 较大处)
                float2 starUV = i.uv * _StarTiling;
                fixed4 starColor = tex2D(_StarTex, starUV);
                
                float starVisibility = smoothstep(0.4, 1.0, _Transition);
                // 让星星只在天空上方出现，地平线附近不出现，更符合自然
                float starMask = smoothstep(0.2, 0.7, radialFactor); 
                finalSky += starColor * _StarIntensity * starVisibility * starMask;

                return finalSky;
            }
            ENDCG
        }
    }
}