namespace YYZ.App
{


using Godot;
using System;

public class UnitPad : PortraitPad
{
    [Export] NodePath strengthLabelPath;
    [Export] NodePath fatigueLabelPath;

    Label strengthLabel;
    Label fatigueLabel;

    public override void _Ready()
    {
        base._Ready();

        strengthLabel = (Label)GetNode(strengthLabelPath);
        fatigueLabel = (Label)GetNode(fatigueLabelPath);
    }

    public void SetData(IData data)
    {
        base.SetData(data);
        
        strengthLabel.Text = data.strength.ToString("N0");
        fatigueLabel.Text = data.fatigue.ToString("P0");
    }

    public new interface IData : PortraitPad.IData
    {
        float strength{get;}
        float fatigue{get;}
    }
}


}