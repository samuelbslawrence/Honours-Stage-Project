Shader "Custom/FingerprintReveal"
{
    Properties
    {
        _MainTex ("Fingerprint Texture", 2D) = "white" {}
        _RevealMask ("Reveal Mask", 2D) = "black" {}
        _Color ("Tint Color", Color) = (0.3, 0.3, 0.3, 1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _RevealMask;
        fixed4 _Color;
        half _Smoothness;
        half _Metallic;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_RevealMask;
        };
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample the fingerprint texture
            fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
            
            // Sample the reveal mask (white = revealed, black = hidden)
            fixed4 revealMask = tex2D(_RevealMask, IN.uv_RevealMask);
            
            // Apply color tint (darker for realistic fingerprint appearance)
            fixed4 finalColor = mainTex * _Color;
            
            // Use reveal mask to control visibility
            // Only show fingerprint where mask is white (revealed areas)
            finalColor.a = mainTex.a * revealMask.r;
            
            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
            o.Smoothness = _Smoothness;
            o.Metallic = _Metallic;
        }
        ENDCG
    }
    
    FallBack "Transparent/Cutout/Diffuse"
}