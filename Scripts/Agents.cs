namespace YYZ.App
{


using System.Collections.Generic;
using Godot;
using System.Linq;

public abstract class Agent
{
    protected Side controllingSide;
    protected ScenarioData scenarioData;

    protected IEnumerable<Unit> controllingUnits{get => from unit in scenarioData.units where unit.side.Equals(controllingSide) select unit;}
    protected IEnumerable<Unit> enemyUnits{get => from unit in scenarioData.units where !unit.side.Equals(controllingSide) select unit;}

    public Agent(ScenarioData scenarioData, Side side)
    {
        this.controllingSide = side;
        this.scenarioData = scenarioData;
    }

    public abstract void Schedule();

}

public class RandomWalkingAgent : Agent
{
    public RandomWalkingAgent(ScenarioData scenarioData, Side side): base(scenarioData, side) {}

    public override void Schedule()
    {
        foreach(var unit in controllingUnits)
            if(!unit.movingState.active && !unit.frozen)
                Schedule(unit);
    }

    public void Schedule(Unit unit)
    {
        var path = SamplePath(unit.parent);
		unit.movingState.ResetToPath(path); // TODO: FOG?
    }

    List<Region> SamplePath(Region src)
    {
        List<Region> path;
        do
        {
            var dst = scenarioData.mapData.SampleRegion();
            var pathFinding = new PathFinding.PathFinding<Region>(scenarioData.mapData);
            path = pathFinding.PathFindingAStar(src, dst);
        }while(path.Count <= 1); // TODO: Add a sentinel?

        return path;
    }
}


}