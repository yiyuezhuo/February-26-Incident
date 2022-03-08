namespace YYZ.App
{

using Godot;
using YYZ.Data.February26;
using System.Collections;
using System.Collections.Generic;

public class StrategyView : Control
{
    [Export] ScenarioDataRes scenarioDataRes;
    [Export] NodePath mapViewPath;
    [Export] PackedScene mapImageScene;

    ScenarioData scenarioData;
    MapView mapView;
    public override void _Ready()
    {
        scenarioData = scenarioDataRes.GetInstance();
        mapView = (MapView)GetNode(mapViewPath);

        foreach(var unit in scenarioData.units)
        {
            var node = mapImageScene.Instance<TextureRect>();
            // var node = new TextureRect();
            node.RectPosition = scenarioData.mapData.MapToWorld(unit.parent.center);
            node.Texture = unit.children[0].portrait;

            GD.Print($"node={node}");

            mapView.AddChild(node);
        }
    }
}


}
