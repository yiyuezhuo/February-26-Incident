namespace YYZ.Data.February26
{


using Godot;
using System;
using System.Collections.Generic;
using YYZ.Collections;

// "App layer" objects, which is detached from `DataTable.Data`.

public class Side : IContainer<HashSet<Region>, Region>
{
    SideTable.Data data;

    public string name{get => data.name;}
    public string nameJap{get => data.nameJap;}
    public Color color{get => color;}
    public Texture picture{get => picture;}

    public Side(SideTable.Data data)
    {
        this.data = data;
    }

    public HashSet<Region> children{get; set;} = new HashSet<Region>();

    public override string ToString() => $"Side({name}, {nameJap}, {children.Count})";
}

public class Unit : Child<Unit, Region, List<Unit>>, IContainer<List<Leader>, Leader>
{
    UnitTable.Data data;

    public string name{get => data.name;}
    public string nameJap{get => data.nameJap;}
    public float strength{get; set;}

    public Unit(UnitTable.Data data)
    {
        this.data  = data;
        this.strength = data.strength;
    }

    public List<Leader> children{get; set;} = new List<Leader>();

    public override string ToString() => $"Unit({name}, {nameJap}, {children.Count})";
}

public class Leader : Child<Leader, Unit, List<Leader>>
{
    LeaderTable.Data data;

    public string name {get => data.name;}
    public string nameJap{get => data.nameJap;}
    public Texture portrait{get => data.portrait;}

    public Leader(LeaderTable.Data data)
    {
        this.data = data;
    }

    // public Unit parent;
    public override string ToString() => $"Leader({name}, {nameJap})";
}

public class ScenarioData
{
    // tables
    public LeaderTable leaderTable;
    public AreaTable areaTable;
    public CelebrityTable celebrityTable;
    public ObjectiveTable objectiveTable;
    public SideTable sideTable;
    public UnitTable unitTable;
    public AssignmentTable assignmentTable;
    
    // 
    public RegionLabelMap regionLabelMap;
    public MapData mapData;

    public HashSet<Side> sides = new HashSet<Side>();
    public HashSet<Region> regions = new HashSet<Region>();
    public HashSet<Unit> units = new HashSet<Unit>();
    public HashSet<Leader> leaders = new HashSet<Leader>();

    public Side rebelSide;
    public Side govSide;

    public ScenarioData(ScenarioDataRes res)
    {
        mapData = res.mapDataRes.GetInstance();
        SetupTableData(res, out var areaToRegion);
        SetupAPPData(areaToRegion);
    }

    void SetupTableData(ScenarioDataRes res, out Dictionary<AreaTable.Data, Region> areaToRegion)
    {
        // Initialize tables

        leaderTable = new LeaderTable(res.leaderTablePath, res.notionDataPath);
        areaTable = new AreaTable(res.areaTablePath, res.notionDataPath);
        celebrityTable = new CelebrityTable(res.celebrityTablePath, res.notionDataPath);
        objectiveTable = new ObjectiveTable(res.objectiveTablePath, res.notionDataPath);
        sideTable = new SideTable(res.sideTablePath, res.notionDataPath);
        unitTable = new UnitTable(res.unitTablePath, res.notionDataPath);
        assignmentTable = new AssignmentTable(res.assignmentTablePath, res.notionDataPath);

        // Link
        foreach(var objective in objectiveTable.Values)
        {
            if(objective.areaId != "")
                objective.area = areaTable[objective.areaId];
        }

        foreach(var unit in unitTable.Values)
        {
            unit.area = areaTable[unit.areaId];
        }

        foreach(var assignment in assignmentTable.Values)
        {
            assignment.leader = leaderTable[assignment.leaderId];
            assignment.unit = unitTable[assignment.unitId];
        }

        // Initialize map label

        regionLabelMap = new RegionLabelMap(res.regionLabelPath);

        // Link map label

        var areaTableByName = new Dictionary<string, AreaTable.Data>();
        foreach(var area in areaTable.Values)
            areaTableByName[area.name] = area;
        
        areaToRegion = new Dictionary<AreaTable.Data, Region>();
        foreach(var regionLabel in regionLabelMap.Values)
        {
            var area = areaTableByName[regionLabel.label];
            var posLeftTop = regionLabel.position;
            var pos = new Vector2(posLeftTop.x - mapData.width / 2, posLeftTop.y - mapData.height / 2);
            var colorNullable = mapData.Pos2Color(pos);
            var region = mapData.ColorToArea(colorNullable.Value);
            region.areaData = area;

            areaToRegion[area] = region;
        }
    }

    void SetupAPPData(Dictionary<AreaTable.Data, Region> areaToRegion)
    {
        // Initialize App layer data

        // Side

        foreach(var sideData in sideTable.Values)
        {
            var side = new Side(sideData);
            sides.Add(side);

            // sideRegionMembership.CreateMembership(side);
            if(sideData.isRebel)
                rebelSide = side;
            else
                govSide = side;
        }

        // Region

        foreach(var region in mapData.GetAllAreas())
        {
            regions.Add(region);
            if(region.areaData == null || region.areaData.movable)
                region.EnterTo(rebelSide);
        }

        // Unit
        var unitDataToUnit = new Dictionary<UnitTable.Data, Unit>();
        foreach(var unitData in unitTable.Values) // Here we assume all units are rebels.
        {
            var unit = new Unit(unitData);
            units.Add(unit);
            unitDataToUnit[unitData] = unit;

            var region = areaToRegion[unitData.area];
            unit.EnterTo(region);
        }

        // Leader
        var leaderDataToLeader = new Dictionary<LeaderTable.Data, Leader>();
        foreach(var leaderData in leaderTable.Values)
        {
            var leader = new Leader(leaderData);
            leaders.Add(leader);
            leaderDataToLeader[leaderData] = leader;
            // EnterTo is delegated to assignment
        }

        // Assignmemt
        foreach(var assignment in assignmentTable.Values)
        {
            leaderDataToLeader[assignment.leader].EnterTo(unitDataToUnit[assignment.unit]);
        }

        // TODO: Rebels occupy inital regions
    }
}

public class ScenarioDataRes : Resource
{
    [Export(PropertyHint.Dir)] public string notionDataPath;
    [Export(PropertyHint.File)] public  string leaderTablePath;
    [Export(PropertyHint.File)] public string areaTablePath;
    [Export(PropertyHint.File)] public string celebrityTablePath;
    [Export(PropertyHint.File)] public string objectiveTablePath;
    [Export(PropertyHint.File)] public string sideTablePath;
    [Export(PropertyHint.File)] public string unitTablePath;
    [Export(PropertyHint.File)] public string assignmentTablePath;
    [Export(PropertyHint.File)] public string regionLabelPath;
    [Export] public MapDataRes mapDataRes;

    ScenarioData instance;
    public ScenarioData GetInstance() => instance != null ? instance : instance = new ScenarioData(this);
}


}