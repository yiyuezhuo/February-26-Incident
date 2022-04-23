namespace YYZ.MapKitPlus.Widgets
{


using Godot;
using System;
using System.Collections.Generic;

public interface IArrowData
{
    Vector2 src{get;set;}
    Vector2 pointTo{get;set;}
}

public class ArrowContainer : AutoExpandedContainer<Arrow, IArrowData>
{
    [Export] PackedScene arrowScene;

    protected override PackedScene GetRefScene() => arrowScene;

    public override void _Ready()
    {
    }

    protected override void ApplyData(Arrow child, IArrowData data)
    {
        child.MoveToCenter(data.src, data.pointTo);
    }
}


}