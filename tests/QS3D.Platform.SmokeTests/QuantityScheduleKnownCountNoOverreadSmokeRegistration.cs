using System.Runtime.CompilerServices;

static class QuantityScheduleKnownCountNoOverreadSmokeRegistration
{
    [ModuleInitializer]
    public static void Initialize()
    {
        QuantityScheduleKnownCountNoOverreadModuleSmoke.Run();
    }
}
