Shader "Custom/LeavesSwaying"
{
    Properties
    {
        _MainTex ("Base Map", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _WindStrength ("Wind Strength", Float) = 0.2
        _WindSpeed ("Wind Speed", Float) = 1.2
        _FlutterStrength ("Flutter Strength", Float) = 0.1
        _GustScale ("Gust Scale", Float) = 0.5
        _GustSpeed ("Gust Speed", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _WindStrength;
        float _WindSpeed;
        float _FlutterStrength;
        float _GustScale;
        float _GustSpeed;

        void vert (inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            
            float gust = sin(worldPos.x * _GustScale + _Time.y * _GustSpeed) * cos(worldPos.z * _GustScale * 0.7 + _Time.y * _GustSpeed * 0.4);

            float gustMask = smoothstep(-0.4, 0.8, gust);
            float t = _Time.y * _WindSpeed;
            float strength = _WindStrength + (gustMask * _WindStrength * 2.0);
            float phase = dot(worldOrigin, float3(1, 0.1, 1));

            float3 sway;
            sway.x = sin(t + phase + worldPos.x * 0.4) * strength;
            sway.z = cos(t * 0.7 + phase + worldPos.z * 0.4) * strength;
            sway.y = sin(t * 3.5 + dot(worldPos.xz, float2(0.4, 0.4))) * (_FlutterStrength + gustMask * _FlutterStrength);

            v.vertex.xyz += sway;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}