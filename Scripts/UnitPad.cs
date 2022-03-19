using Godot;
using System;

public class UnitPad : Node
{
    [Export] NodePath portraitPath;
    [Export] NodePath strengthLabelPath;
    [Export] NodePath fatigueLabelPath;

    TextureRect portrait;
    Label strengthLabel;
    Label fatigueLabel;

    ShaderMaterial portraitMaterial;

    public override void _Ready()
    {
        strengthLabel = (Label)GetNode(strengthLabelPath);
        fatigueLabel = (Label)GetNode(fatigueLabelPath);
        portrait = (TextureRect)GetNode(portraitPath);

        portraitMaterial = (ShaderMaterial)portrait.Material;
    }

    public void SetData(IData data)
    {
        portrait.Texture = data.portrait;
        strengthLabel.Text = data.strength.ToString("N0");
        fatigueLabel.Text = data.fatigue.ToString("P0");
    }

    public interface IData
    {
        Texture portrait{get;}
        float strength{get;}
        float fatigue{get;}
    }

    public void Highlight(bool highlight)
    {
        portraitMaterial.SetShaderParam("highlight", highlight);
    }

}
