namespace YYZ.MapKit.Widgets{


using Godot;

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
        bodyMaterial.SetShaderParam("percent", Mathf.Min(1.0f, percent / headBeginPercent));
        headMaterial.SetShaderParam("percent", percent < headBeginPercent ? 0.0f : (percent - headBeginPercent) / (1 - headBeginPercent));
    }
}


}