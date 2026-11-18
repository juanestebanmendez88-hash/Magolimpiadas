// Sprite que puede pasarse a blanco y negro (lo usa el golpe de poder del ping pong).
// _PingPongGris es global: 0 = color normal, 1 = blanco y negro. Lo cambia NivelPingPong.
Shader "Magolimpiadas/SpriteGris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Entrada { float4 pos : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Salida { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _PingPongGris;

            Salida vert(Entrada i)
            {
                Salida o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag(Salida i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                half g = dot(c.rgb, half3(0.299, 0.587, 0.114)) * 0.85;
                c.rgb = lerp(c.rgb, half3(g, g, g), saturate(_PingPongGris));
                return c;
            }
            ENDHLSL
        }
    }
}
