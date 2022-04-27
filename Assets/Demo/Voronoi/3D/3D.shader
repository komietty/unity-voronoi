Shader "Unlit/Drawer3D"
{
    Properties
    {
        _EdgeCol ("Edge", Color) = (0, 0, 0, 1)
        _Col01 ("01", Color) = (0, 0, 0, 1)
        _Col02 ("02", Color) = (0, 0, 0, 1)
        _Col03 ("03", Color) = (0, 0, 0, 1)
        _Col04 ("04", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off

		CGINCLUDE
            #include "UnityCG.cginc"
			float4 _EdgeCol;
			float4 _Col01;
			float4 _Col02;
			float4 _Col03;
			float4 _Col04;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
		ENDCG

		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag (v2f i) : SV_Target {
                return _EdgeCol;
            }
            ENDCG
        }
		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag (v2f i) : SV_Target {
				return _Col01;
            }
            ENDCG
        }
		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag (v2f i) : SV_Target {
				return _Col02;
            }
            ENDCG
        }
		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag (v2f i) : SV_Target {
				return _Col03;
            }
            ENDCG
        }
		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag (v2f i) : SV_Target {
				return _Col04;
            }
            ENDCG
        }
    }
}
