namespace YYZ.App
{

using Godot;
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
	[Export] NodePath stackBarPath;
	[Export] NodePath arrowButtonPath;
	
	[Export] PackedScene mapImageScene;

	ScenarioData scenarioData;
	MapView mapView;
	MapShower mapShower;
	TimePlayer timePlayer;
	UnitBar unitBar;
	StackBar stackBar;

	// States

	StrategyPad selectedPad;
	Dictionary<Unit, StrategyPad> padMap = new Dictionary<Unit, StrategyPad>();
	Dictionary<Region, Label> depthLabelMap = new Dictionary<Region, Label>();
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
		stackBar = (StackBar)GetNode(stackBarPath);

		timePlayer.simulationEvent += SimulationHandler;
		mapShower = (MapShower)mapView.GetMapShower();
		mapShower.areaClickEvent += OnAreaClick;
		mapShower.areaRightClickEvent += OnAreaRightClick;
		stackBar.unitClickEvent += OnStackUnitClick;

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
			pad.propogateTo = this; // Hack Godot's broken Event propogation system.

			unit.movingState.updated += pad.OnMovingStateUpdated;
			pad.unitClickEvent += OnUnitClick;
			unit.moveEvent += pad.OnUnitMoveEvent;
			unit.moveEvent += OnUnitMoveEvent;

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

		foreach(var region in scenarioData.regions)
			UpdateStackDepthLabel(region);
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

	public void OnUnitMoveEvent(object sender, Unit.MovePath path)
	{
		var unit = (Unit)sender;
		foreach(var region in path.reachedRegions)
		{
			region.MoveTo(unit.side);

			var areaInfo = mapShower.GetAreaInfo(region);
			areaInfo.foregroundColor = unit.side.color;
		}

		// Add stack depth indicator updates.
		UpdateStackDepthLabel(path.src);
		UpdateStackDepthLabel(path.dst);

		var pad = padMap[unit];
		if(selectedPad != null && selectedPad.Equals(pad))
		{
			stackBar.SetData(pad.unit.parent);
			SetStackUnitFocus(pad.unit);
		}
	}

	public void OnStackUnitClick(object sender, int idx)
	{
		GD.Print($"OnStackUnitClick {idx}");

		var unit = selectedPad.unit.parent.children[idx];

		SoftDeselectSelectedPad();
		SelectPad(padMap[unit]);
		SetStackUnitFocus(unit);
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
		mapShower.Flush(); // GoForward may trigger some handlers to call areaInfo, so we flush at every
	}

	public void SimulationStep() // 1 min -> 1 call
	{
		foreach(var pad in padMap.Values)
		{
			if(pad.unit.movingState.active)
			{
				pad.unit.GoForward();
			}
		}
	}

	public void OnAreaClick(object sender, Region area)
	{
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
				unit.movingState.Reset(); // Then SyncArrow will destory arrow if it existed.
			}
			else
			{
				var cacheHit = unit.movingState.active && unit.movingState.destination.Equals(area);
				if(!cacheHit)
				{
					var extendsRequest = unit.movingState.active && Input.IsActionPressed("shift");
					var pathfinding = new PathFinding.PathFinding<Region>(scenarioData.mapData);
					var src = extendsRequest ? unit.movingState.destination : unit.parent;
					var path = pathfinding.PathFindingAStar(src, area);

					if(path.Count == 0)
						return; // don't update arrow

					if(extendsRequest)
						unit.movingState.Extends(path);
					else
						unit.movingState.ResetToPath(path);
				}
			}

			GD.Print($"movingState={unit.movingState}");
		}
	}

	void SoftDeselectSelectedPad()
	{
		selectedPad.selectionState = StrategyPad.SelectionState.SoftSelected;
		selectedPad.TrySetArrowAlpha();
		selectedPad = null;
	}

	void SelectPad(StrategyPad pad)
	{
		if(!(selectedPad is null))
			SoftDeselectSelectedPad();

		pad.selectionState = StrategyPad.SelectionState.Selected;

		unitBar.SetData(pad.unit);
		pad.TrySetArrowAlpha();

		selectedPad = pad;
	}

	/// <summary>
	/// Transform states of "Selected" and "SoftSelected" to "Deselected".
	/// </summary>
	void ForceDeselectAllSelection()
	{
		foreach(var pad in padMap.Values)
			pad.TryForceDeselect();
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
			pad = ToggleStack(pad.unit.parent); // next pad
			SelectPad(pad);
		}
		else
		{
			var stackChanged = selectedPad is null || !pad.unit.parent.Equals(selectedPad.unit.parent);
			
			SelectPad(pad);

			if(stackChanged)
				stackBar.SetData(pad.unit.parent);
		}

		SetStackUnitFocus(pad.unit);
	}

	void SetStackUnitFocus(Unit unit)
	{
		var idx = unit.parent.children.IndexOf(unit);
		stackBar.SetFocus(idx);	
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

	public override void _GuiInput(InputEvent @event)
	{
		mapView._UnhandledInput(@event); 
		// Weird hack, but it seems that Godot can't propogate event into _unhandled_input level after it's accepted in _GuiInput phase:
		// https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html
		// We can set mouse filter to `ignore` for every intermedia controls. But since we need StrategyPad and MapView both to accept events,
		// the ignore way does not work.
	}
}


}
