namespace YYZ.App
{


using YYZ.Data.February26;
using System.Collections.Generic;
using System.Linq;
using System;
using Godot;

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
    public List<Region> path = new List<Region>(); // (Current, ..., Destination)
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
}

public abstract class Unit : Child<Unit, Region, List<Unit>>, IContainer<List<Leader>, Leader>, UnitInfoPad.IData, UnitBar.IData, UnitPad.IData
{
    public abstract string name{get;}
    public abstract string nameJap{get;}

    public Side side;
    public float strength{get; set;}
    public float fatigue{get; set;}

    public float command{get => children.Sum(leader => leader.command);}
    public Texture portrait{get => children[0].portrait;}
    public Vector2 center{get => parent.center;}

    IEnumerable<LeaderPad.IData> UnitBar.IData.children{get => children;}

    // "Constants"

    float moveSpeedMPerMin = 25; // 25m/min -> 1.5km/h
    static float mPerPixel = 6; // 6m = 1 unit pixel distance
    float moveSpeedPiexelPerMin {get => moveSpeedMPerMin / mPerPixel;}

    public MovingState movingState{get;} = new MovingState();

    public event EventHandler<MovePath> moveStateUpdated;
    public event EventHandler childrenUpdated;

    public List<Leader> children{get; set;} = new List<Leader>();

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
		//var src = detachRequest.src;
		// var selectedLeaderList = detachRequest.selectedLeaderList;
		// var strength = detachRequest.strength;

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

    public void GoForward() => GoForward(moveSpeedPiexelPerMin);

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
    public override string name{get => "";} // TODO: C# doesn't allow adding setter in child class???
    // CC: https://stackoverflow.com/questions/82437/why-is-it-impossible-to-override-a-getter-only-property-and-add-a-setter
    // Since These properties are not needed at this time, 
    public override string nameJap{get => "";}

    public UnitProcedure(Side side, float strength, float fatigue)
    {
        this.side = side;
        this.strength = strength;
        this.fatigue = fatigue;
    }
}


}