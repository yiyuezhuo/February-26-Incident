namespace YYZ.App
{


using YYZ.Data.February26;
using System.Collections.Generic;
using Godot;
using System;
using System.Linq;

/// <summary>
/// App layer Region representation.
/// </summary>
public class Region : Child<Region, Side, HashSet<Region>>, IContainer<List<Unit>, Unit>, MapKit.IRegion<Region>, MapKit.IArea, StackBar.IData
{
    // MapKit.IRegion properties
    public Color baseColor{get; set;}
    public Color remapColor{get; set;}
    public HashSet<Region> neighbors{get;set;}
    public Vector2 center{get; set;}
    
    public AreaTable.Data areaData; // Most of time, it's null.

    public List<Unit> children{get; set;} = new List<Unit>();
    IEnumerable<UnitPad.IData> StackBar.IData.children{get => children;}
    public event EventHandler<Region> childrenUpdated;
    public event EventHandler<Unit> childrenEntered;

    string GetAreaDataDesc() => areaData != null ? areaData.ToString() : "[No Area Data]";
    public string ToLabelString() => areaData != null ? areaData.name : center.ToString();

    // public override string ToString() => $"Region({GetAreaDataDesc()}, {center}, {children.Count}, {parent})";
    public override string ToString() => $"Region({GetAreaDataDesc()}, {center}, {children.Count}, {isEdge})";

    public float DistanceTo(Region other) => center.DistanceTo(other.center);
    public bool movable{get => areaData == null || areaData.movable;}
    public bool isEdge;

    public void OnChildrenUpdated()
    {
        childrenUpdated?.Invoke(this, this);
    }
    public void OnChildrenEntered(Unit unit)
    {
        childrenEntered?.Invoke(this, unit);
    }

    HashSet<Unit> overlapSet = new HashSet<Unit>();

    public void StepPre()
    {
        overlapSet.Clear();
    }

    public void AddOverlap(Unit unit)
    {
        overlapSet.Add(unit);
    }

    public IEnumerable<Unit> GetOverlap() => overlapSet;
}

public class RegionData : MapKit.RegionData
{
    public bool IsEdge{get; set;}
}

public class RegionMapFactory : MapKit.RegionMapFactory<RegionData, Region>
{
    protected override void Extract(RegionData regionData, Region region)
    {
        base.Extract(regionData, region);
        region.isEdge = regionData.IsEdge;
    }
}

public class MapData : MapKit.MapData<RegionData, Region>, PathFinding.IGraph<Region>
{
    protected override MapKit.RegionMapFactory<RegionData, Region> regionMapFactory{get => new RegionMapFactory();}

    public MapData(Texture baseTexture, string path) : base(baseTexture, path)
    {
        foreach(var region in areaMap.Values)
            region.center = MapToWorld(region.center); // We transform Map coordinates to world for simplicity.
    }

    public float MoveCost(Region src, Region dst) => src.DistanceTo(dst);
    public float EstimateCost(Region src, Region dst) => MoveCost(src, dst);
    public IEnumerable<Region> Neighbors(Region src)
    {
        foreach(var region in src.neighbors)
        {
            var movable = region.areaData == null || region.areaData.movable;
            if(movable)
                yield return region;
        }
    }

    public Region SampleEdgeRegion()
    {
        var edgeRegionList = (from region in areaMap.Values where region.isEdge && region.movable select region).ToList();
        return edgeRegionList[YYZ.Random.Next() % edgeRegionList.Count];
    }
}


public class MapDataRes : MapKit.MapDataRes, MapKit.IMapDataRes<Region>
{

    static MapData instance;
    public new MapData GetInstance() => instance != null ? instance : instance = new MapData(baseTexture, regionDataPath);
    MapKit.IMapData<Region> MapKit.IMapDataRes<Region>.GetInstance() => GetInstance();
}


}