namespace YYZ.App
{


using Godot;
using System;


public class UnitInfoPad : Control
{
    [Export] NodePath strengthLabelPath;
    [Export] NodePath commandLabelPath;
    [Export] NodePath fatigueLabelPath;
    [Export] NodePath suppressionLabelPath;
    Label strengthLabel;
    Label commandlabel;
    Label fatigueLabel;
    Label suppressionLabel;

    IData data;

    public override void _Ready()
    {
        strengthLabel = (Label)GetNode(strengthLabelPath);
        commandlabel = (Label)GetNode(commandLabelPath);
        fatigueLabel = (Label)GetNode(fatigueLabelPath);
        suppressionLabel = (Label)GetNode(suppressionLabelPath);

        SetData(null); // Clear dummy data for editor preview.
    }

    /// <summary>
    /// If data is null, will show no text but the pad still exists.
    /// </summary>
    public void SetData(IData data)
    {
        this.data = data;

        SoftUpdate();
    }

    public void SoftUpdate()
    {
        strengthLabel.Text = data is null ? "" : "Strength: " + data.strength.ToString("N0");
        commandlabel.Text = data is null ? "" : "Command: " + data.command.ToString("N0");
        fatigueLabel.Text = data is null ? "" : "Fatigue:" + data.fatigue.ToString("P");
        suppressionLabel.Text = data is null ? "" : "Suppression:" + data.suppression.ToString("P");
    }

    public interface IData
    {
        float strength{get;}
        float command{get;}
        float fatigue{get;}
        float suppression{get;}
    }
}


}