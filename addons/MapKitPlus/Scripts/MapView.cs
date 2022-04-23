namespace YYZ.MapKitPlus
{


using Godot;
using YYZ.MapKitPlus.Widgets;
using System.Collections.Generic;
using System;

public interface IMapViewStateData
{
    Vector2 cameraPosition{get;set;}
    Vector2 cameraZoom{get;set;}
    bool required{get;set;}
}


public class MapView<TArea> : MapKit.MapView<TArea> where TArea : MapKit.IArea
{
    [Export] NodePath arrowContainerPath;
    [Export] NodePath mapImageContainerPath;
    [Export] NodePath longArrowPath;
    [Export] Resource uiStateDataRes;

    // "Widgets"
    protected ArrowContainer arrowContainer;
    protected MapImageContainer mapImageContainer;
    protected LongArrow longArrow;


    // Persistent state
    protected IMapViewStateData uiStateData;

    public override void _Ready()
    {
        base._Ready();

        arrowContainer = (ArrowContainer)GetNode(arrowContainerPath);
        mapImageContainer = (MapImageContainer)GetNode(mapImageContainerPath);
        longArrow = (LongArrow)GetNode(longArrowPath);

        uiStateData = (IMapViewStateData)uiStateDataRes;

        Restore();
    }

    void Restore() // It may be helpful to create a `TryResotre` to handle `uiStateData == null` situation.
    {
        if(uiStateData.required)
        {
            camera.Position = uiStateData.cameraPosition;
            camera.Zoom = uiStateData.cameraZoom;
        }
    }

    public void Store()
    {
        uiStateData.required = true;
        uiStateData.cameraPosition = camera.Position;
        uiStateData.cameraZoom = camera.Zoom;
    }

    // Those "high-level" APIs are deprecated, we prefer to control Nodes directly at this point.
    public void SetArrows(IEnumerable<IArrowData> arrowDataIter) => arrowContainer.BindData(arrowDataIter);
    public void SetMapImages(IEnumerable<IMapImageData> mapImageIter) => mapImageContainer.BindData(mapImageIter);
    public void SetLongArrowPoints(IEnumerable<Vector2> controlPoints) => longArrow.SetCurvePositions(controlPoints);
    public void SetLongArrowVisible(bool visible) => longArrow.Visible = visible;

}


}