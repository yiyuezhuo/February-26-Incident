namespace YYZ.App
{


using Godot;
using System;

using YYZ.Data.February26;

/*
public class UnitselectedUpdateEventArgs
{
    public Unit unit;
    public bool selected;
}
*/

public class StrategyPad : TextureRect
{
    ShaderMaterial material;
    bool selected = false;

    public Unit unit;

    public event EventHandler<bool> unitSelectedUpdateEvent;

    public override void _Ready()
    {
        material = (ShaderMaterial)Material; // The `resource_local_to_scene` is enabled, so we don't have to duplicate them manually.

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExited));
    }

    public void OnMouseEnter()
    {
        material.SetShaderParam("hovering", true);
    }

    public void OnMouseExited()
    {
        material.SetShaderParam("hovering", false);
    }

    public override void _GuiInput(InputEvent @event)
    {
        var clickEvent = @event as InputEventMouseButton;
        if(clickEvent != null)
        {
            if(clickEvent.ButtonIndex == (int)ButtonList.Left && clickEvent.Pressed)
            {
                selected = !selected;
                // material.SetShaderParam("selected", selected);
                OnSelectedUpdated();

                unitSelectedUpdateEvent?.Invoke(this, selected);
            }
        }
    }

    /// <summary>
    /// Any effect other than event invoking.
    /// </summary>
    public void OnSelectedUpdated()
    {
        material.SetShaderParam("selected", selected);
    }

    public void SetSelected(bool selected)
    {
        this.selected = selected;
        OnSelectedUpdated();
    }
}


}