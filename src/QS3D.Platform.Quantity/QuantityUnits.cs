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
        var result = ScaleByDecimalPower(value, ScalePowerToCanonical(unit));
        if (!Numeric.IsFinite(result) || (value != 0d && result == 0d))
            throw new OverflowException(value.ToString("R", CultureInfo.InvariantCulture) + " " + Symbol(unit) + " cannot be represented in canonical units.");
        return result == 0d ? 0d : result;
    }

    public static double FromCanonical(double canonicalValue, QuantityUnit unit)
    {
        canonicalValue = Numeric.RequireNonNegativeFinite(canonicalValue, nameof(canonicalValue));
        var result = ScaleByDecimalPower(canonicalValue, -ScalePowerToCanonical(unit));
        if (!Numeric.IsFinite(result) || (canonicalValue != 0d && result == 0d))
            throw new OverflowException("Canonical value " + canonicalValue.ToString("R", CultureInfo.InvariantCulture) + " cannot be represented as " + Symbol(unit) + ".");
        return result == 0d ? 0d : result;
    }

    public static QuantityValue ToQuantityValue(double value, QuantityUnit unit)
        => new QuantityValue(DimensionOf(unit), ToCanonical(value, unit));

    private static int ScalePowerToCanonical(QuantityUnit unit)
    {
        switch (unit)
        {
            case QuantityUnit.Each: return 0;
            case QuantityUnit.Millimeter: return -3;
            case QuantityUnit.Centimeter: return -2;
            case QuantityUnit.Meter: return 0;
            case QuantityUnit.SquareMillimeter: return -6;
            case QuantityUnit.SquareCentimeter: return -4;
            case QuantityUnit.SquareMeter: return 0;
            case QuantityUnit.CubicMillimeter: return -9;
            case QuantityUnit.CubicCentimeter: return -6;
            case QuantityUnit.CubicMeter: return 0;
            case QuantityUnit.Gram: return -3;
            case QuantityUnit.Kilogram: return 0;
            case QuantityUnit.Tonne: return 3;
            default: throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported quantity unit.");
        }
    }

    private static double ScaleByDecimalPower(double value, int power)
    {
        if (power == 0) return value;
        var factor = ExactPowerOfTen(Math.Abs(power));
        return power < 0 ? value / factor : value * factor;
    }

    private static double ExactPowerOfTen(int power)
    {
        switch (power)
        {
            case 2: return 100d;
            case 3: return 1_000d;
            case 4: return 10_000d;
            case 6: return 1_000_000d;
            case 9: return 1_000_000_000d;
            default: throw new ArgumentOutOfRangeException(nameof(power), power, "Unsupported quantity decimal scale power.");
        }
    }
}