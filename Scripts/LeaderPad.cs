namespace YYZ.App
{


using Godot;
using System;
using System.Linq;

public class LeaderPad : PortraitPad
{
    [Export] NodePath nameLabelPath;
    [Export] NodePath nameJapLabelPath;
    Label nameLabel;
    Label nameJapLabel;

    public override void _Ready()
    {
        base._Ready();

        nameLabel = (Label)GetNode(nameLabelPath);
        nameJapLabel = (Label)GetNode(nameJapLabelPath);
    }

    static string VertialTransform(string s) => string.Join("\n", from c in s select c);

    public void SetData(IData data)
    {
        base.SetData(data);

        nameLabel.Text = data.name;
        nameJapLabel.Text = VertialTransform(data.nameJap);
    }

    public new interface IData : PortraitPad.IData
    {
        string name{get;}
        string nameJap{get;}
    }
}


}