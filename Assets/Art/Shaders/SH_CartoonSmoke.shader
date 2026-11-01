Shader "BusJam/CartoonSmoke"
{
    Properties
    {
        _BaseColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.82, 0.86, 0.92, 1)
        _ToonThreshold ("Toon Threshold", Range(0, 1)) = 0.45
        _ToonSoftness ("Toon Softness", Range(0.01, 0.5)) = 0.12
        _RimColor ("Rim Highlight Color", Color) = (1, 1, 1, 0.35)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _OutlineColor ("Outline Color", Color) = (0.22, 0.26, 0.33, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.022
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+60"
            "IgnoreProjector" = "True"
            "PreviewType" = "Sphere"
        }

        Pass
        {
            Name "CartoonSmokeForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half _ToonThreshold;
                half _ToonSoftness;
                half4 _RimColor;
                half _RimPower;
                half4 _OutlineColor;
                half _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = (half3)TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = (half3)GetWorldSpaceNormalizeViewDir(positionWS);
                output.color = input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);

                Light mainLight = GetMainLight();
                half3 lightDir = normalize((half3)mainLight.direction + half3(0.0h, 0.35h, 0.0h));

                half halfLambert = dot(normalWS, lightDir) * 0.5h + 0.5h;
                half toonRamp = smoothstep(_ToonThreshold - _ToonSoftness, _ToonThreshold + _ToonSoftness, halfLambert);

                half3 cloudColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, toonRamp) * input.color.rgb;

                half oneMinusNdotV = saturate(1.0h - dot(normalWS, viewDirWS));
                half rim = (oneMinusNdotV * oneMinusNdotV) * _RimColor.a;
                cloudColor = lerp(cloudColor, _RimColor.rgb, rim);

                return half4(cloudColor, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "CartoonSmokeOutline"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertOutline
            #pragma fragment FragOutline
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half _ToonThreshold;
                half _ToonSoftness;
                half4 _RimColor;
                half _RimPower;
                half4 _OutlineColor;
                half _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings VertOutline(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float scaledWidth = (float)(_OutlineWidth * saturate(input.color.a));
                positionWS += normalWS * scaledWidth;

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 FragOutline(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
