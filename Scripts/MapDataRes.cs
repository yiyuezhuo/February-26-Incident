namespace YYZ.Data.February26
{


using System.Collections.Generic;
using Godot;

/// <summary>
/// App layer Region representation.
/// </summary>
public class Region : Child<Region, Side, HashSet<Region>>, IContainer<List<Unit>, Unit>, MapKit.IRegion<Region>, MapKit.IArea
{
    // MapKit.IRegion properties
    public Color baseColor{get; set;}
    public Color remapColor{get; set;}
    public HashSet<Region> neighbors{get;set;}
    public Vector2 center{get; set;}
    
    public AreaTable.Data areaData; // Most of time, it's null.

    public List<Unit> children{get; set;} = new List<Unit>();

    string GetAreaDataDesc() => areaData != null ? areaData.ToString() : "[No Area Data]";

    // public override string ToString() => $"Region({GetAreaDataDesc()}, {center}, {children.Count}, {parent})";
    public override string ToString() => $"Region({GetAreaDataDesc()}, {center}, {children.Count})";
}


public class MapData : MapKit.MapData<Region>
{
    public MapData(Texture baseTexture, string path) : base(baseTexture, path) {}
}


public class MapDataRes : MapKit.MapDataRes, MapKit.IMapDataRes<Region>
{

    static MapData instance;
    public new MapData GetInstance() => instance != null ? instance : instance = new MapData(baseTexture, regionDataPath);
    MapKit.IMapData<Region> MapKit.IMapDataRes<Region>.GetInstance() => GetInstance();
}


}