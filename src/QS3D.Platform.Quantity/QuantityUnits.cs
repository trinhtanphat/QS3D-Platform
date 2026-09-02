using System.Globalization;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.Quantity;

public enum QuantityUnit
{
    Each = 0,
    Millimeter,
    Centimeter,
    Meter,
    SquareMillimeter,
    SquareCentimeter,
    SquareMeter,
    CubicMillimeter,
    CubicCentimeter,
    CubicMeter,
    Gram,
    Kilogram,
    Tonne
}

public static class QuantityUnits
{
    public static QuantityDimension DimensionOf(QuantityUnit unit)
    {
        switch (unit)
        {
            case QuantityUnit.Each: return QuantityDimension.Count;
            case QuantityUnit.Millimeter:
            case QuantityUnit.Centimeter:
            case QuantityUnit.Meter: return QuantityDimension.Length;
            case QuantityUnit.SquareMillimeter:
            case QuantityUnit.SquareCentimeter:
            case QuantityUnit.SquareMeter: return QuantityDimension.Area;
            case QuantityUnit.CubicMillimeter:
            case QuantityUnit.CubicCentimeter:
            case QuantityUnit.CubicMeter: return QuantityDimension.Volume;
            case QuantityUnit.Gram:
            case QuantityUnit.Kilogram:
            case QuantityUnit.Tonne: return QuantityDimension.Mass;
            default: throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported quantity unit.");
        }
    }

    public static string Symbol(QuantityUnit unit)
    {
        switch (unit)
        {
            case QuantityUnit.Each: return "ea";
            case QuantityUnit.Millimeter: return "mm";
            case QuantityUnit.Centimeter: return "cm";
            case QuantityUnit.Meter: return "m";
            case QuantityUnit.SquareMillimeter: return "mm2";
            case QuantityUnit.SquareCentimeter: return "cm2";
            case QuantityUnit.SquareMeter: return "m2";
            case QuantityUnit.CubicMillimeter: return "mm3";
            case QuantityUnit.CubicCentimeter: return "cm3";
            case QuantityUnit.CubicMeter: return "m3";
            case QuantityUnit.Gram: return "g";
            case QuantityUnit.Kilogram: return "kg";
            case QuantityUnit.Tonne: return "t";
            default: throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported quantity unit.");
        }
    }

    public static double ToCanonical(double value, QuantityUnit unit)
    {
        value = Numeric.RequireNonNegativeFinite(value, nameof(value));
        var result = value * ScaleToCanonical(unit);
        if (!Numeric.IsFinite(result) || (value != 0d && result == 0d))
            throw new OverflowException(value.ToString("R", CultureInfo.InvariantCulture) + " " + Symbol(unit) + " cannot be represented in canonical units.");
        return result == 0d ? 0d : result;
    }

    public static double FromCanonical(double canonicalValue, QuantityUnit unit)
    {
        canonicalValue = Numeric.RequireNonNegativeFinite(canonicalValue, nameof(canonicalValue));
        var result = canonicalValue / ScaleToCanonical(unit);
        if (!Numeric.IsFinite(result) || (canonicalValue != 0d && result == 0d))
            throw new OverflowException("Canonical value " + canonicalValue.ToString("R", CultureInfo.InvariantCulture) + " cannot be represented as " + Symbol(unit) + ".");
        return result == 0d ? 0d : result;
    }

    public static QuantityValue ToQuantityValue(double value, QuantityUnit unit)
        => new QuantityValue(DimensionOf(unit), ToCanonical(value, unit));

    private static double ScaleToCanonical(QuantityUnit unit)
    {
        switch (unit)
        {
            case QuantityUnit.Each: return 1d;
            case QuantityUnit.Millimeter: return 1e-3d;
            case QuantityUnit.Centimeter: return 1e-2d;
            case QuantityUnit.Meter: return 1d;
            case QuantityUnit.SquareMillimeter: return 1e-6d;
            case QuantityUnit.SquareCentimeter: return 1e-4d;
            case QuantityUnit.SquareMeter: return 1d;
            case QuantityUnit.CubicMillimeter: return 1e-9d;
            case QuantityUnit.CubicCentimeter: return 1e-6d;
            case QuantityUnit.CubicMeter: return 1d;
            case QuantityUnit.Gram: return 1e-3d;
            case QuantityUnit.Kilogram: return 1d;
            case QuantityUnit.Tonne: return 1e3d;
            default: throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported quantity unit.");
        }
    }
}
