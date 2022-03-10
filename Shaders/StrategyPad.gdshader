shader_type canvas_item;

uniform bool selected;

void fragment(){
	if(!selected){
		COLOR = texture(TEXTURE, UV);
	}
	else{
		if(UV.x < 0.05 || UV.x > 0.95 || UV.y < 0.05 || UV.y > 0.95){
			COLOR = vec4(1.0, 0.0, 0.0, 1.0);
		}else{
			COLOR = texture(TEXTURE, UV);
		}
	}
}