namespace YYZ.MapKit.Widgets
{


using Godot;
using System;

public enum PositionHint
{
    Center,
    Left,
    Right
}

public interface IMapImageData
{
    Vector2 pos{get;}
    Texture texture{get;}
    PositionHint positionHint{get;}
}


public class MapImageContainer : AutoExpandedContainer<TextureRect, IMapImageData>
{
    [Export] PackedScene strategyIconScene;
    protected override PackedScene GetRefScene() => strategyIconScene;
    protected override void ApplyData(TextureRect child, IMapImageData data)
    {
        child.Texture = data.texture;
        var offset = child.RectSize;
        switch(data.positionHint)
        {
            case PositionHint.Center:
                child.FlipH = false;
                child.RectPosition = data.pos - offset / 2; // Control Node start with left top instead of center.
                break;
            case PositionHint.Left:
                child.FlipH = false;
                child.RectPosition = new Vector2(data.pos.x - offset.x, data.pos.y - offset.y / 2);
                break;
            case PositionHint.Right:
                child.RectPosition = new Vector2(data.pos.x, data.pos.y - offset.y / 2);
                child.FlipH = true;
                break;
        }
        
    }
}


}