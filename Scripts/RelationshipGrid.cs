namespace YYZ.App
{


using Godot;
using System;
using System.Collections.Generic;

public class RelationshipGrid : GridContainer
{
    [Export] PackedScene buttonHeaderScene;
    [Export] PackedScene itemScene;

    public override void _Ready()
    {
        
    }

    public void SetData(IData data)
    {
        Columns = data.sideNames.Count + 1;

    }

    public interface IRelationShip
    {
        int relationship{get;}
        Color color{get;}
    }

    /*
    // We favor object over a meanless empty interface.
    public interface ISide
    {
    }
    */

    public interface IData
    {
        List<object> sideNames{get;}
        Dictionary<object, IRelationShip> relation{get;}
    }
}


}