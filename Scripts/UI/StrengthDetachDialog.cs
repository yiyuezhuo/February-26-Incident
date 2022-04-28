using Godot;
using System;

public class StrengthDetachDialog : ConfirmationDialog // ConfirmationDialog has a "confirmed" signal
{
    [Export] NodePath sliderPath;
    [Export] NodePath labelPath;

    Slider slider;
    Label label;

    public event EventHandler<float> confirmed;

    public override void _Ready()
    {
        slider = (Slider)GetNode(sliderPath);
        label = (Label)GetNode(labelPath);

        slider.Connect("value_changed", this, nameof(OnSliderValueChanged));
        Connect("confirmed", this, nameof(OnConfirmed));

        OnSliderValueChanged((float)slider.Value);
    }

    void OnSliderValueChanged(float value)
    {
        label.Text = value.ToString("N0");
    }

    void OnConfirmed()
    {
        confirmed?.Invoke(this, (float)slider.Value);
    }

    public void Setup(float maxValue)
    {
        slider.MaxValue = maxValue;
        slider.Value = 0;
    }

}
