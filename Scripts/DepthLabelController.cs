namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class DepthLabelController : Node
{
    [Export] ScenarioDataRes scenarioDataRes;
    [Export] NodePath mapContainerPath;

    ScenarioData scenarioData;
    Node mapContainer;

    Dictionary<Region, Label> depthLabelMap = new Dictionary<Region, Label>();

    public override void _Ready()
    {
        scenarioData = scenarioDataRes.GetInstance();
        mapContainer = GetNode(mapContainerPath);

		foreach(var region in scenarioData.regions)
        {
			UpdateStackDepthLabel(region);
            region.childrenUpdated += OnRegionChildrenUpdated;
        }

    }

	void UpdateStackDepthLabel(Region region)
	{
		if(depthLabelMap.TryGetValue(region, out var depthLabel))
		{
			if(region.children.Count == 0)
			{
				depthLabel.QueueFree();
				depthLabelMap.Remove(region);
			}
			else
			{
				depthLabel.Text = region.children.Count.ToString();
			}
		}
		else if(region.children.Count > 0)
		{
			depthLabel = new Label();
			depthLabel.Text = region.children.Count.ToString();
			depthLabel.RectPosition = region.center + new Vector2(0, 50f); // TODO: introduce condif instead of hard-coded offset.
			mapContainer.AddChild(depthLabel);
			depthLabelMap[region] = depthLabel;
		}
	}

	void OnRegionChildrenUpdated(object sender, Region region)
	{
		UpdateStackDepthLabel(region);
	}

}


}