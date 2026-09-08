using System.Runtime.CompilerServices;

static class BoqKnownCountNoOverreadSmokeRegistration
{
    [ModuleInitializer]
    public static void Initialize()
    {
        BoqKnownCountNoOverreadModuleSmoke.Run();
    }
}
