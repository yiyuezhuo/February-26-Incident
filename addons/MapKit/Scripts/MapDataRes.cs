namespace YYZ.MapKit
{
// A "reference" implementation is provided here, while MapShower/MapView themself are not required to use them.

using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class Region : IArea
{
    public Color baseColor;
    public Color remapColor{get;set;}
    public HashSet<Region> neighbors;
    public Vector2 center;

    public override string ToString()
    {
        return $"Region({baseColor}, {center})";
    }

    int ToId()
    {
        return remapColor.g8 * 256 + remapColor.r8;
    }

    static Color Int4ToColor(int[] arr)
    {
        return Color.Color8((byte)arr[0], (byte)arr[1], (byte)arr[2], (byte)arr[3]);
    }

    class RegionData
    {
        public int[] BaseColor;
        public int[] RemapColor;
        public int Points;
        public float X;
        public float Y;
        public int[][] Neighbors;
    }

    class RegionDataResult
    {
        public RegionData[] Areas;

        public static RegionDataResult CreateFromJSON(string jsonString)
        {
            return JsonConvert.DeserializeObject<RegionDataResult>(jsonString);
        }
    }
    /// <summary>
    /// Get a dictionary which maps baseColor to Region.
    /// </summary>
    public static Dictionary<Color, Region> GetRegionMap(string jsonString)
    {
        var regionMap = new Dictionary<Color, Region>();
        var neighborsMap = new Dictionary<Region, int[][]>();

        RegionData[] regionDataList = RegionDataResult.CreateFromJSON(jsonString).Areas;
        // Debug.Log($"regionDataList.Length={regionDataList.Length}");
        foreach(var regionData in regionDataList)
        {
            var region = new Region();
            region.baseColor = Int4ToColor(regionData.BaseColor);
            region.remapColor = Int4ToColor(regionData.RemapColor);
            region.neighbors = new HashSet<Region>();
            neighborsMap[region] = regionData.Neighbors;

            region.center = new Vector2(regionData.X, regionData.Y);
            regionMap[region.baseColor] = region;
        }

        foreach(var KV in regionMap)
        {
            var region = KV.Value;
            foreach(int[] colorInt4 in neighborsMap[region]){
                region.neighbors.Add(regionMap[Int4ToColor(colorInt4)]);
            }
        }

        return regionMap;
    }
}

public class MapData : IMapData<Region>
{
    static MapData instance;

    public Image baseImage;
    public int width{get; set;}
    public int height{get; set;}
    Dictionary<Color, Region> areaMap = new Dictionary<Color, Region>();

    MapData(Texture baseTexture, string path)
    {
        var regionJsonString = YYZ.Text.Read(path);

        baseImage = baseTexture.GetData();
        baseImage.Lock();

        width = baseImage.GetWidth();
        height = baseImage.GetHeight();

        areaMap = Region.GetRegionMap(regionJsonString);
    }

    public static MapData GetInstance(Texture baseTexture, string regionJsonString)
    {
        if(instance == null)
            instance = new MapData(baseTexture, regionJsonString);
        return instance;
    }

    Vector2 WorldToMap(Vector2 worldPos)
    {
        return new Vector2(worldPos.x + width / 2, worldPos.y + height / 2);
    }
    public Color? Pos2Color(Vector2 worldPos)
    {
        // {0, 0} is assumed to be "center"
        var mapPos = WorldToMap(worldPos);
        int x = (int)Mathf.Floor(mapPos.x);
        int y = (int)Mathf.Floor(mapPos.y);

        if(x <= 0 || x >= width-1 || y<=0 || y >= height-1){
            return null;
        }

        // return baseArr[x + y * width];
        return baseImage.GetPixel(x, y);
    }
    
    public Region ColorToArea(Color color)
    {
        //GD.Print($"color={color}, areaMap.Count={areaMap.Count}, areaMap.Keys={areaMap.Keys}");
        return areaMap[color];
    }
}

/// <summary>
/// A reference implementation for `IMapData<IArea>`, though in real game IMapData will be implemented in a more complex class
/// and the `MapDataCore` may be more useful.
/// </summary>
public class MapDataRes : Resource, IMapDataRes<Region> // YYZ.ResourceNeedSetup, 
{
    [Export] Texture baseTexture;
    [Export(PropertyHint.File)] string regionDataPath;

    // MapDataCore core;
    /*
    protected override void Setup()
    {
        core = new MapDataCore(baseTexture, YYZ.Text.Read(regionDataPath));
    }
    */
    public MapData GetInstance() => MapData.GetInstance(baseTexture, regionDataPath);
    IMapData<Region> IMapDataRes<Region>.GetInstance() => GetInstance();

    /*
    public int width{get => core.width;}
    public int height{get => core.height;}
    public Color? Pos2Color(Vector2 worldPos) => core.Pos2Color(worldPos);
    public Region ColorToArea(Color color) => core.ColorToArea(color);
    */
}


}