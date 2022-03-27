namespace YYZ.App
{


using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class UnitBar : Control
{
    [Export] NodePath unitInfoPadPath;
    [Export] NodePath leaderPadContainerPath;
    [Export] PackedScene leaderPadScene;

    UnitInfoPad unitInfoPad;
    Control leaderPadContainer;
    List<LeaderPad> leaderPadList = new List<LeaderPad>();

    IData data;
    
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
        this.data = data;

        unitInfoPad.SetData(data);
        ClearLeaderData();
        if(!(data is null))
        {
            foreach(var leaderData in data.children)
            {
                var leaderPad = leaderPadScene.Instance<LeaderPad>();
                leaderPadContainer.AddChild(leaderPad);

                leaderPad.SetData(leaderData); // Nodes filled here are initialized after leaderPad is added to tree.
                leaderPad.clicked += OnLeadPadClicked;
            }
        }
    }

    public void SoftUpdate()
    {
        foreach(LeaderPad pad in leaderPadContainer.GetChildren())
            pad.SoftUpdate();
        unitInfoPad.SoftUpdate();
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

    void OnLeadPadClicked(object sender, EventArgs _)
    {
        var leaderPad = (LeaderPad)sender;

        leaderPad.Highlight(!leaderPad.highlighted);
    }

    // public List<bool> GetHighlightedStates() => leaderPadContainer.GetChildren().Select(pad => pad.highlighted.ToList();
    // Godot.Array doesn't support Linq?
    public List<bool> GetHighlightedStates()
    {
        var ret = new List<bool>();
        foreach(LeaderPad pad in leaderPadContainer.GetChildren())
            ret.Add(pad.highlighted);
        return ret;
    }

}



}