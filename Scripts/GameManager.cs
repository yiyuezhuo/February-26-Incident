namespace YYZ.App
{


using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Visitor Patten for double dispatch.
/// However, since externel container can only contain IVistor, generic virtual method (like some methods from State) should be listed here as well.
/// Though Visit methods are public, they *cannot* be accessed from externel. Only Accept can be accessed.
///
/// stateFrom.Accept(stateTo)
///
/// </summary>
public interface IVisitor // IVistor should be 
{
	void Visit(AssaultState state);
	void Visit(CeaseFireState state);
	void Visit(CombatState state);
}

public interface IState : IVisitor, IStepConfig
{
	void Accept(IVisitor visitor);
	string ToLabelString();
	void Enter();
	void Exit();
	PathFinding.IGraph<Region> GetGraph();
}

public abstract class State
{
	protected GameManager gameManager;
	protected ScenarioData scenarioData;

	public State(GameManager gameManager, ScenarioData scenarioData)
	{
		this.gameManager = gameManager;
		this.scenarioData = scenarioData;
	}

	// public void Accept(IVisitor visitor) => visitor.Visit(this); // We need type info from derived class.
	public virtual void Enter(){}
	public virtual void Exit(){}
	public virtual string ToLabelString() => GetType().Name;

	public virtual bool IsSkipCombat() => false;

	public virtual PathFinding.IGraph<Region> GetGraph() => gameManager.GetUnconstraitGraph();
}

public class AssaultState : State, IState
{
	public AssaultState(GameManager gameManager, ScenarioData scenarioData) : base(gameManager, scenarioData){}
	public void Accept(IVisitor visitor) => visitor.Visit(this);
	public void Visit(AssaultState state){}
	public void Visit(CeaseFireState state){}
	public void Visit(CombatState state){}
}

public class CeaseFireState : State, IState
{
	public CeaseFireState(GameManager gameManager, ScenarioData scenarioData) : base(gameManager, scenarioData){}
	public void Accept(IVisitor visitor) => visitor.Visit(this);
	public void Visit(AssaultState state)
	{
		foreach(var unit in scenarioData.units)
		{
			// All moves into the hostile region are canceled.
			// if(unit.movingState.active && !unit.movingState.nextRegion.IsConsistentWith(unit.side))
			if(unit.movingState.active && !unit.movingState.nextRegion.parent.Equals(unit.side))
				unit.movingState.Reset();
			
			// All units in hostile regions are "exiled" to a friendly region.
			if(!unit.side.Equals(unit.parent.parent))
			{
				var path = PathFinding.PathFinding<Region>.ExploreNearestTarget(gameManager.GetUnconstraitGraph(), unit.parent, region => region.parent.Equals(unit.side));
				unit.movingState.ResetToPath(path);
			}
		}
	}
	public void Visit(CeaseFireState state){}
	public void Visit(CombatState state){}

	public override bool IsSkipCombat() => true;

	public override PathFinding.IGraph<Region> GetGraph() => gameManager.GetSideConstraitGraph();
}

public class CombatState : State, IState
{
	public CombatState(GameManager gameManager, ScenarioData scenarioData) : base(gameManager, scenarioData){}
	public void Accept(IVisitor visitor) => visitor.Visit(this);
	public void Visit(AssaultState state){}
	public void Visit(CeaseFireState state){}
	public void Visit(CombatState state){}
}

/// <summary>
/// UI-free game logic
/// </summary>
public class GameManager
{
    ScenarioData scenarioData;
    public Agent agent;

	List<IState> states = new List<IState>();
	IState _state;
	public IState state
	{
		get => _state;
		set
		{
			_state?.Exit();
			_state?.Accept(value);
			value.Enter();

			_state = value;
		}
	}

    public GameManager(ScenarioData scenarioData)
    {
        this.scenarioData = scenarioData;
        // this.agent = new RandomWalkingAgent(scenarioData, scenarioData.govSide);
        // this.agent = new SimpleAttackingAgent(scenarioData, scenarioData.govSide);
		// this.agent = new ConvexEncircleAgent(scenarioData, scenarioData.govSide);
		this.agent = new SimpleEncircleAgent(scenarioData, scenarioData.govSide, this);

		states.Add(new AssaultState(this, scenarioData));
		states.Add(new CeaseFireState(this, scenarioData));
		state = states[0];
    }

	public void GotoNextState() => state = states[(states.IndexOf(state) + 1) % states.Count];

	public void SimulationStep() // 1 min -> 1 call
	{
		foreach(var region in scenarioData.regions)
			region.StepPre();
		foreach(var unit in scenarioData.units)
			unit.StepPre(state);
		foreach(var unit in scenarioData.units)
			unit.Step(state);
		foreach(var unit in scenarioData.units.ToList())
			unit.StepPost(state);

        agent.Schedule();
	}

	public PathFinding.IGraph<Region> GetUnconstraitGraph() => scenarioData.mapData;
	public PathFinding.IGraph<Region> GetSideConstraitGraph() => new SideConstraitGraph(GetUnconstraitGraph());

	/// <summary>
	/// State/Phase determine the concrete selected graph. Though we may be something like `GraphFor(Side)`, `GraphFor(Unit)`. 
	/// </summary>
	public PathFinding.IGraph<Region> GetGraph() => state.GetGraph();
}

public class SideConstraitGraph : PathFinding.IGraph<Region>
{
	PathFinding.IGraph<Region> unconstraitGraph;
	public SideConstraitGraph(PathFinding.IGraph<Region> unconstraitGraph)
	{
		this.unconstraitGraph = unconstraitGraph;
	}

	public float MoveCost(Region src, Region dst) => unconstraitGraph.MoveCost(src, dst);
	public float EstimateCost(Region src, Region dst) => unconstraitGraph.EstimateCost(src, dst);
	public IEnumerable<Region> Neighbors(Region src)
	{
		foreach(var nei in unconstraitGraph.Neighbors(src))
			if(nei.parent.Equals(src.parent))
				yield return nei;
	}
}


}