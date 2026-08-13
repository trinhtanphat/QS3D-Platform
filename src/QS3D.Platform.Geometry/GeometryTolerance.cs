namespace QS3D.Platform.Geometry;

public readonly struct GeometryTolerance : IEquatable<GeometryTolerance>
{
    public GeometryTolerance(double linearM, double angularRadians, double relative)
    {
        LinearM = Numeric.RequireNonNegativeFinite(linearM, nameof(linearM));
        AngularRadians = Numeric.RequireNonNegativeFinite(angularRadians, nameof(angularRadians));
        Relative = Numeric.RequireNonNegativeFinite(relative, nameof(relative));
    }

    public static GeometryTolerance Default => new GeometryTolerance(1e-9d, 1e-10d, 1e-12d);

    public double LinearM { get; }
    public double AngularRadians { get; }
    public double Relative { get; }

    public bool NearlyEqualDistance(double leftM, double rightM)
        => NearlyEqual(leftM, rightM, LinearM, Relative);

    public bool NearlyEqualAngle(double leftRadians, double rightRadians)
        => NearlyEqual(leftRadians, rightRadians, AngularRadians, Relative);

    public bool SamePoint(Point3 left, Point3 right)
        => NearlyEqualDistance(left.X, right.X)
            && NearlyEqualDistance(left.Y, right.Y)
            && NearlyEqualDistance(left.Z, right.Z);

    public bool IsZeroLength(double lengthM)
    {
        Numeric.RequireNonNegativeFinite(lengthM, nameof(lengthM));
        return lengthM <= LinearM;
    }

    public bool Equals(GeometryTolerance other)
        => LinearM.Equals(other.LinearM)
            && AngularRadians.Equals(other.AngularRadians)
            && Relative.Equals(other.Relative);

    public override bool Equals(object? obj) => obj is GeometryTolerance other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = LinearM.GetHashCode();
            hash = (hash * 397) ^ AngularRadians.GetHashCode();
            hash = (hash * 397) ^ Relative.GetHashCode();
            return hash;
        }
    }

    private static bool NearlyEqual(double left, double right, double absolute, double relative)
    {
        Numeric.RequireFinite(left, nameof(left));
        Numeric.RequireFinite(right, nameof(right));
        if (left.Equals(right)) return true;

        var difference = Math.Abs(left - right);
        if (Numeric.IsFinite(difference) && difference <= absolute) return true;

        var scale = Math.Max(Math.Abs(left), Math.Abs(right));
        if (scale == 0d) return true;
        var normalizedDifference = Math.Abs((left / scale) - (right / scale));
        var normalizedAbsolute = absolute / scale;
        return normalizedDifference <= relative + normalizedAbsolute;
    }
}
