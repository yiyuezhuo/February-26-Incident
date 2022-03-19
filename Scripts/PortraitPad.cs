namespace YYZ.App
{


using Godot;
using System;

public class PortraitPad : Control
{
    [Export] NodePath portraitPath;
    protected TextureRect portrait;
    protected ShaderMaterial portraitMaterial;

    public event EventHandler clicked;
    public bool highlighted;

    public override void _Ready()
    {
        portrait = (TextureRect)GetNode(portraitPath);

        portraitMaterial = (ShaderMaterial)portrait.Material;
    }

    public virtual void SetData(IData data)
    {
        portrait.Texture = data.portrait;
    }

    public interface IData
    {
        Texture portrait{get;}
    }

    public void Highlight(bool highlight)
    {
        if(highlighted != highlight)
        {
            portraitMaterial.SetShaderParam("highlight", highlight);
            highlighted = highlight;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {

        var clickEvent = @event as InputEventMouseButton;
        if(clickEvent != null && clickEvent.ButtonIndex == (int)ButtonList.Left && clickEvent.Pressed)
            clicked?.Invoke(this, EventArgs.Empty);
    }
}


}