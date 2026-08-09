Shader "UI/GlowLogo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _GlowColor ("Glow Color", Color) = (0.5, 0.9, 1.0, 1.0)
        _GlowSize ("Glow Size", Range(0, 0.1)) = 0.02
        _GlowPower ("Glow Power", Range(0.1, 5)) = 2.0
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3

        // UI Mask用（お約束）
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "GlowLogo"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GlowColor;
                float  _GlowSize;
                float  _GlowPower;
                float  _GlowIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // 周辺8方向のアルファをサンプリングして平均化
            float SampleGlowAlpha(float2 uv)
            {
                float alpha = 0;
                float2 offsets[8] = {
                    float2( 1,  0), float2(-1,  0),
                    float2( 0,  1), float2( 0, -1),
                    float2( 0.707,  0.707), float2(-0.707,  0.707),
                    float2( 0.707, -0.707), float2(-0.707, -0.707)
                };

                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float2 sampleUV = uv + offsets[i] * _GlowSize;
                    alpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).a;
                }
                return alpha / 8.0;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // 元テクスチャ
                half4 mainCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // 周辺サンプリングでにじみを作る
                float glowAlpha = SampleGlowAlpha(IN.uv);
                glowAlpha = pow(saturate(glowAlpha), _GlowPower);

                // 脈打つ演出
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // グロウ部分（元テクスチャの外側だけ光らせる）
                float outerGlow = saturate(glowAlpha - mainCol.a);
                half3 glowRGB = _GlowColor.rgb * _GlowIntensity * pulse * outerGlow;

                // 合成：元色 + 外側のグロウ
                half3 finalRGB = mainCol.rgb + glowRGB;
                half finalAlpha = saturate(mainCol.a + outerGlow * _GlowColor.a);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}
