using Godot;
using System;

public class TimePlayer : Control
{
    [Export] float baseTimeStep = 60f; // x1 time. The default value is 60s game second per 1 elapsed real second
    // [Export] float step = 60f; // 60s game second -> 1 non-physics/animation update/step (UI update, fire result...)
    [Export] string startDate = "2/26/2008 3:00:00 AM";
    [Export] NodePath timeLabelPath;
    [Export] NodePath pauseButtonPath; // x0Button
    [Export] NodePath x1ButtonPath;
    [Export] NodePath x2ButtonPath;
    [Export] NodePath x4ButtonPath;
    [Export] NodePath x8ButtonPath;
    [Export] NodePath x16ButtonPath;
    [Export] NodePath x32ButtonPath;

    Label timeLabel;
    Button pauseButton;
    Button x1Button;
    Button x2Button;
    Button x4Button;
    Button x8Button;
    Button x16Button;
    Button x32Button;

    float multipler = 0.0f;
    // float elapsed = 0.0f;
    DateTime lastDate;

    public event EventHandler<int> simulationEvent;

    public override void _Ready()
    {
        timeLabel = (Label)GetNode(timeLabelPath);

        pauseButton = BindButton(pauseButtonPath, 0.0f);
        x1Button = BindButton(x1ButtonPath, 1.0f);
        x2Button = BindButton(x2ButtonPath, 2.0f);
        x4Button = BindButton(x4ButtonPath, 4.0f);
        x8Button = BindButton(x8ButtonPath, 8.0f);
        x16Button = BindButton(x16ButtonPath, 16.0f);
        x32Button = BindButton(x32ButtonPath, 32.0f);

        lastDate = DateTime.Parse(startDate);
    }

    Godot.Collections.Array GetBindParam(float t) => new Godot.Collections.Array{t};
    Button BindButton(string buttonPath, float t)
    {
        var button = (Button)GetNode(buttonPath);
        button.Connect("pressed", this, nameof(SetMultipler), GetBindParam(t));
        return button;
    }

    public override void _Process(float delta)
    {
        var elapsedSeconds = delta * baseTimeStep * multipler;
        var date = lastDate.AddSeconds(elapsedSeconds);
        var dm = date.Ticks % TimeSpan.TicksPerMinute - lastDate.Ticks % TimeSpan.TicksPerMinute;
        if(dm > 0)
        {
            var trimedDate = new DateTime(date.Ticks - date.Ticks % TimeSpan.TicksPerMinute, date.Kind);
            timeLabel.Text = trimedDate.ToString(); // ToString(System.Globalization.CultureInfo.InvariantCulture)
            simulationEvent?.Invoke(this, (int)dm); // TODO: check Long to Int conversion?
        }
        lastDate = date;
    }

    public void SetMultipler(float multipler)
    {
        this.multipler = multipler;
    }
}
