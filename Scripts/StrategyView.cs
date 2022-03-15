namespace YYZ.App
{

using Godot;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class StrategyView : Control
{
	[Export] ScenarioDataRes scenarioDataRes;
	[Export] NodePath mapViewPath;
	[Export] NodePath timePlayerPath;
	[Export] NodePath nameViewButtonPath;
	[Export] NodePath unitBarPath;
	[Export] NodePath arrowButtonPath;
	
	[Export] PackedScene mapImageScene;

	[Export] float pixelDistance = 6; // 6m = 1 pixel distance.

	ScenarioData scenarioData;
	MapView mapView;
	MapShower mapShower;
	TimePlayer timePlayer;
	UnitBar unitBar;

	// States

	StrategyPad selectedPad;
	// List<StrategyPad> pads = new List<StrategyPad>();
	Dictionary<Unit, StrategyPad> padMap = new Dictionary<Unit, StrategyPad>();
	bool showRegionName = false;
	List<Label> regionNameLabels = new List<Label>();

	Node mapContainer; //{get => mapView;}
	Node arrowContainer; //{get => mapView;}

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

		var arrowButton = (Button)GetNode(arrowButtonPath);
		arrowButton.Connect("pressed", this, nameof(SoftSelectAllPads));

		arrowContainer = new Node();
		mapView.AddChild(arrowContainer);
		// So arrow's "z-index" is lowerer than other widgets.
		mapContainer = new Node();
		mapView.AddChild(mapContainer);

		// Non-binding setup

		foreach(var unit in scenarioData.units)
		{
			var pad = mapImageScene.Instance<StrategyPad>();
			pad.arrowContainer = arrowContainer;
			pad.RectPosition = unit.parent.center;
			pad.Texture = unit.children[0].portrait;
			pad.unit = unit;

			pad.unitClickEvent += OnUnitClick;

			padMap[unit] = pad;
			mapContainer.AddChild(pad);
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

	public void SimulationHandler(object sender, int n)
	{
		for(var i=0; i<n; i++)
			SimulationStep();
	}

	public void SimulationStep() // 1 min -> 1 call
	{
		foreach(var pad in padMap.Values)
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
		foreach(var pad in padMap.Values)
		{
			pad.SyncArrowPercent();
		}
	}

	public void OnAreaClick(object sender, Region area)
	{
		// selectedPad.SetSelected(false);
		GD.Print($"StrategyView.OnAreaClick {area}");
		ForceDeselectAllSelection(); // TODO: Add a option to disable this behavior?
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

	void SoftDeselectSelectedPad()
	{
		selectedPad.selectionState = StrategyPad.SelectionState.SoftSelected;
		selectedPad.OnSelectionStateUpdated();
		selectedPad = null;
	}

	void SelectPad(StrategyPad pad)
	{
		if(!(selectedPad is null))
			SoftDeselectSelectedPad();

		pad.selectionState = StrategyPad.SelectionState.Selected;
		pad.OnSelectionStateUpdated();

		unitBar.SetData(pad.unit);

		selectedPad = pad;
	}

	/// <summary>
	/// Transform states of "Selected" and "SoftSelected" to "Deselected".
	/// </summary>
	void ForceDeselectAllSelection()
	{
		foreach(var pad in padMap.Values)
		{
			pad.TryForceDeselect();
		}
	}

	void SoftSelectAllPads()
	{
		foreach(var pad in padMap.Values)
			pad.TrySoftSelect();
	}

	/// <summary>
	/// The handler is called when "selected" state of a Unit is updated.
	/// </summary>
	public void OnUnitClick(object sender, EventArgs _)
	{
		var pad = (StrategyPad)sender; // for testing usage of sender, may refactor it later.

		GD.Print($"StrategyView {nameof(OnUnitClick)}: {pad}, {pad.unit}");

		if(pad.selectionState == StrategyPad.SelectionState.Selected && pad.unit.parent.children.Count >= 2)
		{
			SoftDeselectSelectedPad();
			var nextTopPad = ToggleStack(pad.unit.parent);
			SelectPad(nextTopPad);
		}
		else
		{
			SelectPad(pad);
		}
	}

	/// <summary>
	/// Move Top StrategyPad to bottom and return new top StrategyPad.
	/// The size of stack is expected to be >= 2.
	/// </summary>
	public StrategyPad ToggleStack(Region region)
	{
		var pads = (from unit in region.children select padMap[unit]).ToList();
		pads.Sort((x, y) => x.GetIndex().CompareTo(y.GetIndex()));

		var maxPad = pads[pads.Count-1];
		var minPad = pads[0];
		mapContainer.MoveChild(maxPad, minPad.GetIndex());

		return pads[pads.Count-2];
	}
}


}
