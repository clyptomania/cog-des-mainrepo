Shader "Custom/GridShader" {
    Properties{
        _BgColor("Background Color", Color) = (.5, .5, .5, 1)
        _MaskColor("Mask Color", Color) = (0, 0, 0, 1)
        
        _nRows("Number of rows", int) = 0
        _nCols("Number of columns", int) = 0
        
        _Radius ("Radius", Range(0,.5)) = .25
        
        _QuadRatio("Y/X quad ratio (FoV ratio)", float) = .1
        _rangeX("Horizontal range", vector) = (0, 1, 0, 0)
        _rangeY("Vertical range", vector) = (0, 1, 0, 0)
        
        // Ring 0 is located at the bottom left
        _TargetCircle("Target ring", int) = 0
        }

        SubShader{

        Tags { "Queue" = "Transparent" }
        Pass{

            // No culling or depth
            Cull Off ZWrite Off ZTest Always
            
//            half _MainTex_ST;
//            UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST)

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 _BgColor;
            float4 _MaskColor;
            
            half _Radius;
            
            uint _nRows;
            uint _nCols;
            
            float _QuadRatio;
            float4 _rangeX;
            float4 _rangeY;
            
            uint _TargetCircle;
            
            bool ring (float2 pos, float2 center, float radius){
                // Get position relative to circle center position
                float2 relPos = float2(center.x, center.y/_QuadRatio) - float2(pos.x, pos.y/_QuadRatio);
                
                // Euclidean distance to circle center
                float dist = length(relPos);

                // Filled circle
                return !(dist > radius);
            }

            fixed4 frag (v2f i) : SV_Target{
            
                // Is this pixel part of the circles?
                bool rings = false;
                
                for (uint irow = 0; irow < _nRows; irow++){
                    for (uint icol = 0; icol < _nCols; icol++){
                        // Circl pos: 0 is bottom left, increments left to right 
                        uint icirl = icol * _nRows + irow;
                        float2 circle_pos = float2(
                                float(irow)/_nRows + 1./_nRows/2, // X&Y pos
                                float(icol)/_nCols + 1./_nCols/2  // add X&Yspacing 
                            );

                        circle_pos.x = (_rangeX.y - _rangeX.x) * circle_pos.x + _rangeX.x;
                        circle_pos.y = (_rangeY.y - _rangeY.x) * circle_pos.y + _rangeY.x;
                        // Resize to specified range so that it isn't too far away for the HMD

                        // If target: (-1 means all or none)
                        
                        if ( _TargetCircle == icirl){
                            // Concentric rings
                            rings = rings | ring(i.uv.xy, circle_pos, _Radius*1.4);
                            rings = rings & !ring(i.uv.xy, circle_pos, _Radius*1.2);
                            rings = rings | ring(i.uv.xy, circle_pos, _Radius);
                            rings = rings & !ring(i.uv.xy, circle_pos, _Radius*.8);
                            rings = rings | ring(i.uv.xy, circle_pos, _Radius*.6);
                            rings = rings & !ring(i.uv.xy, circle_pos, _Radius*.4);
                            rings = rings | ring(i.uv.xy, circle_pos, _Radius*.2);
                        } else if (false) {
                            rings = rings | ring(i.uv.xy, circle_pos, _Radius);
                            rings = rings & !ring(i.uv.xy, circle_pos, _Radius*.65);
                        }
                    }
                }
                
                return rings * _MaskColor + (1-rings) * _BgColor; 
                }
            ENDCG
            }
        }
    }