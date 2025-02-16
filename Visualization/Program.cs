using Raylib_cs;

namespace Visualization;

public static class Program
{
    public static void Main()
    {
        var state = new SimulationState();
        var scheduler = new Scheduler(state);
        var renderer = new Renderer(state);

        renderer.Initialize();
        scheduler.Start(50, 10);

        while (!Raylib.WindowShouldClose())
        {
            renderer.Update();
        }

        scheduler.Stop();
        Raylib.CloseWindow();
    }
}