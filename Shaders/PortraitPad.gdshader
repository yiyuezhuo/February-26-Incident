shader_type canvas_item;

uniform bool highlight;

// Ok, Godot doesn't have #include at this time... So I must copy & paste those "solid" library function eveywhere.

//https://github.com/Jam3/glsl-hsl2rgb/blob/master/index.glsl
vec3 hsl2rgb( in vec3 c )
{
    vec3 rgb = clamp( abs(mod(c.x*6.0+vec3(0.0,4.0,2.0),6.0)-3.0)-1.0, 0.0, 1.0 );

    return c.z + c.y * (rgb-0.5)*(1.0-abs(2.0*c.z-1.0));
}

// https://www.shadertoy.com/view/XljGzV
vec3 rgb2hsl( in vec3 c ){
  	float h = 0.0;
	float s = 0.0;
	float l = 0.0;
	float r = c.r;
	float g = c.g;
	float b = c.b;
	float cMin = min( r, min( g, b ) );
	float cMax = max( r, max( g, b ) );

	l = ( cMax + cMin ) / 2.0;
	if ( cMax > cMin ) {
		float cDelta = cMax - cMin;
        
        //s = l < .05 ? cDelta / ( cMax + cMin ) : cDelta / ( 2.0 - ( cMax + cMin ) ); Original
		s = l < .0 ? cDelta / ( cMax + cMin ) : cDelta / ( 2.0 - ( cMax + cMin ) );
        
		if ( r == cMax ) {
			h = ( g - b ) / cDelta;
		} else if ( g == cMax ) {
			h = 2.0 + ( b - r ) / cDelta;
		} else {
			h = 4.0 + ( r - g ) / cDelta;
		}

		if ( h < 0.0) {
			h += 6.0;
		}
		h = h / 6.0;
	}
	return vec3( h, s, l );
}


void fragment(){
	COLOR = texture(TEXTURE, UV);
	if(highlight){
		float dist = pow(distance(UV, vec2(0.5, 0.5)) * sqrt(2), 4);
		vec3 highlighted = hsl2rgb(rgb2hsl(COLOR.rgb) + vec3(0.0, 0.01, 1.0 * dist));
		highlighted = mix(highlighted, vec3(0.5,0.2,0), dist);
		COLOR = vec4(highlighted, 1.0);
	}
}