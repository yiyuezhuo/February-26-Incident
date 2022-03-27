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
    public event EventHandler rightClicked;
    public bool highlighted;

    protected IData data;

    public override void _Ready()
    {
        portrait = (TextureRect)GetNode(portraitPath);

        portraitMaterial = (ShaderMaterial)portrait.Material;
    }

    public virtual void SetData(IData data)
    {
        this.data = data;
        SoftUpdate();
    }

    public virtual void SoftUpdate()
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
        if(clickEvent != null  && clickEvent.Pressed)
        {
            switch((ButtonList)clickEvent.ButtonIndex)
            {
                case ButtonList.Left:
                    clicked?.Invoke(this, EventArgs.Empty);
                    break;
                case ButtonList.Right:
                    rightClicked?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}


}