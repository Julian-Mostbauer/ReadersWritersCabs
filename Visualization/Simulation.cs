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
    public Color CurrentColor { get; set; }
    public string Status { get; set; } = "Thinking";
    public bool Moving { get; set; } = false;
}