namespace YYZ.Data.February26
{


using Godot;
using System;

public class ScenarioData
{
    static ScenarioData instance;

    public static ScenarioData GetInstance(ScenarioDataRes res)
    {
        if(instance == null)
        {
            instance = new ScenarioData(res);
        }
        return instance;
    }

    public LeaderTable leaderTable;

    public ScenarioData(ScenarioDataRes res)
    {
        leaderTable = new LeaderTable(res.leaderTablePath, res.notionDataPath);
    }
}

public class ScenarioDataRes : Resource
{
    [Export(PropertyHint.Dir)] public string notionDataPath;
    [Export(PropertyHint.File)] public string leaderTablePath;

    public ScenarioData GetInstance() => ScenarioData.GetInstance(this);
}


} 