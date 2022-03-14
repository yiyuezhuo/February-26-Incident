namespace YYZ.App
{


using Godot;
using System;
using YYZ.Data.February26;
using System.Collections.Generic;
using System.Linq;


public class DataValidator : Node
{
    [Export] ScenarioDataRes scenarioDataRes;
    [Export] VerboseLevel verboseLevel;

    enum VerboseLevel
    {
        None,
        Basic,
        Full
    }

    ScenarioData scenarioData;
    public override void _Ready()
    {
        // None verbose
        if(verboseLevel == VerboseLevel.None)
            return;
        
        // Basic verbose
        scenarioData = scenarioDataRes.GetInstance();

        var tables = new IDataTable[]{
            scenarioData.leaderTable, 
            scenarioData.areaTable,
            scenarioData.celebrityTable,
            scenarioData.objectiveTable,
            scenarioData.sideTable,
            scenarioData.unitTable,
            scenarioData.assignmentTable
        };
        
        foreach(var table in tables)

        {
            // var verbose = table == tables[tables.Length - 1];
            // var verbose = true;
            var verbose = false;
            table.Inspect(verbose);
            // Inspect(table, verbose);
        }

        Inspect(scenarioData.regionLabelMap, false);

        GD.Print($"scenarioData.regions.Count: {scenarioData.regions.Count()}");
        /*
        foreach(var region in scenarioData.regions)
            GD.Print(region);
        */
        if(verboseLevel == VerboseLevel.Basic)
            return;
        
        // Full verbose
        
        foreach(var containers in new IEnumerable< IContainerWeak<IEnumerable<object>, object> >[]{scenarioData.sides, scenarioData.regions, scenarioData.units})
        { 
            foreach(var container in containers)
            {
                foreach (var el in container.children)
                {
                    GD.Print($"{container} ∋ {el}");
                }
            }
        }
    }

    public void Inspect<TK, TV>(Dictionary<TV, TK> dict, bool verbose)
    {
        GD.Print($"{dict.GetType().Name}: Count -> {dict.Count}");
        if(verbose)
        {
            // GD.Print($"this={this}");
            foreach(var KV in dict)
            {
                // GD.Print($"KV={KV}");
                GD.Print($"{KV.Key} => {KV.Value}");
            }
        }
    }
}


}