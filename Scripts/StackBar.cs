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

    IData data;

    public EventHandler<int> clicked;
    public EventHandler<int> rightClicked;

    public override void _Ready()
    {
        unitPadContainer = (Control)GetNode(unitPadContainerPath);

        foreach(Node node in unitPadContainer.GetChildren())
            node.QueueFree();
    }

    public void Update()
    {
        
    }

    public void SetData(IData data)
    {
        ClearChildren();
        
        if(data is null)
            return;
        
        foreach(var unitPadData in data.children)
        {
            var unitPad = unitPadPackedScene.Instance<UnitPad>();
            unitPadContainer.AddChild(unitPad);
            unitPad.SetData(unitPadData);
            // unitPad.Connect("")
            unitPad.clicked += OnClick;
            unitPad.rightClicked += OnRightClicked;

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

    void OnClick(object sender, EventArgs _)
    {
        var unitPad = (UnitPad)sender;
        var idx = unitPadList.IndexOf(unitPad);
        clicked?.Invoke(this, idx);
    }

    void OnRightClicked(object sender, EventArgs _) => rightClicked?.Invoke(this, unitPadList.IndexOf((UnitPad)sender));
}


}