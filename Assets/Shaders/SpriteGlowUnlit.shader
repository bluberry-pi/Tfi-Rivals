Shader "Custom/URP/SpriteGlowUnlit"
{
    // A tiny unlit sprite shader for URP.
    //
    // The default Sprite shader can only MULTIPLY the sprite's existing
    // colors by a tint - so tinting with white does nothing (white is the
    // "do nothing" multiplier). This shader instead throws away the
    // sprite's original colors entirely and repaints it with _Color,
    // keeping only its silhouette shape (the texture's alpha channel).
    // _GlowIntensity lets you push the color above 1.0 (HDR) so it reads
    // as "glowing" rather than flat, and pairs well with URP Bloom if you
    // have it set up (see notes below).

    Properties
    {
        _MainTex        ("Sprite Texture", 2D) = "white" {}
        _Color          ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity  ("Glow Intensity (HDR boost)", Range(1, 10)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; // this carries SpriteRenderer.color, incl. alpha
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float _GlowIntensity;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Only the sprite's ALPHA (its shape) is used.
                float texAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                float3 glowRGB = _Color.rgb * _GlowIntensity;

                // IN.color.a is driven by SpriteRenderer.color.a at runtime -
                // this is how the fade-out script controls opacity.
                float alpha = texAlpha * _Color.a * IN.color.a;

                return float4(glowRGB, alpha);
            }
            ENDHLSL
        }
    }
}
