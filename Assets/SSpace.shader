Shader "EdShaders/ScreenWorldSpace"
{
    Properties
    {
        _ScreenSpaceTexture("ScreenSpaceTexture", 2D) = "white" {}
        _TexScale("Tex Scale", Float) = 0
        [HideInInspector] __dirty( "", Int ) = 1
    }
 
    SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha
       
        CGPROGRAM
        #include "UnityCG.cginc"
        #include "UnityShaderVariables.cginc"
        #pragma target 3.0
        #pragma surface surf Standard keepalpha addshadow fullforwardshadows
        struct Input
        {
            float3 worldPos;
        };
 
        uniform sampler2D _ScreenSpaceTexture;
        uniform float _TexScale;
 
        void surf( Input i , inout SurfaceOutputStandard o )
        {
            float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
            float4 unityObjectToClipPos164 = UnityObjectToClipPos( ase_vertex3Pos );
            float4 computeScreenPos162 = ComputeScreenPos( unityObjectToClipPos164 );
            float4 unityObjectToClipPos170 = UnityObjectToClipPos( float3(0,0,0) );
            float4 computeScreenPos171 = ComputeScreenPos( unityObjectToClipPos170 );
            float4 transform182 = mul(unity_ObjectToWorld,float4( 0,0,0,1 ));
            o.Emission = tex2D( _ScreenSpaceTexture, ( ( ( ( computeScreenPos162 / (computeScreenPos162).w ) * _TexScale ) - ( ( computeScreenPos171 / (computeScreenPos171).w ) * _TexScale ) ) * distance( ( float4( _WorldSpaceCameraPos , 0.0 ) - transform182 ) , float4( 0,0,0,0 ) ) ).xy ).rgb;
            o.Alpha = 1;
        }
 
        ENDCG
    }
    Fallback "Diffuse"
 
}