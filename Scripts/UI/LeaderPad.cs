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

    new IData data;

    public override void _Ready()
    {
        base._Ready();

        nameLabel = (Label)GetNode(nameLabelPath);
        nameJapLabel = (Label)GetNode(nameJapLabelPath);
    }

    static string VertialTransform(string s) => string.Join("\n", from c in s select c);

    public void SetData(IData data)
    {
        base.data = data;

        this.data = data;
        SoftUpdate();
    }

    public override void SoftUpdate()
    {
        base.SoftUpdate();
        
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