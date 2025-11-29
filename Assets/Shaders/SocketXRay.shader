Shader "Custom/SocketXRay"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,0.9,0,0.5) // Amarillo por defecto
    }
    SubShader
    {
        // Se dibuja en la cola "Overlay" (encima de todo) y es transparente
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }
        
        // CRÍTICO: Esto apaga la prueba de profundidad. 
        // Dibuja siempre, aunque haya muros delante.
        ZTest Always 
        
        // No escribe en el buffer de profundidad y permite transparencia
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}