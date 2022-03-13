namespace YYZ.Data.February26
{


using System.Collections.Generic;
using System.Linq;

public class MovingState
{
    public List<Region> path;
    public float movedDistance;
    public float nextDistance;
    public float totalDistance;

    public override string ToString()
    {
        var ps = string.Join(",", path);
        return $"MovingState[{path.Count}]({movedDistance}/{nextDistance}/{totalDistance}, {ps})";
    }

    public MovingState(IEnumerable<Region> path)
    {
        this.path = path.ToList();

        movedDistance = 0f;
        totalDistance = 0f;
        for(int i=0; i<this.path.Count-1; i++)
        {
            var dist = this.path[i].DistanceTo(this.path[i+1]);
            if(i==0)
                nextDistance = dist;
            totalDistance += dist;
        }
    }

    /// <summary>
    /// lastReached = `null` denotes no location update happens.
    /// returned bool denotes if the path is "completed"
    /// </summary>
    public bool GoForward(float movement, out List<Region> reachedRegions)
    {
        reachedRegions = new List<Region>();
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
                    return true;
                }
                nextDistance = path[0].DistanceTo(path[1]);
                
                
            }
            else{
                movedDistance += movement;
                movement = 0;
            }
        }
        return false;
    }

    /// <summary>
    /// Python list "extend" like (inplace) method.
    /// </summary>
    public void Extends(MovingState followedState)
    {
        // if(path[path.Count-1].Equals(followedState.path[0]))
        //     throw new ArgumentException("Extended tail and extending head should be the same region");
        
        path.AddRange(followedState.path.Skip(1));
        totalDistance += followedState.totalDistance;
    }

    public Region destination{get => path[path.Count-1];}

    
}

public class Unit : Child<Unit, Region, List<Unit>>, IContainer<List<Leader>, Leader>
{
    UnitTable.Data data;

    public string name{get => data.name;}
    public string nameJap{get => data.nameJap;}
    public Side side;
    public float strength{get; set;}
    public float moveSpeedMPerMin = 25; // 25m/min -> 1.5km/h
    public static float mPerPixel = 6; // 6m = 1 unit pixel distance
    public float moveSpeedPiexelPerMin {get => moveSpeedMPerMin / mPerPixel;}

    public MovingState movingState;

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
    public bool GoForward(float movement, out List<Region> reachedRegions)
    {
        // if(movingState == null)
        //     return null;
        var completed = movingState.GoForward(movement, out reachedRegions);
        if (completed)
            movingState = null;
        if(reachedRegions.Count > 0)
        {
            MoveTo(reachedRegions[reachedRegions.Count - 1]);
        }
        foreach(var region in reachedRegions)
        {
            region.MoveTo(side);
        }
        return completed;
    }

    public bool isMoving {get => movingState != null;}
}


}