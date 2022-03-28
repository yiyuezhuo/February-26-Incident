namespace YYZ.App
{


using Godot;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using System.Linq;
using System;

public class UnitAgent
{
    protected Unit unit;

    protected float command{get => unit.command;}
    protected float fatigue{get => unit.fatigue; set => unit.fatigue = value;}
    protected float suppression{get => unit.suppression; set => unit.suppression = value;}
    protected float strength{get => unit.strength; set => unit.strength = value;}
    protected List<Leader> children{get => unit.children;}
    protected Side side{get => unit.side;}
    protected Region parent{get => unit.parent;}

    protected float strengthWithLeaders{get => unit.strengthWithLeaders;}

    public MovingState movingState{get => unit.movingState;}

    protected void Destroy() => unit.Destroy();

    public UnitAgent(Unit unit)
    {
        this.unit = unit;
    }
}

public class CombatAgent: UnitAgent
{
    public CombatAgent(Unit unit) : base(unit) {}

    bool isFiredLastStep;
    public float totalTakenFire;

    float commandEfficiency{get => Mathf.Min(command / strengthWithLeaders, 2);}

    public void StepPre()
    {
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
        DistributeFirepower();
    }

    public void StepPost()
    {
        TakeDirectFire(totalTakenFire);
    }

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
    static float killVolatile = 0.0045f;
    static float suppressionFactor = 0.2f;
    static float suppressionVolatile = 0.02f;
    static float fatigueFactor = 0.02f;
    static float fatigueVolatile = 0.02f;
    static float fireAlpha = 1f;

    float Sample(float upper, float firepower, float factor, float _volatile)
    {
        if(firepower < 1e-10)
            return 0; // Numerical problems.
        return (float)(new TruncatedNormal(YYZ.Random.GetRandom(), 0, upper, firepower * factor, firepower * _volatile)).Sample();
    }

    public void TakeDirectFire(float firepower)
    {
        // TODO: use correlated sample rather than following independent sampling.
        // var killRaw = Sample(strengthWithLeaders, firepower, killFactor, killVolatile);
        if(firepower == 0)
            return;
        
        firepower *= GetCasualtiesModifer();

        var suppressionDelta = Sample(strengthWithLeaders, firepower, suppressionFactor, suppressionVolatile);
        var fatigueDelta = Sample(strengthWithLeaders, firepower, fatigueFactor, fatigueVolatile);

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
            var killed = Sample(strength, (float)firepowerOnStrength, killFactor, killVolatile);

            /*
            // Test randomness
            var samples = new float[1000];
            for(int i=0; i<1000; i++)
                samples[i] = Sample(strength, (float)firepowerOnStrength, killFactor, killVolatile);
            GD.Print($"draw={killed}, mean={Statistics.Mean(samples)}, std={Statistics.StandardDeviation(samples)}");
            */

            strength -= killed;

            if(float.IsNaN(strength)) // DEBUG
                throw new ArgumentException("strength shouldn't be nan");
        }

        // Elect a leader if no leader lives. If no leader is elected, the unit is destroyed.
        if(children.Count == 0)
        {
            if(strength >= 1)
            {
                strength -= 1;
                var leader = new LeaderProcedure("Unamed Hero", "", side.picture, 1);
                // leader.EnterTo(this);
                leader.EnterTo(unit);
            }
            else
            {
                Destroy();
            }
        }
    }

    float GetFirepower()
    {
        return strengthWithLeaders * GetFireEfficiencyModifer();
    }

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
            units[i].agent.totalTakenFire += firepower * (float)percents[i];
    }
}


}