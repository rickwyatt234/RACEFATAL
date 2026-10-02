Shader "Hidden/RaceFatal/TeamPaintComposite"
{
    Properties { _MainTex("Base",2D)="white"{} _PaintMask("Paint",2D)="black"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _PaintMask;
            float4 _Primary, _Secondary;
            float _AccentStart, _AccentWidth;
            float4 frag(v2f_img i) : SV_Target
            {
                float4 source=tex2D(_MainTex,i.uv);
                float mask=tex2D(_PaintMask,i.uv).r;
                float stripe=step(_AccentStart,i.uv.x)*(1-step(_AccentStart+_AccentWidth,i.uv.x));
                float3 paint=lerp(_Primary.rgb,_Secondary.rgb,stripe);
                // Retain painted surface shading while replacing its hue.
                float shade=max(source.r,max(source.g,source.b));
                return float4(lerp(source.rgb,paint*shade,mask),source.a);
            }
            ENDCG
        }
    }
}
