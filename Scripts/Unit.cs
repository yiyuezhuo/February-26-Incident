namespace YYZ.App
{


using YYZ.Data.February26;
using System.Collections.Generic;
using System.Linq;
using System;
using Godot;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;


public class DetachRequest
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

public class TransferRequest
{
    public DetachRequest detachRequest;
    public Unit dst;

    public TransferRequest(DetachRequest detachRequest, Unit dst)
    {
        this.detachRequest = detachRequest;
        this.dst = dst;
    }

	public void Apply()
	{
        detachRequest.src.TransferTo(dst, detachRequest.strength, detachRequest.selectedLeaderList);
	}

}

public class CreateRequest
{
    public DetachRequest detachRequest;
    public Region dstArea;

    public CreateRequest(DetachRequest detachRequest, Region dstArea)
    {
        this.detachRequest = detachRequest;
        this.dstArea = dstArea;
    }

    public Unit Apply()
    {
		var src = detachRequest.src;

		var unit = new UnitProcedure(src.side, 0, 0);
		unit.EnterTo(src.parent);
		var transferRequest = new TransferRequest(detachRequest, unit);
		transferRequest.Apply();

        return unit;
    }
}

public class MovingState
{
    public List<Region> path = new List<Region>(); // (Current, ..., Destination), len >= 2
    public float movedDistance = 0f;
    public float nextDistance = 0f;
    public float totalDistance = 0f;

    public override string ToString()
    {
        var ps = string.Join(",", path);
        return $"MovingState[{path.Count}]({movedDistance}/{nextDistance}/{totalDistance}, {ps})";
    }

    public event EventHandler<bool> updated; // This event should be used only for UI updating. bool denotes if it's a "small" (progression only / path is not modified) update.
    public bool active{get => path.Count > 0;}

    /// <summary>
    /// [] + [p_1, p_2] => [p_1, p_2]
    /// [p_1, p_2] + [p_2, p_3] => [p_1, p_2, p_3]
    /// [p_1, p_2] + [p_3, p_4] is not valid.
    /// </summary>
    public void _Extends(List<Region> path)
    {
        if(!active)
        {
            this.path.AddRange(path);
            nextDistance = path[0].DistanceTo(path[1]);
        }
        else
        {
            this.path.AddRange(path.Skip(1));
        }
        
        for(int i=0; i<path.Count-1; i++)
            totalDistance += path[i].DistanceTo(path[i+1]);
    }

    public void Extends(List<Region> path)
    {
        _Extends(path);

        updated?.Invoke(this, false);
    }

    public void ResetToPath(List<Region> path)
    {
        _Reset();
        _Extends(path);

        updated?.Invoke(this, false);
    }

    void _Reset()
    {
        path.Clear();
        
        movedDistance = 0f;
        nextDistance = 0f;
        totalDistance = 0f;
    }

    public void Reset()
    {
        _Reset();

        updated?.Invoke(this, false);
    } 

    /// <summary>
    /// lastReached = `null` denotes no location update happens.
    /// returned bool denotes if the path is "completed"
    /// </summary>
    public List<Region> GoForward(float movement)
    {
        var reachedRegions = new List<Region>();
        // assert path.Count >= 1
        while(movement > 0)
        {
            if(movedDistance + movement >= nextDistance)
            {
                movedDistance = 0;
                movement -= nextDistance - movedDistance;
                totalDistance -= nextDistance;
                path.RemoveAt(0);
                reachedRegions.Add(path[0]);
                if(path.Count == 1)
                {
                    _Reset();
                    updated?.Invoke(this, false);
                    return reachedRegions;
                }
                nextDistance = path[0].DistanceTo(path[1]);
            }
            else{
                movedDistance += movement;
                movement = 0;
            }
        }
        updated?.Invoke(this, reachedRegions.Count == 0);
        return reachedRegions;
    }

    public Region destination{get => path[path.Count-1];}
    public Region nextRegion{get => path[1];}
}

/// <summary>
/// Unit is designed to be a complex but "configurable" object, just like ParticleMaterial. So for behavior branching, we favor states and tags over inheriatance and interfaces.
/// BTW, thanks to the fact that I can't see a simple way to refactor this at this time...
/// </summary>
public abstract class Unit : Child<Unit, Region, List<Unit>>, IContainer<List<Leader>, Leader>, UnitInfoPad.IData, UnitBar.IData, UnitPad.IData
{
    public virtual string name{get => "[name]";}
    public virtual string nameJap{get => "[nameJap]";}

    public Side side;
    public float strength{get; set;}
    public float fatigue{get; set;}
    public float suppression{get; set;}

    public float command{get => children.Sum(leader => leader.command);}
    public Texture portrait{get => children[0].portrait;}
    public Vector2 center{get => parent.center;}

    public float totalTakenFire;

    IEnumerable<LeaderPad.IData> UnitBar.IData.children{get => children;}
    Texture UnitPad.IData.flagTex{get => side.picture;}

    // "Constants"

    float moveSpeedMPerMin = 25; // 25m/min -> 1.5km/h
    static float mPerPixel = 6; // 6m = 1 unit pixel distance
    float moveSpeedPiexelPerMin {get => moveSpeedMPerMin / mPerPixel;}

    public MovingState movingState{get;} = new MovingState();
    // public Dictionary<Unit, float> fireTargetMap = new Dictionary<Unit, float>();

    public event EventHandler<MovePath> moveStateUpdated;
    public event EventHandler childrenUpdated;

    public List<Leader> children{get; set;} = new List<Leader>();

    bool isFiredLastStep;

    // public override string ToString() => $"Unit({name}, {nameJap}, {children.Count}, {parent})";
    public override string ToString() => $"Unit({name}, {nameJap}, {children.Count})";

    public void OnChildrenUpdated()
    {
        childrenUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// return value denotes whether region is updated.
    /// </summary>
    void GoForward(float movement)
    {
        var reachedRegions = movingState.GoForward(movement);
        if(reachedRegions.Count > 0)
        {
            var src = parent;
            MoveTo(reachedRegions[reachedRegions.Count - 1]);
            moveStateUpdated.Invoke(this, new MovePath(src, reachedRegions));
        }
    }

    public void TransferTo(Unit dst, float strength, List<Leader> selectedLeaderList)
    {
        var src = this;

		foreach(var leader in selectedLeaderList)
		{
			leader.MoveTo(dst);
		}
		
		var dn = dst.strength + dst.children.Count;
		var sn = strength + selectedLeaderList.Count;
		dst.fatigue = ((dn * dst.fatigue) + (sn * src.fatigue)) / (dn + sn);
		
		dst.strength += strength;
		src.strength -= strength;

		var isFullCombining = src.children.Count == 0 && src.strength == 0;
		if(isFullCombining)
		{
			src.Destroy();
		}
    }

    float strengthWithLeaders{get => strength + children.Count;}
    float commandEfficiency{get => Mathf.Min(command / strengthWithLeaders, 2);}

    float GetCasualtiesModifer()
    {
        var fatigueModifer = fatigue * 4f + 1.0f; // 100% -> 500%
        var suppressionModifer = suppression * 0.2f + 1.0f; // 100% -> 120%
        var commandModifer = commandEfficiency < 1.0f ? commandEfficiency * -0.1f + 1.1f : (commandEfficiency - 1f) * -0.05f + 1f; // 110% -> 100% -> 95%
        return fatigueModifer * suppressionModifer * commandModifer;
    }

    float GetFireEfficiencyModifer()
    {
        var fatigueModifer = fatigue * -0.5f + 1f; // 100% -> 50%
        var suppressionModifier = suppression * -0.5f + 1f; // 100% -> 50%
        var commandModifer = commandEfficiency < 1f ? commandEfficiency * 0.3f + 0.7f : (commandEfficiency - 1f) * 0.1f + 1f; // 70% -> 100% -> 110%
        return fatigueModifer * suppressionModifier * commandModifer;
    }

    static float killFactor = 0.0015f;
    static float killVolative = 0.0045f;
    static float suppressionFactor = 0.2f;
    static float suppressionVolative = 0.02f;
    static float fatigueFactor = 0.02f;
    static float fatigueVolative = 0.02f;

    float Sample(float upper, float firepower, float factor, float volative)
    {
        return (float)(new TruncatedNormal(YYZ.Random.GetRandom(), 0, upper, firepower * factor, firepower * volative)).Sample();
    }

    public void TakeDirectFire(float firepower)
    {
        // TODO: use correlated sample rather than following indepent sampling.
        // var killRaw = Sample(strengthWithLeaders, firepower, killFactor, killVolative);
        if(firepower == 0)
            return;
        
        firepower *= GetCasualtiesModifer();

        var suppressionDelta = Sample(strengthWithLeaders, firepower, suppressionFactor, suppressionVolative);
        var fatigueDelta = Sample(strengthWithLeaders, firepower, fatigueFactor, fatigueVolative);

        suppression = Mathf.Min(suppression + suppressionDelta / strengthWithLeaders, 1f);
        fatigue = Mathf.Min(fatigue + fatigueDelta / strengthWithLeaders, 1f);

        var alpha = strength > 0 ? new double[children.Count + 1] : new double[children.Count];

        for(int i=0; i<children.Count; i++) // TODO: refactor strength > 0 branch
            alpha[i] = 1;

        if(strength > 0)
            alpha[children.Count] = strength;

        var diriDist = new Dirichlet(alpha);
        var w = diriDist.Sample();
        var expDist = new Exponential(2); // rate = 2
        for(var i=0; i<children.Count; i++)
        {
            var fortune = expDist.Sample();
            var firepowerInflicted = firepower * w[i];
            if(fortune < firepowerInflicted * killFactor)
            {
                var leader = children[i];
                leader.Destroy();
                GD.Print($"{leader} is killed"); // true leader kill will be implemented later.
            }
        }

        if(strength > 0)
        {
            var firepowerOnStrength = firepower * w[children.Count];
            var killed = Sample(strength, (float)firepowerOnStrength, killFactor, killVolative);

            /*
            // Test randomness
            var samples = new float[1000];
            for(int i=0; i<1000; i++)
                samples[i] = Sample(strength, (float)firepowerOnStrength, killFactor, killVolative);
            GD.Print($"draw={killed}, mean={Statistics.Mean(samples)}, std={Statistics.StandardDeviation(samples)}");
            */

            strength -= killed;
        }

        // Elect a leader if no leader lives. If no leader is elected, the unit is destroyed.
        if(children.Count == 0)
        {
            if(strength >= 1)
            {
                strength -= 1;
                var leader = new LeaderProcedure("Unamed Hero", "", side.picture, 1);
                leader.EnterTo(this);
            }
            else
            {
                Destroy();
            }
        }
    }

    public void StepPre()
    {
        parent.AddOverlap(this);
        if(movingState.active)
            movingState.nextRegion.AddOverlap(this);

        totalTakenFire = 0;

        if(!isFiredLastStep)
            if(!movingState.active)
                fatigue = Mathf.Max(fatigue - 0.01f, 0);
            else
                fatigue = Mathf.Min(fatigue + 0.001f, 1f);
        else
            fatigue = Mathf.Min(fatigue + 0.0001f, 1f); // 
            
        // suppression *= 0.5f;
        suppression = Mathf.Max(suppression - 0.1f, 0f);
    }

    public void Step()
    {
        if(movingState.active)
            GoForward(moveSpeedPiexelPerMin); // TODO: engage decrease movement speed.
        DistributeFirepower();
    }

    public void StepPost()
    {
        TakeDirectFire(totalTakenFire);
    }
    
    static float fireAlpha = 1f;

    public void DistributeFirepower()
    {
        var overlapRegions = new List<Region>(){parent};
        if(movingState.active)
            overlapRegions.Add(movingState.nextRegion);

        var unitsSet = new HashSet<Unit>();

        foreach(var region in overlapRegions)
            foreach(var unit in region.GetOverlap())
                if(!unit.side.Equals(side))
                    unitsSet.Add(unit);

        var units = unitsSet.ToList();
        var weights = (from unit in units select unit.strengthWithLeaders).ToList();

        /*
        if(parent.areaData != null && parent.areaData.name.Contains("Prime"))
            GD.Print("pause");
        */
        /*
        if(name.Contains("Makino Nobuaki Assassin Group"))
            GD.Print("pause");
        */
        
        if(units.Count == 0)
        {
            isFiredLastStep = false;
            return;
        }

        isFiredLastStep = true;

        var cons = weights.Sum() / fireAlpha;
        var alpha = weights.Select(s => (double)(s / cons)).ToArray<double>();
        var dist = new Dirichlet(alpha);
        var percents = dist.Sample();

        var firepower = GetFirepower();
        for(var i=0; i<units.Count; i++)
            units[i].totalTakenFire += firepower * (float)percents[i];
    }

    float GetFirepower()
    {
        return strengthWithLeaders * GetFireEfficiencyModifer();
    }

    public class MovePath
    {
        public Region src;
        public Region dst{get => reachedRegions[reachedRegions.Count-1];}
        public List<Region> reachedRegions;

        public MovePath(Region src, List<Region> reachedRegions)
        {
            this.src = src;
            this.reachedRegions = reachedRegions;
        }
    }
}

public class UnitFromTable : Unit
{
    UnitTable.Data data;
    
    public override string name{get => data.name;}
    public override string nameJap{get => data.nameJap;}

    public UnitFromTable(UnitTable.Data data)
    {
        this.data  = data;
        this.strength = data.strength;
    }
}

public class UnitProcedure : Unit
{
    // public override string name{get => "";} // TODO: C# doesn't allow adding setter in child class???
    // CC: https://stackoverflow.com/questions/82437/why-is-it-impossible-to-override-a-getter-only-property-and-add-a-setter
    // Since These properties are not needed at this time, 
    // public override string nameJap{get => "";}

    public UnitProcedure(Side side, float strength, float fatigue)
    {
        this.side = side;
        this.strength = strength;
        this.fatigue = fatigue;

        // suppression = 0;
    }
}

/// <summary>
/// UnitFromObjective uses a normal Region as parent but a dummy leader as child.
/// </summary>
public class UnitFromObjective: Unit
{
    public UnitFromObjective(ObjectiveTable.Data data)
    {
        var dummyLeader = new LeaderProcedure(data.name, data.nameJap, data.picture, 1);
        dummyLeader.EnterTo(this);

        if(data.isBuilding) // TODO: We may define a strength field in NotionData.
        {
            // strength
            strength = YYZ.Random.NextFloat() * 30;
        }
        else
        {
            strength = YYZ.Random.NextFloat() * 10;
        }
    }
}


}