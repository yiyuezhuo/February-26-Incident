namespace YYZ.Data.February26
{


using Godot;
using System;
using System.Collections.Generic;

public class ScenarioData
{
    // tables
    public LeaderTable leaderTable;
    public AreaTable areaTable;
    public CelebrityTable celebrityTable;
    public ObjectiveTable objectiveTable;
    public UnitTable unitTable;
    public AssignmentTable assignmentTable;
    
    // 
    public RegionLabelMap regionLabelMap;
    public MapData mapData;

    // public Dictionary<string, AreaTable.Data> areaTableByName = new Dictionary<string, AreaTable.Data>();

    public List<Region> regions = new List<Region>();

    public ScenarioData(ScenarioDataRes res)
    {
        // Initialize tables

        leaderTable = new LeaderTable(res.leaderTablePath, res.notionDataPath);
        areaTable = new AreaTable(res.areaTablePath, res.notionDataPath);
        celebrityTable = new CelebrityTable(res.celebrityTablePath, res.notionDataPath);
        objectiveTable = new ObjectiveTable(res.objectiveTablePath, res.notionDataPath);
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

        // Initialize Regions

        mapData = res.mapDataRes.GetInstance();
        foreach(var region in mapData.GetAllAreas())
            regions.Add(region);

        // Initialize map label

        regionLabelMap = new RegionLabelMap(res.regionLabelPath);

        // Link 
        var areaTableByName = new Dictionary<string, AreaTable.Data>();
        foreach(var area in areaTable.Values)
            areaTableByName[area.name] = area;
        
        foreach(var regionLabel in regionLabelMap.Values)
        {
            var area = areaTableByName[regionLabel.label];
            var posLeftTop = regionLabel.position;
            var pos = new Vector2(posLeftTop.x - mapData.width / 2, posLeftTop.y - mapData.height / 2);
            var colorNullable = mapData.Pos2Color(pos);
            var region = mapData.ColorToArea(colorNullable.Value);
            region.areaData = area;
        }
    }
}

public class ScenarioDataRes : Resource
{
    [Export(PropertyHint.Dir)] public string notionDataPath;
    [Export(PropertyHint.File)] public  string leaderTablePath;
    [Export(PropertyHint.File)] public string areaTablePath;
    [Export(PropertyHint.File)] public string celebrityTablePath;
    [Export(PropertyHint.File)] public string objectiveTablePath;
    [Export(PropertyHint.File)] public string unitTablePath;
    [Export(PropertyHint.File)] public string assignmentTablePath;
    [Export(PropertyHint.File)] public string regionLabelPath;
    [Export] public MapDataRes mapDataRes;

    ScenarioData instance;
    public ScenarioData GetInstance() => instance != null ? instance : instance = new ScenarioData(this);
}


} 