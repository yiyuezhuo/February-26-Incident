shader_type canvas_item;

uniform vec4 inset_color : hint_color = vec4(1.0, 0.3, 0.3, 1.0);
uniform vec4 outside_color : hint_color = vec4(1.0, 0, 0, 1.0);
uniform vec4 modulation_color : hint_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float percent;

void fragment(){
	float p = percent;
	// float p = abs(sin(TIME)); // debug
	// if(UV.x <= p && (UV.y > 0.2 && UV.y < 0.8)){
	if(UV.x <= p){
		COLOR = inset_color * modulation_color;
	}else{
		COLOR = outside_color * modulation_color;
	}
}