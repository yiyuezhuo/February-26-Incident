namespace YYZ.MapKit
{


using Godot;
using System;
using System.Collections.Generic;

using YYZ.MapKit.Widgets;


public interface IMapViewStateData
{
    Vector2 cameraPosition{get;set;}
    Vector2 cameraZoom{get;set;}
    bool required{get;set;}
}

/// <summary>
/// MapView controls all the map related UI (MapShower sprite, counters on the map etc...). MapView is relatively self-included,
/// so it has capture some events from the engine and sends them to MapShower, and it's expected to be contained in a `Viewport` or `ViewportContainer`. 
/// 
/// MapView has some "widgets", the outside should not access those widget directly.
///
/// MapView will store state to or restore state from a resource, since itself doesn't control scene switching, the store should be called from outside.
/// </summary>
public class MapView<TArea> : Node2D where TArea : IArea //, IMapView 
{
    [Export] NodePath mapShowerPath;
    [Export] NodePath cameraPath;
    [Export] NodePath arrowContainerPath;
    [Export] NodePath mapImageContainerPath;
    [Export] Resource uiStateDataRes;
    [Export] NodePath longArrowPath;

    // Persistent state
    protected IMapViewStateData uiStateData;

    protected MapShower<TArea> mapShower;
    protected Camera2D camera;

    // "Widgets"
    protected ArrowContainer arrowContainer;
    protected MapImageContainer mapImageContainer;
    protected LongArrow longArrow;

    protected bool dragging = false;

    Vector2 cameraBeginPos;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mapShower = (MapShower<TArea>)GetNode(mapShowerPath);
        camera = (Camera2D)GetNode(cameraPath);
        arrowContainer = (ArrowContainer)GetNode(arrowContainerPath);
        mapImageContainer = (MapImageContainer)GetNode(mapImageContainerPath);
        uiStateData = (IMapViewStateData)uiStateDataRes;
        longArrow = (LongArrow)GetNode(longArrowPath);

        Restore();

        cameraBeginPos = camera.Position;

        // arrowContainer.Hide();
        // mapImageContainer.Hide();
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

    /// <summary>
    /// Handle navigation and zooming
    /// </summary>
    public override void _UnhandledInput(InputEvent @event) // event is C# keyword
    {
        //GD.Print($"GameManager _UnhandledInput {@event}");

        var buttonEvent = @event as InputEventMouseButton;
        if(buttonEvent != null)
        {
            switch(buttonEvent.ButtonIndex)
            {
                case (int)ButtonList.Left: // 1
                    if(buttonEvent.Pressed)
                    {
                        GD.Print("Left Click");
                        var pos = mapShower.GetLocalMousePosition();
                        mapShower.OnClick(pos);
                    }
                    break;

                case (int)ButtonList.Right: // 2
                    // dragging = buttonEvent.Pressed;
                    // GD.Print($"dragging: {dragging}");
                    DraggingClickHandler(buttonEvent);
                    break;

                case (int)ButtonList.Middle: // 3 (wheel click)
                    GD.Print("Middle click");
                    break;

                case (int)ButtonList.WheelUp: // 4
                    if(buttonEvent.Pressed)
                    {
                        GD.Print("WheelUp Start");
                        camera.Zoom += new Vector2(0.1f, 0.1f);
                    }
                    break;

                case (int)ButtonList.WheelDown: // 5
                    if(buttonEvent.Pressed)
                    {
                        GD.Print("WheelDown Start");
                        var testZoom = camera.Zoom - new Vector2(0.1f, 0.1f);
                        if(testZoom.x > 0.01f & testZoom.y > 0.01f)
                        {
                            camera.Zoom = testZoom;
                        }
                    }
                    break;

                default:
                    GD.Print($"Dedefined actions or wheel? {buttonEvent.ButtonIndex}");
                    break;
            }
        }

        var motionEvent = @event as InputEventMouseMotion;
        if(motionEvent != null)
        {
            var pos = mapShower.GetLocalMousePosition();
            //GD.Print($"pos: {pos}");
            mapShower.OnRaycastHit(pos);
            if(dragging)
            {
                camera.Position -= motionEvent.Relative;
            }
        }
    }

    void DraggingClickHandler(InputEventMouseButton buttonEvent)
    {
        dragging = buttonEvent.Pressed;
        GD.Print($"dragging: {dragging}");
        if(buttonEvent.Pressed)
        {
            cameraBeginPos = camera.Position;
        }
        else
        {
            if(camera.Position.DistanceTo(camera.Position) < 2)
            {
                var pos = mapShower.GetLocalMousePosition();
                mapShower.OnRightClick(pos);
            }
        }
    }

    // Those "high-level" APIs are deprecated, we prefer to control Nodes directly at this time.
    public void SetArrows(IEnumerable<IArrowData> arrowDataIter) => arrowContainer.BindData(arrowDataIter);
    public void SetMapImages(IEnumerable<IMapImageData> mapImageIter) => mapImageContainer.BindData(mapImageIter);
    public void SetLongArrowPoints(IEnumerable<Vector2> controlPoints) => longArrow.SetCurvePositions(controlPoints);
    public void SetLongArrowVisible(bool visible) => longArrow.Visible = visible;

    public MapShower<TArea> GetMapShower() => mapShower;

    public Camera2D GetCamera() => camera;
}


}