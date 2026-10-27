Shader "Qpic/Brand Logo Reveal"
{
    Properties
    {
        [PerRendererData] _MainTex("Logo", 2D) = "white" {}
        _WhiteKey("Black ink on white", Float) = 0
        _Reveal("Scan reveal", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; float _WhiteKey, _Reveal;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 sample=tex2D(_MainTex,i.uv);
                float ink=lerp(sample.a, (1-dot(sample.rgb,float3(0.299,0.587,0.114)))*sample.a, _WhiteKey);
                float coverage=saturate((_Reveal-i.uv.y)*180+1)*step(0.001,_Reveal);
                float edge=exp(-abs(i.uv.y-_Reveal)*130)*0.8*step(_Reveal,0.995);
                return fixed4(lerp(i.color.rgb,1,edge),ink*coverage*i.color.a);
            }
            ENDCG
        }
    }
}
