namespace YYZ.MapKit.Widgets
{


using Godot;
using System;

public class Arrow : Node2D
{
    [Export] NodePath bodyPath;
    [Export] NodePath headPath;
    [Export] float bodyPercent = 0.8f;
    [Export] float fullPercent = 0.3f;

    Line2D body;
    Line2D head;
    public override void _Ready()
    {
        body = (Line2D)GetNode(bodyPath);
        head = (Line2D)GetNode(headPath);
    }

    static Vector2 Vector2Lerp(Vector2 src, Vector2 dst, float p)
    {
        return new Vector2(Mathf.Lerp(src.x, dst.x, p), Mathf.Lerp(src.y, dst.y, p));
    }

    public void MoveTo(Vector2 src, Vector2 pointTo)
    {
        var pivot = Vector2Lerp(src, pointTo, bodyPercent);

        /*
        // Set value by index will not work:
        // https://godotengine.org/qa/121256/cannot-update-individual-points-of-line2d-in-c%23
        var bp = body.Points;
        var hp = head.Points;

        GD.Print($"before body->{bp.Length}->{bp}->{bp[0]}->{bp[1]}, head->{hp.Length}->{hp}->{hp[0]}->{hp[1]}");

        body.Points[0] = src;
        body.Points[1] = pivot;
        head.Points[0] = pivot;
        head.Points[1] = pointTo;

        GD.Print($"after body->{bp.Length}->{bp}->{bp[0]}->{bp[1]}, head->{hp.Length}->{hp}->{hp[0]}->{hp[1]}");
        */

        body.Points = new Vector2[]{src, pivot};
        head.Points = new Vector2[]{pivot, pointTo};

        var bp = body.Points;
        var hp = head.Points;

        // GD.Print($"after body->{bp.Length}->{bp}->{bp[0]}->{bp[1]}, head->{hp.Length}->{hp}->{hp[0]}->{hp[1]}");

    }

    public void MoveToCenter(Vector2 src, Vector2 pointTo)
    {
        var offsetPercent = (1 - fullPercent) / 2;
        var _src = Vector2Lerp(src, pointTo, offsetPercent);
        var _pointTo = Vector2Lerp(src, pointTo, 1 - offsetPercent);

        MoveTo(_src, _pointTo);
    }
}


}