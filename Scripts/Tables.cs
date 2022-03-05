namespace YYZ.Data.February26
{


using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Godot;
using System.Collections.Generic;
using static System.Net.WebUtility;

public interface IDataTable
{
    void Inspect(bool verbose); // Debug purpose
}

public class DataTable<RT, T> : Dictionary<string, T>, IDataTable where T : DataTable<RT, T>.IData, new()
{
    public interface IData
    {
        void Setup(RT item, string root);
        string id{get;}
    }

    public DataTable(string tablePath, string root)
    {
        var csvReader = YYZ.Text.GetCsvReader(tablePath);
        var items = csvReader.GetRecords<RT>();
        foreach(var item in items)
        {
            var d = new T();
            d.Setup(item, root);
            Add(d.id, d);
        }
    }

    protected static string UrlToId(string url) => UrlDecode(url);
    protected static Texture UrlToTexture(string url, string root)
    {
        if(url == "")
            return null;
        
        var portraitPath = root + "/" + UrlDecode(url);
        // GD.Print($"portraitPath={portraitPath}");
        return GD.Load<Texture>(portraitPath);
    }

    public void Inspect(bool verbose)
    {
        GD.Print($"{GetName()}: Count -> {Count}");
        if(verbose)
        {
            GD.Print($"this={this}");
            foreach(var KV in this)
            {
                GD.Print($"KV={KV}");
                GD.Print($"{KV.Key} => {KV.Value}");
            }
        }
    }

    public virtual string GetName() => "DataTable";
}

public class LeaderTable : DataTable<LeaderTable.Item, LeaderTable.Data> // Dictionary<string, LeaderTable.Leader>
{
    public LeaderTable(string tablePath, string root) : base(tablePath, root) {}

    public class Item
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

    public class Data : IData
    {
        Item item;

        public string name {get => item.Name;}
        public string nameJap{get => item.Name_Jap;}
        public string id{get; set;}
        public Texture portrait;

        public void Setup(Item item, string root)
        {
            this.item = item;
            this.id = UrlToId(item.ID);
            this.portrait = UrlToTexture(item.Portrait, root);
        }

        public override string ToString()
        {
            return $"Leader({name}, {nameJap})";
        }
    }

    public override string GetName() => "LeaderTable";
}

public class AreaTable : DataTable<AreaTable.Item, AreaTable.Data>
{
    public AreaTable(string tablePath, string root) : base(tablePath, root) {}

    public class Item
    {
        public string Name{get; set;}
        public string Tags{get; set;}
        public string ID{get; set;}
        public string Name_Jap{get; set;}
    }

    public class Data : IData
    {
        Item item;

        public string name{get => item.Name;}
        public bool movable;
        public string id{get;set;} // URL encoded
        public string nameJap{get => item.Name_Jap;}

        public void Setup(Item item, string _)
        {
            this.item = item;
            this.movable = item.Tags.Contains("unmovable");
            id = UrlToId(item.ID);
        }

        public override string ToString() => $"Area({name}, {nameJap})";
    }

    public override string GetName() => "AreaTable";
}

public class CelebrityTable : DataTable<CelebrityTable.Item, CelebrityTable.Data>
{
    public CelebrityTable(string tablePath, string root) : base(tablePath, root) {}

    public class Item
    {
        public string Name{get; set;}
        public string Name_Jap{get; set;}
        public string Portrait{get; set;}
    }

    public class Data : IData
    {
        Item item;
        public string name{get => item.Name;}
        public string nameJap{get => item.Name_Jap;}
        public Texture portrait;
        public string id{get => item.Name;} // TODO: We don't actually need reversing reference so we don't need id (as we don't need it in Notion).

        public void Setup(Item item, string root)
        {
            this.item = item;
            this.portrait = UrlToTexture(item.Portrait, root);
        }

        public override string ToString() => $"Celebrity({name}, {nameJap})";
    }

    public override string GetName() => "CelebrityTable";
}

public class ObjectiveTable : DataTable<ObjectiveTable.Item, ObjectiveTable.Data>
{
    public ObjectiveTable(string tablePath, string root) : base(tablePath, root) {}

    public class Item
    {
        public string Name{get; set;}
        public string Name_Jap{get; set;}
        public string Picture{get; set;}
        public string Area{get; set;}
        public string ID{get; set;}
    }

    public class Data : IData
    {
        Item item;

        public string name{get => item.Name;}
        public string nameJap{get => item.Name_Jap;}
        public string id{get; set;}
        public Texture picture;
        public AreaTable.Data area; // This kind of value will be "linked" after initialization (include `Setup`).
        public string areaId;

        public void Setup(Item item, string root)
        {
            this.item = item;

            picture = UrlToTexture(item.Picture, root);
            id = UrlToId(item.ID);
            areaId = UrlToId(item.Area);
            // `area` will be linked in later phase.
        }

        public override string ToString() => $"Objective({name}, {nameJap})";
    }

    public override string GetName() => "ObjectiveTable";
}

public class UnitTable : DataTable<UnitTable.Item, UnitTable.Data>
{
    public UnitTable(string tablePath, string root) : base(tablePath, root) {}

    public class Item
    {
        public string Name{get; set;}
        public string Name_Jap{get; set;}
        public string Strength{get; set;}
        public string ID{get; set;}
        public string Area{get; set;}
        // [Name("Related to Leader Assignments Table (Unit)")] public string Leaders; // Parse assignment info from here instead of original file.
    }

    public class Data : IData
    {
        Item item;

        public string name{get => item.Name;}
        public string nameJap{get => item.Name_Jap;}
        public float strength;
        public string id{get; set;}
        public AreaTable.Data area;
        public string areaId;
        // public List<LeaderTable.Data> leaders = new List<LeaderTable.Data>();
        // public string[] leadersStringArray;

        public void Setup(Item item, string root)
        {
            this.item = item;
            strength = float.Parse(item.Strength);
            id = UrlToId(item.ID);
            areaId = UrlToId(item.Area);
            // GD.Print($"item.Leaders={item.Leaders}");
            // leadersStringArray = item.Leaders.Split(" ");
        }

        public override string ToString() => $"Unit({name}, {nameJap})";
    }

    public override string GetName() => "UnitTable";
}


public class AssignmentTable : DataTable<AssignmentTable.Item, AssignmentTable.Data>
{
    public AssignmentTable(string tablePath, string root) : base(tablePath, root) {}

    public class Item
    {
        public string Leader{get; set;}
        public string Unit{get; set;}
    }

    public class Data : IData
    {
        Item item;
        public string leaderId;
        public string unitId;
        public LeaderTable.Data leader;
        public UnitTable.Data unit;

        public string id{get => leaderId;} // though maybe we don't need key and save it using Dictionary...

        public void Setup(Item item, string root)
        {
            this.item = item;

            leaderId = UrlToId(item.Leader);
            unitId = UrlToId(item.Unit);
        }
    }

    public override string GetName() => "AssignmentTable";
}



}