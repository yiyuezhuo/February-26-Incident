namespace YYZ.App
{

using Godot;
using YYZ.Data.February26;

public class StrategyView : Control
{
    [Export] ScenarioDataRes scenarioDataRes;

    ScenarioData scenarioData;
    public override void _Ready()
    {
        scenarioData = scenarioDataRes.GetInstance();

        GD.Print($"Leader Table: {scenarioData.leaderTable.Count}");
        
        foreach(var KV in scenarioData.leaderTable)
        {
            GD.Print($"{KV.Key} => {KV.Value}");
        }
    }
}


}
