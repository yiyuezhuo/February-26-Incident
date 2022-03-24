namespace YYZ.App
{


using MathNet.Numerics.Distributions;
using System;

public class TruncatedNormal
{
    Random random;
    double left;
    double right;
    double mean;
    double stddev;

    double leftCDF;
    double rightCDF;

    public TruncatedNormal(Random random, double left, double right, double mean, double stddev)
    {
        this.random = random;
        this.mean = mean;
        this.stddev = stddev;

        leftCDF = Normal.CDF(mean, stddev, left);
        rightCDF = Normal.CDF(mean, stddev, right);
    }
    public double Sample()
    {
        var p = random.NextDouble();
        var pt = (rightCDF - leftCDF) * p + leftCDF;
        var x = Normal.InvCDF(mean, stddev, pt);
        return x;
    }

    public override string ToString() => $"TruncatedNormal({left}, {right}, {mean}, {stddev})";
}


}