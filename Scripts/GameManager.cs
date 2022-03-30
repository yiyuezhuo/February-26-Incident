namespace YYZ.App
{


using System.Linq;

/// <summary>
/// UI-free game logic
/// </summary>
public class GameManager
{
    ScenarioData scenarioData;
    public Agent agent;

    public GameManager(ScenarioData scenarioData)
    {
        this.scenarioData = scenarioData;
        // this.agent = new RandomWalkingAgent(scenarioData, scenarioData.govSide);
        // this.agent = new SimpleAttackingAgent(scenarioData, scenarioData.govSide);
		// this.agent = new ConvexEncircleAgent(scenarioData, scenarioData.govSide);
		this.agent = new SimpleEncircleAgent(scenarioData, scenarioData.govSide);
    }

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