Shader "Custom/InteractableOutline"
{
    Properties
    {
        [HDR] _FresnelColor ("Fresnel Color", Color) = (0.5, 0.9, 1.0, 1.0)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 20)) = 5.0
        _RimBoost ("Rim Boost (縁ブースト)", Range(0, 10)) = 3.0

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.5

        _FlickerSpeed ("Flicker Speed", Range(0, 30)) = 8.0
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.15

        _InnerGlow ("Inner Glow (全体発光)", Range(0, 2)) = 0.3

        _MainTex ("Base Texture (アルファ形状に合わせる用)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+100"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "FresnelGlow"
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FresnelColor;
                float  _FresnelPower;
                float  _FresnelIntensity;
                float  _RimBoost;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _FlickerSpeed;
                float  _FlickerAmount;
                float  _InnerGlow;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 疑似ランダム（ちらつき用）
            float hash(float n) { return frac(sin(n) * 43758.5453); }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);
                float3 normalWS = normalize(IN.normalWS);

                // Fresnel: 縁ほど1
                float ndotv = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower);

                // 縁ブースト（Fresnelの上限を突き抜けさせる）
                fresnel = fresnel * (1.0 + _RimBoost);

                // 脈打ち（ゆっくり大きく）
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // ちらつき（速く小さく、ランダム）
                float flickerNoise = hash(floor(_Time.y * _FlickerSpeed));
                float flicker = 1.0 - _FlickerAmount + flickerNoise * _FlickerAmount * 2.0;

                // 全体発光（縁だけじゃなく全面もうっすら光らせる）
                float glow = fresnel + _InnerGlow;

                // 最終カラー
                half3 col = _FresnelColor.rgb * _FresnelIntensity * glow * pulse * flicker;
                // 元のテクスチャの透明部分（紙の破れた縁の外側など）にはグローを出さない
                half texAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                half alpha = saturate(glow) * _FresnelColor.a * texAlpha;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
