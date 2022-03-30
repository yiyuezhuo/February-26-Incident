namespace YYZ.App
{


using System.Collections.Generic;
using Godot;
using System.Linq;

public abstract class Agent
{
    protected Side controllingSide;
    protected ScenarioData scenarioData;

    protected IEnumerable<Unit> controllingUnits{get => from unit in scenarioData.units where unit.side.Equals(controllingSide) && !unit.frozen select unit;}
    protected IEnumerable<Unit> enemyUnits{get => from unit in scenarioData.units where !unit.side.Equals(controllingSide) select unit;}
    protected IEnumerable<Region> RegionsOf(Side side)
    {
        foreach(var region in scenarioData.regions)
            foreach(var unit in region.children)
                if(unit.side.Equals(side))
                {
                    yield return region;
                    break;
                }
    }

    protected IEnumerable<Region> RegionsOfOpponentOf(Side side)
    {
        foreach(var region in scenarioData.regions)
            foreach(var unit in region.children)
                if(!unit.side.Equals(side))
                {
                    yield return region;
                    break;
                }
    }


    public Agent(ScenarioData scenarioData, Side side)
    {
        this.controllingSide = side;
        this.scenarioData = scenarioData;
    }

    public virtual void Schedule() // Schedule is not necessarily consistent with Schedule(Unit unit)
    {
        foreach(var unit in controllingUnits)
            if(!unit.movingState.active)
                Schedule(unit);
    }

    public abstract void Schedule(Unit unit);

    /// <summary>
    /// Extract center info and invert y.
    /// </summary>
    protected System.Numerics.Vector2 CenterFor(Region region) => new System.Numerics.Vector2(region.center.x, -region.center.y); 
    // TODO: performance issue? Is it better to make a dedicate interface targeting Godot or Unity's Vector2?
}


public class RandomWalkingAgent : Agent
{
    public RandomWalkingAgent(ScenarioData scenarioData, Side side): base(scenarioData, side) {}

    public override void Schedule(Unit unit)
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
            path = PathFinding.PathFinding<Region>.AStar(scenarioData.mapData, src, dst);
        }while(path.Count <= 1); // TODO: Add a sentinel?

        return path;
    }
}

public class SimpleAttackingAgent : Agent
{
    public SimpleAttackingAgent(ScenarioData scenarioData, Side side): base(scenarioData, side) {}

    public override void Schedule(Unit unit)
    {
        var path = PathFinding.PathFinding<Region>.ExploreNearestTarget(scenarioData.mapData, unit.parent, HasEnemy);
		unit.movingState.ResetToPath(path); // TODO: FOG?
    }

    bool HasEnemy(Region region)
    {
        foreach(var unit in region.children)
            if(!unit.side.Equals(controllingSide))
                return true;
        return false;
    }
}

/// <summary>
/// Plan a group of units to attack. The behavior of `Schedule` are not consistent here.
/// </summary>
public class ComplexAttackingAgent : Agent
{
    public ComplexAttackingAgent(ScenarioData scenarioData, Side side): base(scenarioData, side) {}

    public void Schedule(IEnumerable<Unit> units)
    {
        var opponentRegions = RegionsOfOpponentOf(controllingSide).ToList();
        if(opponentRegions.Count == 0)
            return;
        
        var wrapper = PathFinding.PathFinding<Region>.RegionConvexHullWrapper(scenarioData.mapData, opponentRegions, CenterFor);
        // How to distribute units to occupy is a complex planning problem. Here we just distribute it using order.
        var idx = 0;
        foreach(var unit in units)
        {
            var dst = wrapper[idx];
            var path = PathFinding.PathFinding<Region>.AStar(scenarioData.mapData, unit.parent, dst);
            unit.movingState.ResetToPath(path);

            idx = (idx + 1) % wrapper.Count;
        }
    }

    public override void Schedule()
    {
        Schedule(controllingUnits.Where(x => !x.movingState.active));
    }

    public override void Schedule(Unit unit)
    {
        // Schedule(new Unit[]{unit});
    }

}


}