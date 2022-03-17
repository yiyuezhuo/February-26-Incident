namespace YYZ.App
{


using YYZ.Data.February26;
using System.Collections.Generic;
using System.Linq;
using System;

public class MovingState
{
    public List<Region> path = new List<Region>(); // (Current, ..., Destination)
    public float movedDistance = 0f;
    public float nextDistance = 0f;
    public float totalDistance = 0f;

    public override string ToString()
    {
        var ps = string.Join(",", path);
        return $"MovingState[{path.Count}]({movedDistance}/{nextDistance}/{totalDistance}, {ps})";
    }

    public event EventHandler updated; // This event should be used only for UI updating.
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

        updated?.Invoke(this, EventArgs.Empty);
    }

    public void ResetToPath(List<Region> path)
    {
        _Reset();
        _Extends(path);

        updated?.Invoke(this, EventArgs.Empty);
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

        updated?.Invoke(this, EventArgs.Empty);
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
                    updated?.Invoke(this, EventArgs.Empty);
                    return reachedRegions;
                }
                nextDistance = path[0].DistanceTo(path[1]);
            }
            else{
                movedDistance += movement;
                movement = 0;
            }
        }
        updated?.Invoke(this, EventArgs.Empty);
        return reachedRegions;
    }

    /*
    /// <summary>
    /// Python list "extend" like (inplace) method.
    /// </summary>
    public void Extends(MovingState followedState)
    {
        // if(path[path.Count-1].Equals(followedState.path[0]))
        //     throw new ArgumentException("Extended tail and extending head should be the same region");
        
        path.AddRange(followedState.path.Skip(1));
        totalDistance += followedState.totalDistance;

        updated.Invoke(this, EventArgs.Empty);
    }
    */

    public Region destination{get => path[path.Count-1];}
}

public class Unit : Child<Unit, Region, List<Unit>>, IContainer<List<Leader>, Leader>, UnitInfoPad.IData, UnitBar.IData
{
    UnitTable.Data data;

    public string name{get => data.name;}
    public string nameJap{get => data.nameJap;}
    public Side side;
    public float strength{get; set;}
    public float fatigue{get; set;}
    public float command{get => children.Sum(leader => leader.command);}

    IEnumerable<LeaderPad.IData> UnitBar.IData.children{get => children;}

    public float moveSpeedMPerMin = 25; // 25m/min -> 1.5km/h
    public static float mPerPixel = 6; // 6m = 1 unit pixel distance
    public float moveSpeedPiexelPerMin {get => moveSpeedMPerMin / mPerPixel;}

    public MovingState movingState{get;} = new MovingState();

    public Unit(UnitTable.Data data)
    {
        this.data  = data;
        this.strength = data.strength;
    }

    public List<Leader> children{get; set;} = new List<Leader>();

    // public override string ToString() => $"Unit({name}, {nameJap}, {children.Count}, {parent})";
    public override string ToString() => $"Unit({name}, {nameJap}, {children.Count})";

    /// <summary>
    /// return value denotes whether region is updated.
    /// </summary>
    public List<Region> GoForward(float movement)
    {
        // if(movingState == null)
        //     return null;
        var reachedRegions = movingState.GoForward(movement);
        if(reachedRegions.Count > 0)
        {
            MoveTo(reachedRegions[reachedRegions.Count - 1]);
        }
        foreach(var region in reachedRegions)
        {
            region.MoveTo(side);
        }
        return reachedRegions;
    }

    // public bool isMoving {get => movingState.actived;}
}


}