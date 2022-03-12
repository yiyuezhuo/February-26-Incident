namespace YYZ.App
{


using Godot;
using System;

using YYZ.Data.February26;
using System.Linq;

public class StrategyPad : TextureRect
{
    [Export] PackedScene progressLongArrowScene;

    public Node arrowContainer;

    ShaderMaterial material;
    bool selected = false;

    public Unit unit;

    public event EventHandler<bool> unitSelectedUpdateEvent;

    public MapKit.Widgets.ProgressLongArrow arrow;

    public override void _Ready()
    {
        // material = (ShaderMaterial)Material.Duplicate();
        material = (ShaderMaterial)Material;

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

    public void SyncArrowPercent()
    {
        if(arrow != null)
            arrow.SetPercent(unit.movingState.movedDistance / unit.movingState.totalDistance);
    }

    public void GoForward(float movement)
    {
        var completed = unit.GoForward(movement, out var lastReached);
        if(lastReached != null)
        {
            RectPosition = lastReached.center;
            SyncArrow();
        }
        SyncArrowPercent();
    }

    /// <summary>
    /// Sync arrow "shape" but not percent.
    /// </summary>
    public void SyncArrow()
    {
        if(arrow != null)
            arrow.QueueFree();
        if(unit.movingState == null)
        {
            arrow = null;
            return;
        }
        
        arrow = progressLongArrowScene.Instance<MapKit.Widgets.ProgressLongArrow>();
        arrowContainer.AddChild(arrow);

        var controlPoints = unit.movingState.path.Select(x => x.center);
        arrow.SetCurvePositions(controlPoints);
    }
}


}