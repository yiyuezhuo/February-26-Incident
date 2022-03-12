namespace YYZ.App
{

using Godot;
using YYZ.Data.February26;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StrategyView : Control
{
	[Export] ScenarioDataRes scenarioDataRes;
	[Export] NodePath mapViewPath;
	[Export] NodePath timePlayerPath;
	// [Export] NodePath debugProgressLongArrowPath;
	[Export] PackedScene progressLongArrowScene;
	[Export] PackedScene mapImageScene;

	ScenarioData scenarioData;
	MapView mapView;
	MapShower mapShower;
	TimePlayer timePlayer;

	StrategyPad selectedPad;

	MapKit.Widgets.ProgressLongArrow debugProgressLongArrow;
	float debugPercentAcc = 0f;

	public override void _Ready()
	{
		scenarioData = scenarioDataRes.GetInstance();
		mapView = (MapView)GetNode(mapViewPath);
		timePlayer = (TimePlayer)GetNode(timePlayerPath);

		foreach(var unit in scenarioData.units)
		{
			var pad = mapImageScene.Instance<StrategyPad>();
			// var node = new TextureRect();
			pad.RectPosition = scenarioData.mapData.MapToWorld(unit.parent.center);
			pad.Texture = unit.children[0].portrait;
			pad.unit = unit;

			pad.unitSelectedUpdateEvent += OnUnitSelectedUpdate;

			// GD.Print($"node={node}");

			mapView.AddChild(pad);
		}

		timePlayer.simulationEvent += SimulationHandler;
		mapShower = (MapShower)mapView.GetMapShower();
		mapShower.areaClickEvent += OnAreaClick;
		mapShower.areaRightClickEvent += OnAreaRightClick;

		// DEBUG
		// debugProgressLongArrow = (MapKit.Widgets.ProgressLongArrow)GetNode(debugProgressLongArrowPath);
		debugProgressLongArrow = progressLongArrowScene.Instance<MapKit.Widgets.ProgressLongArrow>();
		mapView.AddChild(debugProgressLongArrow);
		debugProgressLongArrow.SetCurvePositions(new Vector2[]{new Vector2(100f, 100f), new Vector2(500f, 300f)});
		debugProgressLongArrow.SetPercent(debugPercentAcc);
	}

	public void SimulationHandler(object sender, int n)
	{
		for(var i=0; i<n; i++)
			SimulationStep();
	}

	public void SimulationStep() // 1 min -> 1 call
	{
		debugPercentAcc += 0.01f;
		// debugProgressLongArrow.SetPercent(debugPercentAcc);
	}

	public override void _PhysicsProcess(float delta)
	{
		debugProgressLongArrow.SetPercent(debugPercentAcc);
	}

	public void OnAreaClick(object sender, Region area)
	{
		GD.Print($"StrategyView.OnAreaClick {area}");
	}

	public void OnAreaRightClick(object sender, Region area)
	{
		GD.Print($"StrategyView.OnAreaRightClick {area}");
		if(selectedPad != null)
		{
			var src = selectedPad.unit.parent;
			var pathfinding = new PathFinding.PathFinding<Region>(scenarioData.mapData);
			var path = pathfinding.PathFindingAStar(src, area);
			
			GD.Print($"{path}|{path.Count}|" + string.Join(", ", path));

			var controlPoints = path.Select(x => scenarioData.mapData.MapToWorld(x.center));

			var arrow  = progressLongArrowScene.Instance<MapKit.Widgets.ProgressLongArrow>();
			mapView.AddChild(arrow);
			arrow.SetCurvePositions(controlPoints);
			arrow.SetPercent(0.5f);
		}
	}

	public void OnUnitSelectedUpdate(object sender, bool selected)
	{
		var pad = (StrategyPad)sender; // for testing usage of sender, may refactor it later.

		GD.Print($"StrategyView.OnUnitSelectedUpdate {pad}, {pad.unit}, {selected}");

		if(selected)
		{
			if(selectedPad != null)
			{
				selectedPad.SetSelected(false); 
			}
			pad.SetSelected(true);
			selectedPad = pad;
		}
		else
		{
			selectedPad = pad;
		}
	}

	void TryClearSelectedPad()
	{
		if(selectedPad != null)
		{
			selectedPad.SetSelected(false);
			selectedPad = null;
		}

	}
}


}
