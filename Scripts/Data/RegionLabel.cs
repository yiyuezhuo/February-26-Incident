namespace YYZ.Data.February26
{


using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

// using UnityEngine;
using Godot;


/// <summmary>
/// Read LabelMe format data.
/// </summary>
public class RegionLabelMap : Dictionary<string, RegionLabelMap.Data>
{
    public RegionLabelMap(string path)
    {
        var jsonString = YYZ.Text.Read(path);
        var shapes = JsonConvert.DeserializeObject<RegionLabelData>(jsonString).shapes;
        foreach(var shape in shapes)
        {
            var p = shape.points[0];
            var position = new Vector2(p[0], p[1]);
            Add(shape.label, new Data(shape.label, position));
        }
    }

    public class RegionLabelShape
    {
        public string label;
        public float[][] points;
    }

    public class RegionLabelData
    {
        public int imageHeight;
        public int imageWidth;
        public string version;
        public RegionLabelShape[] shapes;
    }

    public class Data
    {
        public string label;
        public Vector2 position;
        public Data(string label, Vector2 position)
        {
            this.label = label;
            this.position = position;
        }
    }
}



}