namespace QS3D.Platform.Geometry;

public static class Numeric
{
    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    public static double RequireFinite(double value, string parameterName)
    {
        if (!IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
        return value;
    }

    public static double RequireNonNegativeFinite(double value, string parameterName)
    {
        RequireFinite(value, parameterName);
        if (value < 0d)
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be non-negative.");
        return value;
    }

    internal static double Length3(double x, double y, double z)
    {
        var scale = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
        if (scale == 0d)
            return 0d;
        var sx = x / scale;
        var sy = y / scale;
        var sz = z / scale;
        var result = scale * Math.Sqrt((sx * sx) + (sy * sy) + (sz * sz));
        if (!IsFinite(result))
            throw new OverflowException("Vector length is not representable as a finite double.");
        return result;
    }
}

public readonly struct Point3 : IEquatable<Point3>
{
    public Point3(double x, double y, double z = 0d)
    {
        X = Numeric.RequireFinite(x, nameof(x));
        Y = Numeric.RequireFinite(y, nameof(y));
        Z = Numeric.RequireFinite(z, nameof(z));
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static Vector3 operator -(Point3 left, Point3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    public static Point3 operator +(Point3 point, Vector3 vector) => new(point.X + vector.X, point.Y + vector.Y, point.Z + vector.Z);
    public double DistanceTo(Point3 other) => (this - other).Length;
    public bool Equals(Point3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is Point3 other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + X.GetHashCode();
            hash = (hash * 31) + Y.GetHashCode();
            hash = (hash * 31) + Z.GetHashCode();
            return hash;
        }
    }
    public override string ToString() => $"({X:R}, {Y:R}, {Z:R})";
}

public readonly struct Vector3 : IEquatable<Vector3>
{
    public Vector3(double x, double y, double z = 0d)
    {
        X = Numeric.RequireFinite(x, nameof(x));
        Y = Numeric.RequireFinite(y, nameof(y));
        Z = Numeric.RequireFinite(z, nameof(z));
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public double Length => Numeric.Length3(X, Y, Z);
    public bool Equals(Vector3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + X.GetHashCode();
            hash = (hash * 31) + Y.GetHashCode();
            hash = (hash * 31) + Z.GetHashCode();
            return hash;
        }
    }
}

public readonly struct BoundingBox3 : IEquatable<BoundingBox3>
{
    public BoundingBox3(Point3 min, Point3 max)
    {
        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
            throw new ArgumentException("Minimum point must not exceed maximum point.", nameof(min));
        Min = min;
        Max = max;
    }

    public Point3 Min { get; }
    public Point3 Max { get; }
    public static BoundingBox3 FromPoints(Point3 first, Point3 second) => new(
        new Point3(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y), Math.Min(first.Z, second.Z)),
        new Point3(Math.Max(first.X, second.X), Math.Max(first.Y, second.Y), Math.Max(first.Z, second.Z)));
    public bool Equals(BoundingBox3 other) => Min.Equals(other.Min) && Max.Equals(other.Max);
    public override bool Equals(object? obj) => obj is BoundingBox3 other && Equals(other);
    public override int GetHashCode()
    {
        unchecked { return (Min.GetHashCode() * 397) ^ Max.GetHashCode(); }
    }
}
