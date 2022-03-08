Shader "URP/Post/DOF_BokehBlur"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}

        //散景模糊
        _BlurSize ("模糊强度", Float) = 1.0
        _Iteration ("迭代次数", int) = 3
        _DownSample ("像素大小", int) = 2

        //景深
        _Distance ("Distance", Float) = 0.0
        _LensCoeff ("LensCoeff", Float) = 1.0
        _RcpMaxCoC ("RcpMaxCoC", Float) = 1.0
    }

    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)

        half4 _MainTex_TexelSize;

        float _BlurSize;
        float _Iteration;
        float _DownSample;

        float _Distance;
        float _LensCoeff;
        half _RcpMaxCoC;


        CBUFFER_END

        // Texture
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        // DepthTexture
        TEXTURE2D_X_FLOAT(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);


        struct VertexInput //输入结构
        {
            float4 vertex : POSITION;
            float2 uv0 : TEXCOORD0;
        };

        struct VertexOutput //输出结构
        {
            float4 pos : SV_POSITION;
            float2 uv0 : TEXCOORD0;
        };


        //顶点shader
        VertexOutput VertDefault(VertexInput v)
        {
            VertexOutput o;
            o.pos = TransformObjectToHClip(v.vertex);
            o.uv0 = v.uv0;
            return o;
        }


        //散景模糊
        half4 BokehBlur(VertexOutput i)
        {
            //预计算旋转
            float c = cos(2.39996323f);
            float s = sin(2.39996323f);
            half4 _GoldenRot = half4(c, s, -s, c);

            half2x2 rot = half2x2(_GoldenRot);
            half4 accumulator = 0.0; //累加器
            half4 divisor = 0.0; //因子

            half r = 1.0;
            half2 angle = half2(0.0, _BlurSize);

            for (int j = 0; j < _Iteration; j++)
            {
                r += 1.0 / r; //每次 + r分之一 1.1
                angle = mul(rot, angle);
                half4 bokeh = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(i.uv0 + (r - 1.0) * angle));
                accumulator += bokeh * bokeh;
                divisor += bokeh;
            }
            return half4(accumulator / divisor);
        }

        //像素shader
        half4 DOF_BokehBlur(VertexOutput i) : SV_Target
        {
            float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv0).r;
            float linearDepth = Linear01Depth(depth, _ZBufferParams);
            half coc = (linearDepth - _Distance) * _LensCoeff / max(depth, 1e-4);
            coc = saturate(coc * 0.5 * _RcpMaxCoC + 0.5);
            half4 finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0);
            finalColor.rgb = lerp(finalColor.rgb, BokehBlur(i).rgb, coc);

            return finalColor;
        }
        ENDHLSL

        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VertDefault
            #pragma fragment DOF_BokehBlur
            ENDHLSL
        }

    }
}