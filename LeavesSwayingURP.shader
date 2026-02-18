Shader "Custom/LeavesSwayingURP"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _WindStrength ("Wind Strength", Float) = 0.2
        _WindSpeed ("Wind Speed", Float) = 1.2
        _FlutterStrength ("Flutter Strength", Float) = 0.1
        _GustScale ("Gust Scale", Float) = 0.5
        _GustSpeed ("Gust Speed", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float _Smoothness;
            float _Metallic;
            float _WindStrength;
            float _WindSpeed;
            float _FlutterStrength;
            float _GustScale;
            float _GustSpeed;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv          : TEXCOORD0;
            float4 color       : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float2 uv          : TEXCOORD2;
            half3  bakedGI    : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings vert(Attributes v)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            float3 worldPosRef = TransformObjectToWorld(v.positionOS.xyz);
            float3 worldOrigin = TransformObjectToWorld(float3(0,0,0));
            
            float gust = sin(worldPosRef.x * _GustScale + _Time.y * _GustSpeed) *
                         cos(worldPosRef.z * _GustScale * 0.7 + _Time.y * _GustSpeed * 0.4);

            float gustMask = smoothstep(-0.4, 0.8, gust);

            float3 posOS = v.positionOS.xyz;
            
            float t = _Time.y * _WindSpeed;
            float strength = _WindStrength + (gustMask * _WindStrength * 2.0);
            float phase = dot(worldOrigin, float3(1, 0.1, 1));

            float3 sway;
            sway.x = sin(t + phase + worldPosRef.x * 0.4) * strength;
            sway.z = cos(t * 0.7 + phase + worldPosRef.z * 0.4) * strength;
            sway.y = sin(t * 3.5 + dot(worldPosRef.xz, float2(0.4, 0.4))) *
                     (_FlutterStrength + gustMask * _FlutterStrength);

            float mask = v.color.r;
            posOS += sway * mask;

            o.positionWS = TransformObjectToWorld(posOS);
            o.positionCS = TransformWorldToHClip(o.positionWS);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

            o.bakedGI = SampleSH(o.normalWS);

            return o;
        }
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbwd_shadows_full
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 albedo = tex.rgb * _BaseColor.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = normalize(i.normalWS);
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.bakedGI = i.bakedGI;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;
                surfaceData.emission = 0;
                surfaceData.alpha = tex.a * _BaseColor.a;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalGBuffer"
            Tags { "LightMode"="UniversalGBuffer" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _GBUFFER_NORMALS_OCT

            struct GBufferOutput
            {
                half4 GBuffer0 : SV_Target0;
                half4 GBuffer1 : SV_Target1;
                half4 GBuffer2 : SV_Target2;
                half4 GBuffer3 : SV_Target3;
            };

            GBufferOutput frag(Varyings i)
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 albedo = tex.rgb * _BaseColor.rgb;

                float3 normalWS = normalize(i.normalWS);

                #ifdef _GBUFFER_NORMALS_OCT
                    float2 oct = PackNormalOctQuadEncode(normalWS);
                    half3 packedNormal = half3(oct, 0);
                #else
                    half3 packedNormal = normalWS * 0.5h + 0.5h;
                #endif

                GBufferOutput o;
                o.GBuffer0 = half4(albedo, 1.0h);
                o.GBuffer1 = half4(_Metallic, 1.0h, 0.0h, _Smoothness);
                o.GBuffer2 = half4(packedNormal, 0.0h);
                o.GBuffer3 = half4(i.bakedGI * albedo, 1.0h);

                return o;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragShadow
            half4 fragShadow(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            half4 fragDepth(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}