using System.Numerics;
using Raylib_cs;

namespace Visualization;

public class Scheduler
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly SemaphoreSlim _db = new(1, 1);
    private readonly SemaphoreSlim _writerQueue = new(1, 1);
    private readonly SimulationState _state;
    private readonly Random _random = new();
    private bool _running = true;

    public Scheduler(SimulationState state) => _state = state;

    public void Start(int readers, int writers)
    {
        for (var i = 0; i < readers; i++)
            CreateEntity(EntityType.Reader);
        
        for (var i = 0; i < writers; i++)
            CreateEntity(EntityType.Writer);
    }

    private void CreateEntity(EntityType type)
    {
        var entity = new Entity
        {
            Type = type,
            Position = new Vector3(
                (float)(_random.NextDouble() * 1000 - 500),
                0,
                (float)(_random.NextDouble() * 1000 - 500)
            ),
            CurrentColor = type == EntityType.Reader ? Color.Red : Color.Blue
        };

        _state.Entities.Add(entity);
        new Thread(() => RunEntityLifecycle(entity)).Start();
    }

    private void RunEntityLifecycle(Entity entity)
    {
        while (_running)
        {
            if (entity.Type == EntityType.Reader)
                RunReaderLogic(entity);
            else
                RunWriterLogic(entity);
        }
    }

    private void RunReaderLogic(Entity entity)
    {
        // Thinking phase
        UpdateEntityState(entity, Color.White, "Thinking");
        Thread.Sleep(_random.Next(500, 1000));

        // Request access
        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait();
        _mutex.Wait();
        _state.ReaderCount++;
        if (_state.ReaderCount == 1) _db.Wait();
        _mutex.Release();
        _writerQueue.Release();

        // Access database
        UpdateEntityState(entity, Color.Purple, "Reading");
        Thread.Sleep(_random.Next(800, 1400));

        // Release access
        _mutex.Wait();
        _state.ReaderCount--;
        if (_state.ReaderCount == 0) _db.Release();
        _mutex.Release();
    }

    private void RunWriterLogic(Entity entity)
    {
        // Thinking phase
        UpdateEntityState(entity, Color.White, "Thinking");
        Thread.Sleep(_random.Next(600, 1500));

        // Request access
        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait();
        _db.Wait();

        // Access database
        UpdateEntityState(entity, Color.Purple, "Writing");
        Thread.Sleep(_random.Next(400, 800));

        // Release access
        _db.Release();
        _writerQueue.Release();
    }

    private void UpdateEntityState(Entity entity, Color color, string status)
    {
        entity.CurrentColor = color;
        entity.Status = status;
    }

    public void Stop() => _running = false;
}