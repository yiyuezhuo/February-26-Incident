namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class InsetLongArrow : Node2D
{
    [Export] NodePath outerArrowPath;
    [Export] NodePath innerArrowPath;

    MapKit.Widgets.LongArrow outerArrow;
    MapKit.Widgets.LongArrow innerArrow;

    public override void _Ready()
    {
        outerArrow = (MapKit.Widgets.LongArrow)GetNode(outerArrowPath);
        innerArrow = (MapKit.Widgets.LongArrow)GetNode(innerArrowPath);
    }

    public void SetCurvePositions(IEnumerable<Vector2> controlPoints)
    {
        outerArrow.SetCurvePositions(controlPoints);
        innerArrow.SetCurvePositions(controlPoints);
    }
}


}