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
                /*
                if(selectionState == SelectionState.Selected)
                    selectionState = SelectionState.SoftUnselected;
                else
                    selectionState = SelectionState.Unselected;
                
                OnSelectionStateUpdated();
                */

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

    /*
    public void SetSelected(bool selected)
    {
        this.selected = selected;
        OnSelectionStateUpdated();
    }

    public void Select()
    {
        this.selected = true;
        OnSelectionStateUpdated();
    }

    /// <summary>
    /// Goto a state between selected and unselected. Keep the arrow.
    /// </summary>
    public void SoftUnselect()
    {
        this.selected = true;
        OnSelectionStateUpdated();
    }
    */

    public void SyncArrowPercent()
    {
        if(arrow != null)
            arrow.SetPercent(unit.movingState.movedDistance / unit.movingState.totalDistance);
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

    public enum SelectionState
    {
        Unselected,
        SoftSelected, // A state between selected and unselected. Keep the arrow.
        Selected,
    }
}


}