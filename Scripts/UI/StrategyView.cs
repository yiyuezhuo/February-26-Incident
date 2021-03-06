namespace YYZ.App
{


using Godot;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// "Entry point", top UI logic and game logic which is not moved to proper location yet.
/// </summary>
public class StrategyView : Control
{
	[Export] ScenarioDataRes scenarioDataRes;
	[Export] NodePath mapViewPath;
	[Export] NodePath timePlayerPath;
	[Export] NodePath unitBarPath;
	[Export] NodePath stackBarPath;
	[Export] NodePath arrowButtonPath;
	[Export] NodePath artilleryFireButtonPath;
	[Export] NodePath suppressionButtonPath;
	[Export] NodePath strengthDetachDialogPath;
	[Export] NodePath infoLabelPath;
	[Export] NodePath phaseLabelPath;
	[Export] NodePath ceaseFireButtonPath;
	
	[Export] PackedScene mapImageScene;

	ScenarioData scenarioData;
	MapView mapView;
	MapShower mapShower;
	UnitBar unitBar;
	StackBar stackBar;
	StrengthDetachDialog strengthDetachDialog;
	Label infoLabel;
	Label phaseLabel;

	// States

	StrategyPad selectedPad;
	Dictionary<Unit, StrategyPad> padMap = new Dictionary<Unit, StrategyPad>();
	TransferRequest currentTransferRequest; // async refactoring?
	CreateRequest currentCreateRequest; // This request is not relevent to `currentTransferRequest`.

	Node mapContainer;
	Node arrowContainer;

	GameManager gameManager;

	public override void _Ready()
	{
		scenarioData = scenarioDataRes.GetInstance();
		mapView = (MapView)GetNode(mapViewPath);
		unitBar = (UnitBar)GetNode(unitBarPath);
		stackBar = (StackBar)GetNode(stackBarPath);
		strengthDetachDialog = (StrengthDetachDialog)GetNode(strengthDetachDialogPath);
		infoLabel = (Label)GetNode(infoLabelPath);
		phaseLabel = (Label)GetNode(phaseLabelPath);
		
		mapShower = (MapShower)mapView.GetMapShower();
		mapShower.areaClickEvent += OnAreaClick;
		mapShower.areaRightClickEvent += OnAreaRightClick;
		stackBar.clicked += OnStackUnitClick;
		stackBar.rightClicked += OnStackUnitRightClicked;
		strengthDetachDialog.confirmed += ApplyDetach;

		var timePlayer = (TimePlayer)GetNode(timePlayerPath);
		timePlayer.simulationEvent += SimulationHandler;

		var arrowButton = (Button)GetNode(arrowButtonPath);
		arrowButton.Connect("pressed", this, nameof(SoftSelectAllPads));

		var artilleryFireButton = (Button)GetNode(artilleryFireButtonPath);
		artilleryFireButton.Connect("pressed", this, nameof(OnArtilleryButtonPressed));

		var suppressButton = (Button)GetNode(suppressionButtonPath);
		suppressButton.Connect("pressed", this, nameof(OnSuppressButtonPressed));

		var ceaseFireButton = (Button)GetNode(ceaseFireButtonPath);
		ceaseFireButton.Connect("pressed", this, nameof(OnCeaseFireButtonPressed));

		arrowContainer = new Node();
		mapView.AddChild(arrowContainer);
		// Arrow's "z-index" is lower than other widgets.

		mapContainer = new Node();
		mapView.AddChild(mapContainer);

		// Non-binding setup
		
		foreach(var region in scenarioData.regions)
			region.childrenEntered += OnRegionChildrenEntered;

		foreach(var unit in scenarioData.units)
		{
			RegisterUnit(unit);
			unit.childrenEntered += OnUnitChildrenEntered;
		}

		foreach(var leader in scenarioData.leaders)
			RegisterLeader(leader);

		foreach(var region in scenarioData.regions)
		{
			var areaInfo = mapShower.GetAreaInfo(region);
			if(region.movable)
				areaInfo.foregroundColor = region.parent.color;
			else
				areaInfo.foregroundColor = new Color(0.6f, 0.6f, 1.0f, 1.0f); // river color workaround

			region.moved += OnRegionMoved;
		}
		mapShower.Flush();

		gameManager = new GameManager(scenarioData);

		infoLabel.Text = "";
	}

	void OnCeaseFireButtonPressed()
	{
		gameManager.GotoNextState();
		var text = gameManager.state.ToLabelString();
		ShowInfo($"Go to phase {text}");
		phaseLabel.Text = text;
	}

	void OnRegionMoved(object sender, Side sideOri)
	{
		var region = (Region)sender;
		var areaInfo = mapShower.GetAreaInfo(region);
		areaInfo.foregroundColor = region.parent.color;
	}

	/// <summary>
	/// If a leader is added before the unit is added to the "tree", the leader will not be registered automatically.
	/// </summary>
	void OnRegionChildrenEntered(object sender, Unit unit)
	{
		RegisterUnit(unit);
	}

	void OnUnitChildrenEntered(object sender, Leader leader)
	{
		RegisterLeader(leader);
	}

	/// <summary>
	/// In most time, the registry should be done by `EnterTo` through `OnUnitChildrenEntered`, once the `OnUnitChildrenEntered` is bound.
	/// </summary>
	void RegisterLeader(Leader leader)
	{
		// TODO: hack way to "register" leader only once in StrategyView layer.
		// https://stackoverflow.com/questions/136975/has-an-event-handler-already-been-added
		leader.destroyed -= OnLeaderDestroy;
		leader.destroyed += OnLeaderDestroy;
	}

	/// <summary>
	/// Add an army summoning button, by which 20,000 armies (20~40 units) enter the map 
	/// and try to "encompass" rebels (limit their movement).
	/// </summary>
	void OnSuppressButtonPressed()
	{
		GD.Print("OnSuppressButtonPressed");

		var regionSampled = scenarioData.mapData.SampleEdgeRegion(); // TODO: Move `SampleEdgeRegion` to GameManager
		var unit = new UnitSingleLeader("Government Leader", "", scenarioData.govSide.picture, 300, 500, scenarioData.govSide);
		unit.EnterTo(regionSampled);

		/*
		var agent = new RandomWalkingAgent(scenarioData, scenarioData.govSide);
		agent.Schedule(unit);
		*/
		GD.Print($"regionSampled={regionSampled}");
		gameManager.agent.Schedule(unit);
	}

	void OnLeaderDestroy(object sender, Leader leader)
	{
		if(selectedPad != null && leader.parent.Equals(selectedPad.unit))
		{
			unitBar.HardUpdate();
		}
		if(leader.important)
			ShowInfo(leader.ToDestoryString());
			// ShowInfo($"{leader.name} {leader.nameJap} is killed.");
	}

	void ShowInfo(string text) => infoLabel.Text = text;

	void OnArtilleryButtonPressed()
	{
		GD.Print("OnArtilleryButtonPressed");
		// Call an "instant" fire on selected unit.
		if(selectedPad != null)
		{
			selectedPad.unit.agent.TakeDirectFire(100f);

			// stackBar.SetData(selectedPad?.unit.parent);
			// unitBar.SetData(selectedPad?.unit);
			stackBar.SoftUpdate();
			unitBar.SoftUpdate();
			// stackBar.HardUpdate();
			// unitBar.HardUpdate();
		}
	}

	/// <summary>
	/// Create a StrategyPad and other wraps for a "bare" unit.
	///
	/// In most time, the registry should be done by `EnterTo` through `OnRegionChildrenEntered`, once the `OnRegionChildrenEntered` is bound.
	/// </summary>
	void RegisterUnit(Unit unit)
	{
		if(padMap.ContainsKey(unit))
			return;
		
		var pad = mapImageScene.Instance<StrategyPad>();
		pad.Setup(unit, arrowContainer, this);

		pad.clicked += OnUnitClick;

		padMap[unit] = pad;
		unit.destroyed += OnUnitDestroyed;
		unit.moveStateUpdated += OnUnitMoveEvent;
		unit.childrenEntered += OnUnitChildrenEntered;
		
		mapContainer.AddChild(pad);

		// "try" to register leaders
		foreach(var leader in unit.children)
			RegisterLeader(leader);
	}

	void OnUnitDestroyed(object sender, Unit unit)
	{
		var region = unit.parent;

		if(selectedPad != null)
		{
			if(unit.Equals(selectedPad.unit))
			{
				selectedPad = null;
				stackBar.SetData(null);
				unitBar.SetData(null);
			}
			else if(selectedPad.unit.parent.Equals(region))
			{
				// stackBar.SetData(unit.parent);
				stackBar.HardUpdate();
			}
		}
		padMap.Remove(unit);

		if(!region.IsEmpty() && region.IsUnique() && !region.children[0].side.Equals(region.parent))
			region.MoveTo(region.children[0].side);
			
	}

	void OnUnitMoveEvent(object sender, Unit.MovePath path)
	{
		var unit = (Unit)sender;
		foreach(var region in path.reachedRegions)
			if(region.IsConsistentWith(unit.side))
				region.MoveTo(unit.side);

		var pad = padMap[unit];
		if(selectedPad != null && selectedPad.Equals(pad))
		{
			stackBar.SetData(unit.parent);
			SetStackUnitFocus(unit);
		}
	}

	void OnStackUnitClick(object sender, int idx)
	{
		GD.Print($"OnStackUnitClick {idx}");

		var unit = selectedPad.unit.parent.children[idx];

		SoftDeselectSelectedPad();
		SelectPad(padMap[unit]);
		SetStackUnitFocus(unit);
	}

	void SimulationHandler(object sender, int n)
	{
		for(var i=0; i<n; i++)
			gameManager.SimulationStep();
			// SimulationStep();
		
		mapShower.Flush(); // GoForward may trigger some handlers to call areaInfo, so we flush at every

		stackBar.SoftUpdate();
		unitBar.SoftUpdate();
	}

	void OnAreaClick(object sender, Region area)
	{
		GD.Print($"StrategyView.OnAreaClick {area}");
		ForceDeselectAllSelection(); // TODO: Add a option to disable this behavior?
	}

	/// <summary>
	/// If any leaders are selected, a detaching unit starts constructing (it will be built after strength assignment),
	/// otherwise, original unit will be selected.
	/// </summary>
	Unit TryDetachTo(Region area)
	{
		var detachRequest = GetDetachRequest();
		if(detachRequest.selectedLeaderList.Count == 0 || detachRequest.strengthDetermined)
		{
			return selectedPad.unit;
		} 

		currentCreateRequest = new CreateRequest(detachRequest, area);
		AskDetachedStrength(detachRequest.src.strength);

		return null;
	}

	void CreateUnit(CreateRequest createRequest)
	{
		var unit = createRequest.Apply();

		RegisterUnit(unit);
		// scenarioData.RegisterUnit(unit); // TODO: Factory hooker refactor?

		PointUnitTo(unit, createRequest.dstArea);

		stackBar.SetData(unit.parent);
		SelectPad(padMap[unit]);
	}

	void OnAreaRightClick(object sender, Region area)
	{
		GD.Print($"StrategyView.OnAreaRightClick {area}");
		if(selectedPad != null && selectedPad.unit.side.Equals(scenarioData.rebelSide)) // We may disable rebelSize check for debugging purpose.
		{
			var unit = TryDetachTo(area);
			if(unit != null)
				PointUnitTo(unit, area);
		}
	}

	void PointUnitTo(Unit unit, Region area)
	{
		if(unit.parent.Equals(area)) // cancel movement when right click to area that unit lived in.
		{
			unit.movingState.Reset();
		}
		else
		{
			var cacheHit = unit.movingState.active && unit.movingState.destination.Equals(area);
			if(!cacheHit)
			{
				var extendsRequest = unit.movingState.active && Input.IsActionPressed("shift");
				var src = extendsRequest ? unit.movingState.destination : unit.parent;
				var path = PathFinding.PathFinding<Region>.AStar(gameManager.GetGraph(), src, area);

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
	void OnUnitClick(object sender, EventArgs _)
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

	DetachRequest GetDetachRequest()
	{
		var srcUnit = selectedPad.unit;

		var highlightList = unitBar.GetHighlightedStates();
		var selectedLeaderList = new List<Leader>();

		for(int i=0; i<srcUnit.children.Count; i++)
			if(highlightList[i]) // FIXME: Sometimes srcUnit is not synced with UI states? If happend again, check strength and whether it's "destroyed". Godot's thread system lacks document though.
				selectedLeaderList.Add(srcUnit.children[i]);

		return new DetachRequest(srcUnit, selectedLeaderList);
	}

	void OnStackUnitRightClicked(object sender, int idx)
	{
		// merge unit

		var detachRequest = GetDetachRequest();

		var dstUnit = selectedPad.unit.parent.children[idx];
		currentTransferRequest = new TransferRequest(detachRequest, dstUnit);

		if(!detachRequest.strengthDetermined)
		{
			AskDetachedStrength(detachRequest.src.strength);
		}
		else
		{
			TransferPower(currentTransferRequest);
			currentTransferRequest = null;
		}
	}

	void AskDetachedStrength(float strength)
	{
		strengthDetachDialog.Popup_();
		strengthDetachDialog.Setup(strength);
	}

	void ApplyDetach(object sender, float value)
	{
		GD.Print($"ApplyDetach {value}");
		
		if(currentTransferRequest != null)
		{
			currentTransferRequest.detachRequest.strength = value;
			TransferPower(currentTransferRequest);
			currentTransferRequest = null;
		}
		else if(currentCreateRequest != null)
		{
			currentCreateRequest.detachRequest.strength = value;
			CreateUnit(currentCreateRequest);
			currentCreateRequest = null;
		}
	}

	void TransferPower(TransferRequest request)
	{
		request.Apply();

		var src = request.detachRequest.src;
		var dst = request.dst;

		stackBar.SetData(dst.parent); // force update.
		if(src.isDestroying)
			SelectPad(padMap[dst]);
		else
			SelectPad(padMap[src]);
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
	StrategyPad ToggleStack(Region region)
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
		// Weird hack, but it seems that Godot can't propagate event into _unhandled_input level after it's accepted in _GuiInput phase:
		// https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html
		// We can set mouse filter to `ignore` for every intermedia controls. But since we need StrategyPad and MapView both to accept events,
		// the ignore way does not work.
	}
}


}
