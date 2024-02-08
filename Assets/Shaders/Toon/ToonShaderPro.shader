Shader "Unlit/ToonShaderPro"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _mNormal ("Normal Texture", 2D) = "bump" { }
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            
            // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct appdata members worldNormal)
            #pragma exclude_renderers d3d11
            
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                float3 worldNormal; INTERNAL_DATA
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex: SV_POSITION;
                float3 worldNormal; INTERNAL_DATA
            };
            
            sampler2D _MainTex;
            sampler2D _mNormal;
            
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = v.worldNormal;
                o.uv = TRANSFORM_TEX(v.uv, _mNormal);
                UnpackNormal(tex2D(_mNormal, IN.uv_mNormal + IN.worldNormal)).xyz
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                float3 normal = UnpackNormal(tex2D(_mNormal, i.uv + i.worldNormal)).xyz;
                half4 shadowCoord = TransformWorldToShadowCoord(i.vertex.xyz);
                Light mainLight = GetMainLight(shadowCoord);
                float3 direction = mainLight.direction;
                float3 color = mainLight.color;
                half distanceAtten = mainLight.distanceAttenuation;
                half shadowAtten = mainLight.shadowAttenuation;
                
                
                
                
                // sample the texture
                fixed4 col = dot(normal, direction);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            
            ENDCG
            
        }
    }
}
