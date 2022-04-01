namespace YYZ.App
{


using System.Collections.Generic;
using Godot;
using System.Linq;

public abstract class Agent
{
    protected Side controllingSide;
    protected ScenarioData scenarioData;
    protected GameManager gameManager;

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

    public Agent(ScenarioData scenarioData, Side side, GameManager gameManager)
    {
        this.controllingSide = side;
        this.scenarioData = scenarioData;
        this.gameManager = gameManager;
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
    public RandomWalkingAgent(ScenarioData scenarioData, Side side, GameManager gameManager): base(scenarioData, side, gameManager) {}

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
            var dst = scenarioData.mapData.SampleRegion(); // TODO: Move `SampleRegion` to GameManager? It seems like that it may need phase related info to work.
            path = PathFinding.PathFinding<Region>.AStar(gameManager.GetGraph(), src, dst);
        }while(path.Count <= 1); // TODO: Add a sentinel?

        return path;
    }
}

public class SimpleAttackingAgent : Agent
{
    public SimpleAttackingAgent(ScenarioData scenarioData, Side side, GameManager gameManager): base(scenarioData, side, gameManager) {}

    public override void Schedule(Unit unit)
    {
        var path = PathFinding.PathFinding<Region>.ExploreNearestTarget(gameManager.GetUnconstraitGraph(), unit.parent, HasEnemy);
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

public abstract class EncircleAgent : Agent
{
    public EncircleAgent(ScenarioData scenarioData, Side side, GameManager gameManager): base(scenarioData, side, gameManager) {}

    protected abstract List<Region> GetWrapper(List<Region> nonEmptyOpponentRegions);

    public void Schedule(IEnumerable<Unit> units)
    {
        var opponentRegions = RegionsOfOpponentOf(controllingSide).ToList();
        if(opponentRegions.Count == 0)
            return;
        
        var wrapper = GetWrapper(opponentRegions);
        Distribute(units, wrapper);
    }

    void Distribute(IEnumerable<Unit> units, List<Region> wrapper)
    {
        // How to distribute units to occupy is a complex planning problem. Here we just distribute it using order.
        var idx = 0;
        foreach(var unit in units)
        {
            var dst = wrapper[idx];
            var path = PathFinding.PathFinding<Region>.AStar(gameManager.GetGraph(), unit.parent, dst);
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

/// <summary>
/// Plan a group of units to attack. The behavior of `Schedule` are not consistent here.
/// </summary>
public class ConvexEncircleAgent : EncircleAgent
{
    public ConvexEncircleAgent(ScenarioData scenarioData, Side side, GameManager gameManager): base(scenarioData, side, gameManager) {}

    protected override List<Region> GetWrapper(List<Region> opponentRegions) => PathFinding.PathFinding<Region>.RegionConvexHullWrapper(gameManager.GetUnconstraitGraph(), opponentRegions, CenterFor);
}

public class SimpleEncircleAgent : EncircleAgent
{
    public SimpleEncircleAgent(ScenarioData scenarioData, Side side, GameManager gameManager): base(scenarioData, side, gameManager) {}

    protected override List<Region> GetWrapper(List<Region> opponentRegions)
    {
        var selectedSet = new HashSet<Region>();
        var excludeSet = opponentRegions.ToHashSet();
        
        foreach(var opponentRegion in opponentRegions)
            foreach(var region in opponentRegion.neighbors)
                if(!excludeSet.Contains(region))
                    selectedSet.Add(region);
        
        return selectedSet.ToList();
    }
}



}