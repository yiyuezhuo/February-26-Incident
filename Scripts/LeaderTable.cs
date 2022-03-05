namespace YYZ.Data.February26
{


using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Godot;
using System.Collections.Generic;

public class LeaderTable : Dictionary<string, LeaderTable.Leader>
{
    public class LeaderTableItem
    {
        public string Name {get;set;}
        public string Name_Jap {get;set;}
        public string Portrait {get;set;} // URL encoded
        public string Job {get;set;}
        public string Job_Jap {get;set;}
        public string Rank {get;set;}
        public string Rank_Jap {get;set;}
        public string Membership {get;set;}
        public string Membership_Jap {get;set;}
        // TODO: We don't really need "Related to ***" attribute, test if we can just ignore it.
        // [Name("Related to Scenario Table (Column)")] public string ScenarioTable {get;set;}
        public string ID {get;set;} // URL encoded
    }

    public class Leader
    {
        LeaderTableItem data;

        public string name {get => data.Name;}
        public string nameJap{get => data.Name_Jap;}
        public string id{get => data.ID;} // We don't need to decode url of ID.
        public Texture portrait;

        public Leader(LeaderTableItem data, string root)
        {
            this.data = data;

            if(data.Portrait != "")
            {
                var portraitPath = root + "/" + System.Net.WebUtility.UrlDecode(data.Portrait);
                // GD.Print($"portraitPath={portraitPath}");
                portrait = GD.Load<Texture>(portraitPath);
            }
        }

        public override string ToString()
        {
            return $"Leader({name}, {nameJap})";
        }
    }

    public LeaderTable(string csvString, string root)
    {
        var csvReader = YYZ.Text.GetCsvReader(csvString);
        var leaderTableItems = csvReader.GetRecords<LeaderTableItem>();
        foreach(var leaderTableItem in leaderTableItems)
        {
            var leader = new Leader(leaderTableItem, root);
            Add(leader.id, leader);
        }
    }
}


}