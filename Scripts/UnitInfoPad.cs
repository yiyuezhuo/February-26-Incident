namespace YYZ.App
{


using Godot;
using System;


public class UnitInfoPad : Control
{
    [Export] NodePath strengthLabelPath;
    [Export] NodePath commandLabelPath;
    [Export] NodePath fatigueLabelPath;
    Label strengthLabel;
    Label commandlabel;
    Label fatigueLabel;

    public override void _Ready()
    {
        strengthLabel = (Label)GetNode(strengthLabelPath);
        commandlabel = (Label)GetNode(commandLabelPath);
        fatigueLabel = (Label)GetNode(fatigueLabelPath);

        SetData(null); // Clear dummy data for editor preview.
    }

    /// <summary>
    /// If data is null, will show no text but the pad still exists.
    /// </summary>
    public void SetData(IData data)
    {
        strengthLabel.Text = data is null ? "" : "Strength: " + data.strength.ToString("N0");
        commandlabel.Text = data is null ? "" : "Command: " + data.command.ToString("N0");
        fatigueLabel.Text = data is null ? "" : "Fatigue:" + data.fatigue.ToString("P");
    }

    public interface IData
    {
        float strength{get;}
        float command{get;}
        float fatigue{get;}
    }
}


}