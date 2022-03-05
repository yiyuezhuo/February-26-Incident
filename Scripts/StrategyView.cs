namespace YYZ.App
{

using Godot;
using YYZ.Data.February26;
using System.Collections;
// using System.Collections.Generic;

public class StrategyView : Control
{
    [Export] ScenarioDataRes scenarioDataRes;

    ScenarioData scenarioData;
    public override void _Ready()
    {
        scenarioData = scenarioDataRes.GetInstance();

        var tables = new IDataTable[]{
            scenarioData.leaderTable, 
            scenarioData.areaTable,
            scenarioData.celebrityTable,
            scenarioData.objectiveTable,
            scenarioData.unitTable
        };

        foreach(var table in tables)
        {
            var verbose = table == tables[tables.Length - 1];
            // var verbose = true;
            table.Inspect(verbose);
        }

    }
}


}
