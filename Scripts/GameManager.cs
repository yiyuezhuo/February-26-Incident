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

public interface IState : IVisitor
{
	void Accept(IVisitor visitor);
	string ToLabelString();
	void Enter();
	void Exit();
}

public abstract class State
{
	protected GameManager gameManager;
	public State(GameManager gameManager)
	{
		this.gameManager = gameManager;
	}

	// public void Accept(IVisitor visitor) => visitor.Visit(this); // We need type info from derived class.
	public virtual void Enter(){}
	public virtual void Exit(){}
	public virtual string ToLabelString() => GetType().Name;
}

public class AssaultState : State, IState
{
	public AssaultState(GameManager gameManager) : base(gameManager){}
	public void Accept(IVisitor visitor) => visitor.Visit(this);
	public void Visit(AssaultState state){}
	public void Visit(CeaseFireState state){}
	public void Visit(CombatState state){}
}

public class CeaseFireState : State, IState
{
	public CeaseFireState(GameManager gameManager) : base(gameManager){}
	public void Accept(IVisitor visitor) => visitor.Visit(this);
	public void Visit(AssaultState state){}
	public void Visit(CeaseFireState state){}
	public void Visit(CombatState state){}
}

public class CombatState : State, IState
{
	public CombatState(GameManager gameManager) : base(gameManager){}
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
		this.agent = new SimpleEncircleAgent(scenarioData, scenarioData.govSide);

		states.Add(new AssaultState(this));
		states.Add(new CeaseFireState(this));
		state = states[0];
    }

	public void GotoNextState() => state = states[(states.IndexOf(state) + 1) % states.Count];

	public void SimulationStep() // 1 min -> 1 call
	{
		foreach(var region in scenarioData.regions)
			region.StepPre();
		foreach(var unit in scenarioData.units)
			unit.StepPre();
		foreach(var unit in scenarioData.units)
			unit.Step();
		foreach(var unit in scenarioData.units.ToList())
			unit.StepPost();

        agent.Schedule();
	}
}


}