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

    public event EventHandler unitClickEvent;

    public MapKit.Widgets.ProgressLongArrow arrow;

    public Control propogateTo;

    public override void _Ready()
    {
        material = (ShaderMaterial)Material;

        Connect("mouse_entered", this, nameof(OnMouseEnter));
        Connect("mouse_exited", this, nameof(OnMouseExited));
    }

    public override string ToString() => $"StrategyPad({unit})";

    public void Setup(Unit unit, Node arrowContainer, Control propogateTo=null)
    {
        this.arrowContainer = arrowContainer;
        RectPosition = unit.parent.center;
        this.unit = unit;
        SyncPortrait();
        this.propogateTo = propogateTo; // Hack Godot's broken Event propogation system.

        unit.movingState.updated += OnMovingStateUpdated;
        // pad.unitClickEvent += OnUnitClick;
        unit.moveStateUpdated += OnUnitMoveEvent;
        unit.destroyed += OnUnitDestroyed;
        unit.moveStateUpdated += OnUnitMoveEvent;
        unit.childrenUpdated += OnUnitChildrenUpdate;
    }

    public void OnUnitChildrenUpdate(object sender, EventArgs _)
    {
        if(unit.children.Count > 0)
            SyncPortrait();
    }

    public void SyncPortrait()
    {
        Texture = unit.children[0].portrait;
    }

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
    public void OnMovingStateUpdated(object sender, bool progressionOnlyUpdate)
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

    
    public override void _GuiInput(InputEvent @event) // unit selection toggle -> stack toggle
    {
        var clickEvent = @event as InputEventMouseButton;
        if(clickEvent != null)
        {
            if(clickEvent.ButtonIndex == (int)ButtonList.Left && clickEvent.Pressed)
            {
                unitClickEvent?.Invoke(this, EventArgs.Empty);
                return; // Disable Propogating for Left-clicking only.
            }
        }

        propogateTo?._GuiInput(@event);
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

    public void OnUnitMoveEvent(object sender, Unit.MovePath movePath)
    {
        var regions = movePath.reachedRegions;
        RectPosition = regions[regions.Count-1].center;
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
}


}