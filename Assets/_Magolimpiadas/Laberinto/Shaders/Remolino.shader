// Remolino mágico para portales: el patrón gira en coordenadas polares alrededor de un centro,
// con un anillo brillante en el borde. Es opaco y sin iluminación: el color se ve igual con
// cualquier luz, y si la intensidad pasa de 1, el Bloom del Volumen Global le da halo.
Shader "Magolimpiadas/Remolino"
{
    Properties
    {
        _Color ("Color", Color) = (0.4, 0.8, 1, 1)
        [NoScaleOffset] _Patron ("Patrón", 2D) = "white" {}
        _Velocidad ("Velocidad de giro", Float) = 0.25
        _Repeticion ("Repetición (radio, vueltas)", Vector) = (1, 1, 0, 0)
        _Torsion ("Torsión hacia el centro", Range(-10, 10)) = 3
        _BrilloCentro ("Brillo del centro", Range(0, 3)) = 0.6
        _BrilloAnillo ("Brillo del anillo", Range(0, 10)) = 2
        _Intensidad ("Intensidad", Range(0, 8)) = 1.3
        _CentroUV ("Centro (UV)", Vector) = (0.5, 0.5, 0, 0)
        _EscalaRadio ("Escala del radio", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_Patron);
        SAMPLER(sampler_Patron);

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Velocidad;
            float4 _Repeticion;
            float _Torsion;
            float _BrilloCentro;
            float _BrilloAnillo;
            float _Intensidad;
            float4 _CentroUV;
            float _EscalaRadio;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float fog : TEXCOORD1;
        };

        Varyings Vert(Attributes entrada)
        {
            Varyings salida;
            salida.positionCS = TransformObjectToHClip(entrada.positionOS.xyz);
            salida.uv = entrada.uv;
            salida.fog = ComputeFogFactor(salida.positionCS.z);
            return salida;
        }
        ENDHLSL

        Pass
        {
            Name "Remolino"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            half4 Frag(Varyings entrada) : SV_Target
            {
                // Distancia al centro (0 en el centro, 1 en el borde) y ángulo en vueltas.
                float2 delta = (entrada.uv - _CentroUV.xy) * 2.0 * _EscalaRadio;
                float radio = length(delta);
                float angulo = atan2(delta.y, delta.x) / (2.0 * PI) + 0.5;
                angulo += _Torsion * (1.0 - saturate(radio)) * 0.15;

                // Dos capas del patrón girando a distinta velocidad.
                float tiempo = _Time.y * _Velocidad;
                float2 polar = float2(radio, angulo) * _Repeticion.xy;
                float patron = SAMPLE_TEXTURE2D(_Patron, sampler_Patron, polar + tiempo).r * 0.6
                             + SAMPLE_TEXTURE2D(_Patron, sampler_Patron, polar * float2(1.7, 1.0) - tiempo * 0.6).r * 0.4;

                float3 centro = _Color.rgb * (_BrilloCentro + patron);
                float3 anillo = _Color.rgb * pow(saturate(radio), 4.0) * _BrilloAnillo;
                float3 color = (centro + anillo) * _Intensidad;
                color = MixFog(color, entrada.fog);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragProfundidad
            half FragProfundidad(Varyings entrada) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
