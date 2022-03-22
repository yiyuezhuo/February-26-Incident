namespace YYZ.App
{


using Godot;
using System;

using System.Linq;
using System.Collections.Generic; 

public class StrategyPad : TextureRect
{
    [Export] PackedScene progressLongArrowScene;

    Node arrowContainer;

    ShaderMaterial material;
    SelectionState _selectionState = SelectionState.Unselected;
    public SelectionState selectionState
    {
        get => _selectionState;
        set 
        {
            _selectionState = value;
            OnSelectionStateUpdated();
        }
    }

    public Unit unit;

    public event EventHandler clicked;

    MapKit.Widgets.ProgressLongArrow arrow;

    Control propogateTo;

    public override void _Ready()
    {
        material = (ShaderMaterial)Material;

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExited));
    }

    public override void _GuiInput(InputEvent @event) // unit selection toggle -> stack toggle
    {
        var clickEvent = @event as InputEventMouseButton;
        if(clickEvent != null)
        {
            if(clickEvent.ButtonIndex == (int)ButtonList.Left && clickEvent.Pressed)
            {
                clicked?.Invoke(this, EventArgs.Empty);
                return; // Disable Propogating for Left-clicking only.
            }
        }

        propogateTo?._GuiInput(@event);
    }

    public override string ToString() => $"StrategyPad({unit})";

    public void Setup(Unit unit, Node arrowContainer, Control propogateTo=null)
    {
        this.arrowContainer = arrowContainer;
        RectPosition = unit.center;
        this.unit = unit;
        SyncPortrait();
        this.propogateTo = propogateTo; // Hack Godot's broken Event propogation system.

        unit.movingState.updated += OnMovingStateUpdated;
        unit.moveStateUpdated += OnUnitMoveEvent;
        unit.destroyed += OnUnitDestroyed;
        unit.childrenUpdated += OnUnitChildrenUpdate;
    }

    public enum SelectionState // The associated int value are passed as shader param.
    {
        Unselected = 0,
        SoftSelected = 1, // A state between selected and unselected. Keep the arrow.
        Selected = 2,
    }

    public void TrySetArrowAlpha()
    {
        if(arrow != null)
        {
            if(selectionState == SelectionState.SoftSelected)
                arrow.SetAlpha(0.6f);
            else
                arrow.SetAlpha(1.0f);
        }
    }

    public void TryForceDeselect()
    {
        if(selectionState != StrategyPad.SelectionState.Unselected)
        {
            selectionState = StrategyPad.SelectionState.Unselected;
        }
    }

    public void TrySoftSelect()
    {
        if(selectionState == StrategyPad.SelectionState.Unselected)
        {
            selectionState = StrategyPad.SelectionState.SoftSelected;
            TrySetArrowAlpha();
        }
    }

    // Private methods

    void OnUnitChildrenUpdate(object sender, EventArgs _)
    {
        if(unit.children.Count > 0)
            SyncPortrait();
    }

    void SyncPortrait()
    {
        Texture = unit.children[0].portrait;
    }

    void OnMouseEnter()
    {
        material.SetShaderParam("hovering", true);
    }

    void OnMouseExited()
    {
        material.SetShaderParam("hovering", false);
    }

    /// <summary>
    /// Update arrow related UI.
    /// </summary>
    void OnMovingStateUpdated(object sender, bool progressionOnlyUpdate)
    {
        var movingState = (MovingState)sender;

        if(progressionOnlyUpdate)
        {
            arrow?.SetPercent(unit.movingState.movedDistance / unit.movingState.totalDistance);
            return;
        }

        arrow?.QueueFree();
        if(!movingState.active || selectionState == SelectionState.Unselected)
        {
            arrow = null;
            return;
        }

        // So movingState.active == true && selectionState != SelectionState.Unselected in following code: (with arrow being "undefined")
        
        IntializeArrow();
    }

    void OnUnitDestroyed(object sender, Unit unit)
    {
        arrow?.QueueFree();
        QueueFree();
        // arrow = null;
        // TODO: reset movingState?
    }

    void IntializeArrow()
    {
        arrow = progressLongArrowScene.Instance<MapKit.Widgets.ProgressLongArrow>();
        arrowContainer.AddChild(arrow);

        var controlPoints = unit.movingState.path.Select(x => x.center);
        arrow.SetCurvePositions(controlPoints);

        arrow.SetPercent(unit.movingState.movedDistance / unit.movingState.totalDistance);
    }

    
    
    /// <summary>
    /// Any effect other than event invoking.
    /// </summary>
    void OnSelectionStateUpdated()
    {
        material.SetShaderParam("selected", selectionState == SelectionState.Selected);

        if(selectionState != SelectionState.Unselected && arrow is null && unit.movingState.active)
        {
            IntializeArrow();
        }
        else if(selectionState == SelectionState.Unselected)
        {
            arrow?.QueueFree();
            arrow = null;
        }
    }

    void OnUnitMoveEvent(object sender, Unit.MovePath movePath)
    {
        var regions = movePath.reachedRegions;
        RectPosition = regions[regions.Count-1].center;
    }

}


}