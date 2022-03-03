namespace YYZ.MapKit.Widgets
{


using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class LongArrow : Node2D
{
    [Export] NodePath bodyPath;
    [Export] NodePath headPath;
    [Export] float bodyEndPercent = 0.97f;
    [Export] float headBeginPercent = 0.93f; // continuous constraint: headBeginPercent < bodyEndPercent
    [Export] int size = 100;

    Line2D body;
    Line2D head;
    public override void _Ready()
    {
        body = (Line2D)GetNode(bodyPath);
        head = (Line2D)GetNode(headPath);

        /*
        var k = 100;
        var b = new Vector2(300, 300);

        // Test
        var testPoints = new Vector2[]{
            new Vector2(0.0f, 0.0f) * k + b,
            new Vector2(1.0f, 1.0f) * k + b,
            new Vector2(2.0f, 1.0f) * k + b,
            new Vector2(3.0f, 0.0f) * k + b,
            new Vector2(2.0f,-1.0f) * k + b
        };
        SetCurvePositions(testPoints);
        */
        
    }

    public void SetCurvePositions(IEnumerable<Vector2> controlPoints)
    {
        var points = controlPoints.ToList();

        var xArr = new double[points.Count];
        var yArr = new double[points.Count];
        var tArr = new double[points.Count];
        for(int i=0; i<points.Count; i++)
        {
            xArr[i] = points[i].x;
            yArr[i] = points[i].y;
            tArr[i] = ((double)i) / (points.Count-1);
        }
        // var fx = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkima(tArr, xArr);
        // var fy = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkima(tArr, yArr); // Akima requires at least 5 points and looks ugly, while "robust"
        var fx = MathNet.Numerics.Interpolation.CubicSpline.InterpolateNatural(tArr, xArr);
        var fy = MathNet.Numerics.Interpolation.CubicSpline.InterpolateNatural(tArr, yArr); // We can also consider Hermite InterpolateHermite

        // const int size = 100;
        
        var bodySize = (int)Mathf.Floor((size - 1) * bodyEndPercent);
        var headBeginIdx = (int)Mathf.Floor((size - 1) * headBeginPercent);
        //if(size - cutoff < 2)
        //    cutoff = size - 2;

        var bodyPoints = new Vector2[bodySize];
        var headPoints = new Vector2[size - headBeginIdx];
        for(int i=0; i<size; i++)
        {
            var t = ((double)i) / (size-1);
            var x = fx.Interpolate(t);
            var y = fy.Interpolate(t);
            var v = new Vector2((float)x, (float)y);

            if(i < bodySize)
                bodyPoints[i] = v;
            if(i >= headBeginIdx)
                headPoints[i-headBeginIdx] = v;
        }

        body.Points = bodyPoints;
        //head.Points = headPoints;
        head.Points = new Vector2[]{headPoints[0], headPoints[headPoints.Length-1]};

        /*
        var headSrc = xyArr[];
        var headDst = xyArr[size - 1];
        head.Points = new Vector2[]{headSrc, headDst};
        */

        //GD.Print($"{body.Points[0]}, {body.Points[body.Points.Length-1]}");
    }
}


}