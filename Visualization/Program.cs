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
        scheduler.Start(300, 10);

        while (!Raylib.WindowShouldClose())
        {
            renderer.Update();
            
            if (Raylib.IsKeyPressed(KeyboardKey.RightControl))
                scheduler.SpeedUp();

            if (Raylib.IsKeyPressed(KeyboardKey.LeftControl))
                scheduler.SlowDown();
            
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
                scheduler.ResetSpeed();
        }

        scheduler.Stop();
        Raylib.CloseWindow();
    }
}