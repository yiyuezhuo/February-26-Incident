namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class InsetLongArrow : Node2D
{
    [Export] NodePath outerArrowPath;
    [Export] NodePath innerArrowPath;

    MapKitPlus.Widgets.LongArrow outerArrow;
    MapKitPlus.Widgets.LongArrow innerArrow;

    public override void _Ready()
    {
        outerArrow = (MapKitPlus.Widgets.LongArrow)GetNode(outerArrowPath);
        innerArrow = (MapKitPlus.Widgets.LongArrow)GetNode(innerArrowPath);
    }

    public void SetCurvePositions(IEnumerable<Vector2> controlPoints)
    {
        outerArrow.SetCurvePositions(controlPoints);
        innerArrow.SetCurvePositions(controlPoints);
    }
}


}