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
        // material = (ShaderMaterial)Material.Duplicate(); // "Local to scene" is enabled.
        material = (ShaderMaterial)Material;

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExited));

        // var p = Mathf.Min(width / Texture.GetWidth(), height / Texture.GetHeight());
        // SetScale(new Vector2(p));
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
                arrow.SetAlpha(0.5f);
            else
                arrow.SetAlpha(1.0f);
        }
    }

    public void GoForward(float movement, out List<Region> reachedRegions)
    {
        var completed = unit.GoForward(movement, out reachedRegions);
        if(reachedRegions.Count > 0)
        {
            RectPosition = reachedRegions[reachedRegions.Count-1].center;
            SyncArrowShape();
        }
        SyncArrowPercent();
    }

    /// <summary>
    /// Sync arrow "shape" but not percent.
    /// </summary>
    public void SyncArrowShape()
    {
        if(arrow != null)
            arrow.QueueFree();
        if(unit.movingState == null || selectionState == SelectionState.Unselected)
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