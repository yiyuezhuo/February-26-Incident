namespace YYZ.App
{

using Godot;
// using YYZ.Data.February26;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StrategyView : Control
{
	[Export] ScenarioDataRes scenarioDataRes;
	[Export] NodePath mapViewPath;
	[Export] NodePath timePlayerPath;
	[Export] NodePath nameViewButtonPath;
	[Export] NodePath unitBarPath;
	// [Export] NodePath debugProgressLongArrowPath;
	
	[Export] PackedScene mapImageScene;

	[Export] float pixelDistance = 6; // 6m = 1 pixel distance.

	ScenarioData scenarioData;
	MapView mapView;
	MapShower mapShower;
	TimePlayer timePlayer;
	UnitBar unitBar;

	// States

	StrategyPad selectedPad;
	List<StrategyPad> pads = new List<StrategyPad>();
	bool showRegionName = false;
	List<Label> regionNameLabels = new List<Label>();

	// MapKit.Widgets.ProgressLongArrow debugProgressLongArrow;
	// float debugPercentAcc = 0f;

	// MapKit.Widgets.ProgressLongArrow focusedProgressLongArrow;

	// HashSet<MapKit.Widgets.ProgressLongArrow> trackedArrows = new HashSet<MapKit.Widgets.ProgressLongArrow>();

	public override void _Ready()
	{
		scenarioData = scenarioDataRes.GetInstance();
		mapView = (MapView)GetNode(mapViewPath);
		timePlayer = (TimePlayer)GetNode(timePlayerPath);
		unitBar = (UnitBar)GetNode(unitBarPath);

		timePlayer.simulationEvent += SimulationHandler;
		mapShower = (MapShower)mapView.GetMapShower();
		mapShower.areaClickEvent += OnAreaClick;
		mapShower.areaRightClickEvent += OnAreaRightClick;

		var nameViewButton = (Button)GetNode(nameViewButtonPath);
		nameViewButton.Connect("pressed", this, nameof(OnNameViewButtonPressed));

		// Non-binding setup

		foreach(var unit in scenarioData.units)
		{
			var pad = mapImageScene.Instance<StrategyPad>();
			pad.arrowContainer = mapView;
			// var node = new TextureRect();
			pad.RectPosition = unit.parent.center;
			pad.Texture = unit.children[0].portrait;
			pad.unit = unit;

			pad.unitSelectedUpdateEvent += OnUnitSelectedUpdate;

			// GD.Print($"node={node}");
			pads.Add(pad);
			mapView.AddChild(pad);
		}

		foreach(var region in scenarioData.regions)
		{
			var areaInfo = mapShower.GetAreaInfo(region);
			if(region.movable)
				areaInfo.foregroundColor = region.parent.color;
			else
				areaInfo.foregroundColor = new Color(0.6f, 0.6f, 1.0f, 1.0f); // river color workaround
		}
		mapShower.Flush();
	}

	public void OnNameViewButtonPressed()
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
				mapView.AddChild(label);
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

	public void SimulationHandler(object sender, int n)
	{
		for(var i=0; i<n; i++)
			SimulationStep();
	}

	public void SimulationStep() // 1 min -> 1 call
	{
		// debugPercentAcc += 0.01f;
		foreach(var pad in pads)
		{
			if(pad.unit.isMoving)
			{
				pad.GoForward(pad.unit.moveSpeedPiexelPerMin, out var reachedRegions);
				foreach(var region in reachedRegions)
				{
					var areaInfo = mapShower.GetAreaInfo(region);
					areaInfo.foregroundColor = pad.unit.side.color;
				}
			}
		}
		mapShower.Flush();
	}

	public override void _PhysicsProcess(float delta)
	{
		// debugProgressLongArrow.SetPercent(debugPercentAcc);
		foreach(var pad in pads)
		{
			pad.SyncArrowPercent();
		}
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
			var unit = selectedPad.unit;

			if(unit.parent.Equals(area)) // cancel movement when right click to area that unit lived in.
			{
				unit.movingState = null; // Then SyncArrow will destory arrow if it existed.
			}
			else
			{
				var cacheHit = unit.movingState != null && unit.movingState.path[unit.movingState.path.Count-1].Equals(area);
				if(!cacheHit)
				{
					var extendsRequest = unit.movingState != null && Input.IsActionPressed("shift");

					var pathfinding = new PathFinding.PathFinding<Region>(scenarioData.mapData);

					var src = extendsRequest ? unit.movingState.destination : unit.parent;

					var path = pathfinding.PathFindingAStar(src, area);

					if(path.Count == 0)
						return; // don't update arrow

					var movingState = new MovingState(path);

					if(extendsRequest)
						unit.movingState.Extends(movingState);
					else
						unit.movingState = movingState;
				}
			}

			GD.Print($"movingState={unit.movingState}");

			selectedPad.SyncArrowShape();
		}
	}

	/// <summary>
	/// The handler is called when "selected" state of a Unit is updated.
	/// </summary>
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

			unitBar.SetData(pad.unit);
		}
		else
		{
			selectedPad = pad;

			unitBar.SetData(null);
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
