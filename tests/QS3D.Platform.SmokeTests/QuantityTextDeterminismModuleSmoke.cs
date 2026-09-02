using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityTextDeterminismModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var cultures = new[]
            {
                CultureInfo.GetCultureInfo("en-US"),
                CultureInfo.GetCultureInfo("de-DE"),
                CultureInfo.GetCultureInfo("vi-VN")
            };

            foreach (var culture in cultures)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var moneyText = new Money(1234.5m, "usd").ToString();
                if (!StringComparer.Ordinal.Equals(moneyText, "1234.5 USD"))
                    throw new InvalidOperationException($"Money rendering is culture-dependent under {culture.Name}: '{moneyText}'.");

                var quantityText = new QuantityValue(QuantityDimension.Area, 1234.5d).ToString();
                if (!StringComparer.Ordinal.Equals(quantityText, "1234.5 m2"))
                    throw new InvalidOperationException($"QuantityValue rendering is culture-dependent under {culture.Name}: '{quantityText}'.");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        Console.WriteLine("PASS quantity/commercial text culture determinism");
    }
}
