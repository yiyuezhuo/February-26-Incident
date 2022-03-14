namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class UnitBar : Control
{
    [Export] NodePath unitInfoPadPath;
    [Export] NodePath leaderPadContainerPath;
    [Export] PackedScene leaderPadScene;

    UnitInfoPad unitInfoPad;
    Control leaderPadContainer;
    
    public override void _Ready()
    {
        unitInfoPad = (UnitInfoPad)GetNode(unitInfoPadPath);
        leaderPadContainer = (Control)GetNode(leaderPadContainerPath);

        ClearLeaderData(); // Delete dummy data for editor preview.
    }

    /// <summary>
    /// If data is null, no UnitInfoPad will show no text but the framework, and no LeaderPad will be displayer.
    /// </summary>
    public void SetData(IData data)
    {
        unitInfoPad.SetData(data);
        ClearLeaderData();
        if(!(data is null))
        {
            foreach(var leaderData in data.children)
            {
                var leaderPad = leaderPadScene.Instance<LeaderPad>();
                leaderPadContainer.AddChild(leaderPad);

                leaderPad.SetData(leaderData); // Nodes filled here are initialized after leaderPad is added to tree.
            }
        }
    }

    void ClearLeaderData()
    {
        foreach(LeaderPad child in leaderPadContainer.GetChildren())
            child.QueueFree(); // TODO: Object Pool refactor?
    }

    public interface IData : UnitInfoPad.IData
    {
        IEnumerable<LeaderPad.IData> children{get;}
    }

}



}