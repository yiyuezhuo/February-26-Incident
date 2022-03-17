namespace YYZ.App
{


using Godot;
using System;

using System.Linq;
using System.Collections.Generic; 

public class StrategyPad : TextureRect
{
    [Export] PackedScene progressLongArrowScene;

    public Node arrowContainer;

    ShaderMaterial material;
    // bool selected = false;
    public SelectionState selectionState = SelectionState.Unselected;

    public Unit unit;

    public event EventHandler unitClickEvent;

    public MapKit.Widgets.ProgressLongArrow arrow;

    public override void _Ready()
    {
        material = (ShaderMaterial)Material;

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExited));
    }

    public override string ToString() => $"StrategyPad({unit})";

    public void OnMouseEnter()
    {
        material.SetShaderParam("hovering", true);
    }

    public void OnMouseExited()
    {
        material.SetShaderParam("hovering", false);
    }

    /// <summary>
    /// Update arrow related UI.
    /// </summary>
    public void OnMovingStateUpdated(object sender, EventArgs _)
    {
        var movingState = (MovingState)sender;
        if(!movingState.active)
        {
            arrow?.QueueFree();
            arrow = null;
            return;
        }

        
    }

    public override void _GuiInput(InputEvent @event) // unit selection toggle -> stack toggle
    {
        var clickEvent = @event as InputEventMouseButton;
        if(clickEvent != null)
        {
            if(clickEvent.ButtonIndex == (int)ButtonList.Left && clickEvent.Pressed)
            {
                unitClickEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Any effect other than event invoking.
    /// </summary>
    public void OnSelectionStateUpdated()
    {
        material.SetShaderParam("selected", selectionState == SelectionState.Selected);
    }

    public void SyncArrowPercent()
    {
        if(arrow != null)
            arrow.SetPercent(unit.movingState.movedDistance / unit.movingState.totalDistance);
    }

    public void SyncArrowAlpha()
    {
        if(arrow != null)
        {
            if(selectionState == SelectionState.SoftSelected)
                arrow.SetAlpha(0.7f);
            else
                arrow.SetAlpha(1.0f);
        }
    }

    public List<Region> GoForward(float movement)
    {
        var reachedRegions = unit.GoForward(movement);
        if(reachedRegions.Count > 0)
        {
            RectPosition = reachedRegions[reachedRegions.Count-1].center;
            SyncArrowShape();
        }
        SyncArrowPercent();
        return reachedRegions;
    }

    /// <summary>
    /// Sync arrow "shape" but not percent.
    /// </summary>
    public void SyncArrowShape()
    {
        if(arrow != null)
            arrow.QueueFree();
        if(!unit.movingState.active || selectionState == SelectionState.Unselected)
        {
            arrow = null;
            return;
        }
        
        arrow = progressLongArrowScene.Instance<MapKit.Widgets.ProgressLongArrow>();
        arrowContainer.AddChild(arrow);

        var controlPoints = unit.movingState.path.Select(x => x.center);
        arrow.SetCurvePositions(controlPoints);
    }

    public enum SelectionState // The associated int value are passed as shader param.
    {
        Unselected = 0,
        SoftSelected = 1, // A state between selected and unselected. Keep the arrow.
        Selected = 2,
    }

    public void TryForceDeselect()
    {
        if(selectionState != StrategyPad.SelectionState.Unselected)
        {
            selectionState = StrategyPad.SelectionState.Unselected;
            OnSelectionStateUpdated(); // TODO: We don't need call this when state == SoftSelected at this time. But the situation may change soon.
            SyncArrowShape();
        }
    }

    public void TrySoftSelect()
    {
        if(selectionState == StrategyPad.SelectionState.Unselected)
        {
            selectionState = StrategyPad.SelectionState.SoftSelected;
            OnSelectionStateUpdated(); // TODO: We don't need call this when state == SoftSelected at this time. But the situation may change soon.
            SyncArrowShape();
            SyncArrowAlpha();
        }
    }
}


}