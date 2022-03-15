shader_type canvas_item;

uniform bool selected;
uniform vec4 selected_color : hint_color = vec4(1,0,0,1);
uniform bool hovering;                
uniform vec4 hovering_color : hint_color = vec4(1, 0.7, 0.7, 1);

void fragment(){
	if(!selected && !hovering){
		COLOR = texture(TEXTURE, UV);
	}
	else{
		if(UV.x < 0.05 || UV.x > 0.95 || UV.y < 0.05 || UV.y > 0.95){
			COLOR = selected ? selected_color : hovering_color;
		}else{
			COLOR = texture(TEXTURE, UV);
		}
	}
}