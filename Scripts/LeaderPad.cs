namespace YYZ.App
{


using Godot;
using System;
using System.Linq;

public class LeaderPad : Control
{
    [Export] NodePath nameLabelPath;
    [Export] NodePath nameJapLabelPath;
    [Export] NodePath portraitPath;
    Label nameLabel;
    Label nameJapLabel;
    TextureRect portrait;

    public override void _Ready()
    {
        nameLabel = (Label)GetNode(nameLabelPath);
        nameJapLabel = (Label)GetNode(nameJapLabelPath);
        portrait = (TextureRect)GetNode(portraitPath);
    }

    public void SetData(string name, string nameJap, Texture portraitTex)
    {
        nameLabel.Text = name;
        nameJapLabel.Text = VertialTransform(nameJap);
        portrait.Texture = portraitTex;
    }

    static string VertialTransform(string s) => string.Join("\n", from c in s select c);

    public void SetData(IData data) => SetData(data.name, data.nameJap, data.portrait);

    public interface IData
    {
        string name{get;}
        string nameJap{get;}
        Texture portrait{get;}
    }

}


}