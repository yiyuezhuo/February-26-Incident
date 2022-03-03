namespace YYZ.MapKit
{


using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// A reference implementation for `IMapViewStateData`. Note `Resource` may be freed when scene switched (while most of time it will not be freed)
/// 
/// Those properties are expored for the debugging purpose. But if required field take its default value, false, those values will do nothing.
/// </summary>
public class UIStateData : Resource, IMapViewStateData
{
    [Export] public Vector2 cameraPosition{get;set;}
    [Export]public Vector2 cameraZoom{get;set;}
    [Export] public bool required{get;set;} = false; // C# 6.0 feature: https://stackoverflow.com/questions/40730/what-is-the-best-way-to-give-a-c-sharp-auto-property-an-initial-value
}


}