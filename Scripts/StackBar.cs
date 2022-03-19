namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class StackBar : Node
{
    [Export] NodePath unitPadContainerPath;
    [Export] PackedScene unitPadPackedScene;
    Control unitPadContainer;

    public override void _Ready()
    {
        unitPadContainer = (Control)GetNode(unitPadContainerPath);

        ClearChildren();
    }

    public void SetData(IData data)
    {
        ClearChildren();
        
        foreach(var unitPadData in data.children)
        {
            var unitPad = unitPadPackedScene.Instance<UnitPad>();
            unitPadContainer.AddChild(unitPad);
            unitPad.SetData(unitPadData);
        }
    }

    public void ClearChildren()
    {
        foreach(Node node in unitPadContainer.GetChildren())
            node.QueueFree();
    }

    public interface IData
    {
        IEnumerable<UnitPad.IData> children{get;}
    }
}


}