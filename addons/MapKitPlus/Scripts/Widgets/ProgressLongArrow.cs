namespace YYZ.MapKitPlus.Widgets
{


using Godot;

/// <summary>
/// ProgressLongArrow assumes that two Line2Ds have a material to illustrate progression percent.
/// </summary>
public class ProgressLongArrow : LongArrow
{
    ShaderMaterial headMaterial;
    ShaderMaterial bodyMaterial;

    public override void _Ready()
    {
        base._Ready();

        headMaterial = (ShaderMaterial)head.Material;
        bodyMaterial = (ShaderMaterial)body.Material;
    }
    public void SetPercent(float percent)
    {
        bodyMaterial.SetShaderParam("percent", Mathf.Min(1.0f, percent / bodyEndPercent));
        headMaterial.SetShaderParam("percent", percent < headBeginPercent ? 0.0f : (percent - headBeginPercent) / (1 - headBeginPercent));
    }

    public void SetModulation(Color color)
    {
        bodyMaterial.SetShaderParam("modulation_color", color);
        headMaterial.SetShaderParam("modulation_color", color);
    }

    public void SetAlpha(float alpha) => SetModulation(new Color(1.0f, 1.0f, 1.0f, alpha));
}


}