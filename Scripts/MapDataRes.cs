namespace YYZ.Data.February26
{


using System.Collections.Generic;
using Godot;

public class Region : MapKit.Region, MapKit.IRegion<Region>
{
    
    public AreaTable.Data areaData;
    public new HashSet<Region> neighbors{get; set;}

    string GetAreaDataDesc() => areaData != null ? areaData.ToString() : "[No Area Data]";

    public override string ToString() => $"Region({GetAreaDataDesc()}, {center})";
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