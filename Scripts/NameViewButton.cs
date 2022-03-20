namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class NameViewButton : Button
{
    [Export] ScenarioDataRes scenarioDataRes;
    [Export] NodePath mapContainerPath;

    ScenarioData scenarioData;
    Node mapContainer;

    List<Label> regionNameLabels = new List<Label>();
    bool showRegionName = false;

    public override void _Ready()
    {
        scenarioData = scenarioDataRes.GetInstance();
        mapContainer = GetNode(mapContainerPath);

        Connect("pressed", this, nameof(OnNameViewButtonPressed));
    }

	void OnNameViewButtonPressed()
	{
		showRegionName = !showRegionName;
		if(showRegionName)
		{
			foreach(var region in scenarioData.regions)
			{
				var label = new Label();
				label.Text = region.ToLabelString();
				
				label.AddColorOverride("font_color", new Color(0, 0, 0, 1));
				label.Align = Label.AlignEnum.Center;

				regionNameLabels.Add(label);
				mapContainer.AddChild(label);
				label.RectPosition = region.center - label.RectSize / 2; // RectSize get desired value when it actually enter the tree.
			}
		}
		else
		{
			foreach(var label in regionNameLabels)
			{
				label.QueueFree(); // TODO: Object Pool seems overkill for this.
			}
			regionNameLabels.Clear();
		}
	}
}


}