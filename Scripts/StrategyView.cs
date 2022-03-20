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
	[Export] NodePath strengthDetachDialogPath;
	
	[Export] PackedScene mapImageScene;

	ScenarioData scenarioData;
	MapView mapView;
	MapShower mapShower;
	TimePlayer timePlayer;
	UnitBar unitBar;
	StackBar stackBar;
	StrengthDetachDialog strengthDetachDialog;

	// States

	StrategyPad selectedPad;
	Dictionary<Unit, StrategyPad> padMap = new Dictionary<Unit, StrategyPad>();
	Dictionary<Region, Label> depthLabelMap = new Dictionary<Region, Label>();
	bool showRegionName = false;
	List<Label> regionNameLabels = new List<Label>();
	TransferRequest currentTransferRequest;
	CreateRequest currentCreateRequest; // This request is not relevent to `currentTransferRequest`.

	Node mapContainer; //{get => mapView;}
	Node arrowContainer; //{get => mapView;}

	public override void _Ready()
	{
		scenarioData = scenarioDataRes.GetInstance();
		mapView = (MapView)GetNode(mapViewPath);
		timePlayer = (TimePlayer)GetNode(timePlayerPath);
		unitBar = (UnitBar)GetNode(unitBarPath);
		stackBar = (StackBar)GetNode(stackBarPath);
		strengthDetachDialog = (StrengthDetachDialog)GetNode(strengthDetachDialogPath);

		timePlayer.simulationEvent += SimulationHandler;
		mapShower = (MapShower)mapView.GetMapShower();
		mapShower.areaClickEvent += OnAreaClick;
		mapShower.areaRightClickEvent += OnAreaRightClick;
		stackBar.clicked += OnStackUnitClick;
		stackBar.rightClicked += OnStackUnitRightClicked;
		strengthDetachDialog.confirmed += ApplyDetach;

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
			CreateStrategyPad(unit);
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

	/// <summary>
	/// Create a StrategyPad and other wraps for a "bare" unit.
	/// </summary>
	void CreateStrategyPad(Unit unit)
	{
		var pad = mapImageScene.Instance<StrategyPad>();
		pad.Setup(unit, arrowContainer, this);

		pad.unitClickEvent += OnUnitClick;

		padMap[unit] = pad;
		unit.destroyed += OnUnitDestroyed;
		unit.moveStateUpdated += OnUnitMoveEvent;
		
		mapContainer.AddChild(pad);
	}

	void OnUnitDestroyed(object sender, Unit unit)
	{
		// padMap[unit].QueueFree();
		padMap.Remove(unit);
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

	void OnUnitMoveEvent(object sender, Unit.MovePath path)
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

	void OnStackUnitClick(object sender, int idx)
	{
		GD.Print($"OnStackUnitClick {idx}");

		var unit = selectedPad.unit.parent.children[idx];

		SoftDeselectSelectedPad();
		SelectPad(padMap[unit]);
		SetStackUnitFocus(unit);
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

	void SimulationHandler(object sender, int n)
	{
		for(var i=0; i<n; i++)
			SimulationStep();
		mapShower.Flush(); // GoForward may trigger some handlers to call areaInfo, so we flush at every
	}

	void SimulationStep() // 1 min -> 1 call
	{
		foreach(var pad in padMap.Values)
		{
			if(pad.unit.movingState.active)
			{
				pad.unit.GoForward();
			}
		}
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
		var src = createRequest.detachRequest.src;

		var unit = new UnitProcedure(src.side, 0, 0);
		unit.EnterTo(src.parent);
		var transferRequest = new TransferRequest(createRequest.detachRequest, unit);
		TransferPower(transferRequest);

		CreateStrategyPad(unit);
		scenarioData.RegisterUnit(unit); // TODO: Factory hooker refactor?

		PointUnitTo(unit, createRequest.dstArea);
		SelectPad(padMap[unit]);

		UpdateStackDepthLabel(unit.parent);
	}

	void OnAreaRightClick(object sender, Region area)
	{
		GD.Print($"StrategyView.OnAreaRightClick {area}");
		if(selectedPad != null)
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

	class DetachRequest
	{
		public Unit src;
		public List<Leader> selectedLeaderList;
		public DetachRequest(Unit src, List<Leader> selectedLeaderList)
		{
			this.src = src;
			this.selectedLeaderList = selectedLeaderList;
		}

		bool isFullDetach{get => src.children.Count == selectedLeaderList.Count;}
		float? strengthSuggested;
		public bool strengthDetermined{get =>  isFullDetach || strengthSuggested != null;}
		public float strength
		{
			get => isFullDetach ? src.strength : strengthSuggested.Value;
			set => strengthSuggested = value;
		}
	}

	class TransferRequest
	{
		public DetachRequest detachRequest;
		public Unit dst;

		public TransferRequest(DetachRequest detachRequest, Unit dst)
		{
			this.detachRequest = detachRequest;
			this.dst = dst;
		}
	}

	class CreateRequest
	{
		public DetachRequest detachRequest;
		public Region dstArea;

		public CreateRequest(DetachRequest detachRequest, Region dstArea)
		{
			this.detachRequest = detachRequest;
			this.dstArea = dstArea;
		}
	}

	DetachRequest GetDetachRequest()
	{
		var srcUnit = selectedPad.unit;

		var highlightList = unitBar.GetHighlightedStates();
		var selectedLeaderList = new List<Leader>();

		for(int i=0; i<srcUnit.children.Count; i++)
			if(highlightList[i])
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

		// GD.Print($"currentTransferRequest={currentTransferRequest}, currentCreateRequest={currentCreateRequest}");
	}

	void TransferPower(TransferRequest request)
	{
		var src = request.detachRequest.src;
		var selectedLeaderList = request.detachRequest.selectedLeaderList;
		var strength = request.detachRequest.strength;
		var dst = request.dst;

		foreach(var leader in selectedLeaderList)
		{
			leader.MoveTo(dst);
		}
		
		var dn = dst.strength + dst.children.Count;
		var sn = strength + selectedLeaderList.Count;
		dst.fatigue = ((dn * dst.fatigue) + (sn * src.fatigue)) / (dn + sn);
		
		dst.strength += strength;
		src.strength -= strength;

		if(src.children.Count == 0 && src.strength == 0)
		{
			src.Destroy();
			UpdateStackDepthLabel(dst.parent);
		}

		stackBar.SetData(dst.parent);
		if(src.isDestroyed)
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
		// Weird hack, but it seems that Godot can't propogate event into _unhandled_input level after it's accepted in _GuiInput phase:
		// https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html
		// We can set mouse filter to `ignore` for every intermedia controls. But since we need StrategyPad and MapView both to accept events,
		// the ignore way does not work.
	}
}


}
