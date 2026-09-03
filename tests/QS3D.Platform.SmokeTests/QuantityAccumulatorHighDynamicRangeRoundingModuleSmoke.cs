using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityAccumulatorHighDynamicRangeRoundingModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        VerifyOneUlpHighDynamicRangeRegression();
        VerifyClassicRecoverableContribution();
        VerifyPermutationDeterminism();
        VerifySubnormalAndNormalBoundary();
        VerifyRoundToNearestEven();
        VerifyTrueOverflowRejected();
        Console.WriteLine("PASS quantity accumulator high-dynamic-range rounding");
    }

    private static void VerifyOneUlpHighDynamicRangeRegression()
    {
        var actual = Summarize(1e-60d, 1.5e160d, 1.75e160d);
        const double expected = 3.2500000000000004e160d;
        if (actual != expected)
            throw new InvalidOperationException($"Quantity accumulator rounded high-dynamic-range total to {actual:R}; expected {expected:R}.");
    }

    private static void VerifyClassicRecoverableContribution()
    {
        var actual = Summarize(1e16d, 1d, 1d);
        const double expected = 10000000000000002d;
        if (actual != expected)
            throw new InvalidOperationException($"Quantity accumulator lost recoverable small contributions: {actual:R} != {expected:R}.");
    }

    private static void VerifyPermutationDeterminism()
    {
        const double expected = 3.2500000000000004e160d;
        var permutations = new[]
        {
            new[] { 1e-60d, 1.5e160d, 1.75e160d },
            new[] { 1.75e160d, 1e-60d, 1.5e160d },
            new[] { 1.5e160d, 1.75e160d, 1e-60d }
        };

        foreach (var permutation in permutations)
        {
            var actual = Summarize(permutation);
            if (actual != expected)
                throw new InvalidOperationException($"Quantity accumulator is not deterministic/correct across input permutations: {actual:R} != {expected:R}.");
        }
    }

    private static void VerifySubnormalAndNormalBoundary()
    {
        var twiceEpsilon = Summarize(double.Epsilon, double.Epsilon);
        var expectedTwiceEpsilon = BitConverter.Int64BitsToDouble(2L);
        if (twiceEpsilon != expectedTwiceEpsilon)
            throw new InvalidOperationException("Quantity accumulator corrupted an exactly representable subnormal sum.");

        var maximumSubnormal = BitConverter.Int64BitsToDouble(0x000fffffffffffffL);
        var minimumNormal = BitConverter.Int64BitsToDouble(0x0010000000000000L);
        var crossedBoundary = Summarize(maximumSubnormal, double.Epsilon);
        if (crossedBoundary != minimumNormal)
            throw new InvalidOperationException("Quantity accumulator corrupted the maximum-subnormal to minimum-normal boundary.");
    }

    private static void VerifyRoundToNearestEven()
    {
        var halfUlpAtOne = Math.Pow(2d, -53d);
        var tieDown = Summarize(1d, halfUlpAtOne);
        if (tieDown != 1d)
            throw new InvalidOperationException($"Quantity accumulator did not round the even lower significand on a half-ULP tie: {tieDown:R}.");

        var tieUp = Summarize(1d, halfUlpAtOne, halfUlpAtOne, halfUlpAtOne);
        var expectedTieUp = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1d) + 2L);
        if (tieUp != expectedTieUp)
            throw new InvalidOperationException($"Quantity accumulator did not round to the even upper significand on a half-ULP tie: {tieUp:R} != {expectedTieUp:R}.");
    }

    private static void VerifyTrueOverflowRejected()
    {
        try
        {
            _ = Summarize(double.MaxValue, double.MaxValue);
        }
        catch (OverflowException)
        {
            return;
        }

        throw new InvalidOperationException("Quantity accumulator accepted a true finite-sum overflow.");
    }

    private static double Summarize(params double[] values)
    {
        var facts = values.Select(value =>
            new QuantityFact(
                ElementId.New(),
                "TEST.LENGTH",
                new QuantityValue(QuantityDimension.Length, value)))
            .ToArray();

        var summaries = QuantityAccumulator.Summarize(facts);
        if (summaries.Count != 1
            || summaries[0].FactCount != values.Length
            || summaries[0].ElementCount != values.Length)
            throw new InvalidOperationException("Quantity accumulator corrupted fact/element counts while summing.");
        return summaries[0].Quantity.Value;
    }
}
