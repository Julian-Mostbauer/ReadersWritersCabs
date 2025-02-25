using System.Collections.Concurrent;
using System.Numerics;
using Raylib_cs;

namespace Visualization;

public enum EntityType
{
    Reader,
    Writer
}

public class SimulationState
{
    public ConcurrentBag<Entity> Entities { get; } = new();
    public int ReaderCount { get; set; }
    public bool DatabaseAvailable { get; set; } = true;
}

public class Entity
{
    public EntityType Type { get; init; }
    public Vector3 Position { get; set; }
    public Vector3 TargetPosition { get; set; }
    public string Status { get; set; } = "Thinking";
    public bool Moving { get; set; } = false;
    public Color CurrentColor { get; set; }

    public void SmoothUpdateColor(Color targetColor)
    {
        new Thread(() =>
        {
            float t = 0;
            while (t < 1)
            {
                CurrentColor = new Color(
                    (byte)Helper.Lerp(CurrentColor.R, targetColor.R, t),
                    (byte)Helper.Lerp(CurrentColor.G, targetColor.G, t),
                    (byte)Helper.Lerp(CurrentColor.B, targetColor.B, t),
                    (byte)Helper.Lerp(CurrentColor.A, targetColor.A, t)
                );
                t += 0.01f;
                Thread.Sleep(16); // Approximate 60 FPS
            }
        }).Start();
    }
}

internal static class Helper
{
    public static bool SameColor(this Color a, Color b, int tolerance = 1)
    {
        return Math.Abs(a.R - b.R) <= tolerance &&
               Math.Abs(a.G - b.G) <= tolerance &&
               Math.Abs(a.B - b.B) <= tolerance &&
               Math.Abs(a.A - b.A) <= tolerance;
    }

    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}