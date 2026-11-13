using MemoryLifecycle.Gc;

internal static class Program
{
    private static void Main()
    {
    //    PressureScenarios.RunAll();
        MemoryLifecycle.Gc.GcModeScenarios.RunParallelAllocation();
    }
}