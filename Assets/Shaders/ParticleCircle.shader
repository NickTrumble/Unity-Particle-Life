Shader "Custom/ParticleCircle"
{
    Properties
    {
        //public colour to let you pick the circle colours
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        //queue Transparent means draw after solid shapes so circles arent at the back
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        //a single execution from the gpu
        Pass
        {
            
            Blend SrcAlpha OneMinusSrcAlpha// aka circle is bright background is Transparent    
            ZWrite Off // only needed if z coordinates were involved
            Cull Off // aka dont stop drawing the other side of the shape since could lead to issues

            //start the gpu program stuff here
            CGPROGRAM
            #pragma vertex vert // tells the shader vert is the vertex function
            #pragma fragment frag // tells the shader frag is the fragment function
            #pragma multi_compile_instancing //allows for gpu instancing

            #include "UnityCG.cginc"

            //raw mesh input
            struct appdata
            {
                float4 vertex : POSITION;//object space of the vertex
                float2 uv : TEXCOORD0;//where is it in the texture
                UNITY_VERTEX_INPUT_INSTANCE_ID // instance id
            };

            struct v2f
            {
                float4 vertex : SV_POSITION; // screen space coordinates
                float2 uv : TEXCOORD0; // the uv coordinates
                UNITY_VERTEX_INPUT_INSTANCE_ID // instance id
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            //converts teh local space coords to screen space
            //keeps the uv coordinates the same tho (corners)
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //runs once per pixel in the quad

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i)
                // change centre from 0.5, 0.5 to 0, 0
                float2 p = i.uv * 2.0 - 1.0;

                //dot(p, p) = px * px + py * py
                float dist = dot(p, p);

                // cut out circle if distance is bigger than radius of square (1)
                if (dist > 1.0)
                    discard;

                return UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
            }
            ENDCG
        }
    }
}