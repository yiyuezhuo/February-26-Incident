namespace YYZ.Data.February26
{


using Godot;
using System;

public class ScenarioData
{
    /*
    static ScenarioData instance;

    public static ScenarioData GetInstance(ScenarioDataRes res)
    {
        if(instance == null)
        {
            instance = new ScenarioData(res);
        }
        return instance;
    }
    */

    // tables
    public LeaderTable leaderTable;
    public AreaTable areaTable;
    public CelebrityTable celebrityTable;
    public ObjectiveTable objectiveTable;
    public UnitTable unitTable;
    public AssignmentTable assignmentTable;
    
    // 
    public RegionLabelMap regionLabelMap;

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

        // Initialize map label

        regionLabelMap = new RegionLabelMap(res.regionLabelPath);
        
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

    ScenarioData instance;
    public ScenarioData GetInstance() => instance != null ? instance : instance = new ScenarioData(this);
}


} 