Shader "Custom/ParallaxWaveShader"
{
    Properties
    {
        _MainTex ("海浪贴图", 2D) = "white" {}
        _WaveColor ("叠加颜色", Color) = (1,1,1,1)
        
        [Header(Wave Animation)]
        _WaveSpeed ("波动速度", Float) = 2.0
        _WaveCount ("圆环波浪总数", Float) = 10.0 // 必须为整数，确保 360 度闭合
        _WaveAmplitude ("波动振幅", Float) = 0.1
        
        [Header(UV Scroll)]
        _ScrollSpeed ("贴图滚动速度", Float) = 0.5
        
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _WaveColor;
            float _WaveSpeed;
            float _WaveCount;
            float _WaveAmplitude;
            float _ScrollSpeed;
            float _CurrentAngle;

            v2f vert (appdata v)
            {
                v2f o;
                
                // 核心同步公式：将角度转换为弧度
                // 使波动基于地块在圆环上的绝对位置而非本地坐标
                float rad = _CurrentAngle * (3.1415926 / 180.0);
                
                // 计算波浪：rad * _WaveCount 确保了在 360 度内波浪是连续的
                float wave = sin(rad * _WaveCount + _Time.y * _WaveSpeed) * _WaveAmplitude;
                
                // 只让顶部受影响，底部锚定
                v.vertex.y += wave * v.uv.y;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.x += _Time.y * _ScrollSpeed;
                o.color = v.color * _WaveColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                return col;
            }
            ENDCG
        }
    }
}