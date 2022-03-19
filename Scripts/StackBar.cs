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
    List<UnitPad> unitPadList = new List<UnitPad>();
    UnitPad focusing;

    public EventHandler<int> unitClickEvent;

    public override void _Ready()
    {
        unitPadContainer = (Control)GetNode(unitPadContainerPath);

        foreach(Node node in unitPadContainer.GetChildren())
            node.QueueFree();
    }

    public void SetData(IData data)
    {
        ClearChildren();
        
        foreach(var unitPadData in data.children)
        {
            var unitPad = unitPadPackedScene.Instance<UnitPad>();
            unitPadContainer.AddChild(unitPad);
            unitPad.SetData(unitPadData);
            // unitPad.Connect("")
            unitPad.clicked += OnUnitClick;

            unitPadList.Add(unitPad);
        }
    }

    public void ClearChildren()
    {
        foreach(Node node in unitPadList)
            node.QueueFree();
        unitPadList.Clear();
        focusing = null;
    }

    public interface IData
    {
        IEnumerable<UnitPad.IData> children{get;}
    }

    public void SetFocus(int idx)
    {
        focusing?.Highlight(false);
        focusing = unitPadList[idx];
        focusing.Highlight(true);
    }

    public void OnUnitClick(object sender, EventArgs _)
    {
        var unitPad = (UnitPad)sender;
        var idx = unitPadList.IndexOf(unitPad);
        unitClickEvent?.Invoke(this, idx);
    }
}


}