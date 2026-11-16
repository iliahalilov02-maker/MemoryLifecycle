using MemoryLifecycle.Demos;
using MemoryLifecycle.Gc;

internal static class Program
{
    private static void Main()
    {
        //    PressureScenarios.RunAll();
        //MemoryLifecycle.Gc.GcModeScenarios.RunParallelAllocation();

        //ResourceDemos.RunFinalizerDemo();
        //ResourceDemos.RunUsingDemo();
        //ResourceDemos.RunForgottenDisposeDemo();
        //ResourceDemos.RunLogFileDemo("log.txt");
        DisposeDemos.RunSyncAndAsyncDemoAsync();
        DisposeDemos.RunFileBufferDemoAsync();

    }
}