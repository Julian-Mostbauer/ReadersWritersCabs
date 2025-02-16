using System.Numerics;
using Raylib_cs;

namespace Visualization;

public class Scheduler(SimulationState state)
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly SemaphoreSlim _db = new(1, 1);
    private readonly SemaphoreSlim _writerQueue = new(1, 1);
    private readonly Random _random = new();
    private bool _running = true;
    private static readonly Vector3 DatabasePosition = new(0, 10, 0);

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
                10,
                (float)(_random.NextDouble() * 1000 - 500)
            ),
            TargetPosition = DatabasePosition,
            CurrentColor = type == EntityType.Reader ? Color.Red : Color.Blue
        };

        state.Entities.Add(entity);
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
        UpdateEntityState(entity, Color.White, "Thinking");
        Thread.Sleep(_random.Next(500, 1000));

        entity.Moving = true;
        MoveEntity(entity, DatabasePosition);
        entity.Moving = false;

        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait();
        _mutex.Wait();
        state.ReaderCount++;
        if (state.ReaderCount == 1) _db.Wait();
        _mutex.Release();
        _writerQueue.Release();

        UpdateEntityState(entity, Color.Purple, "Reading");
        Thread.Sleep(_random.Next(800, 1400));

        _mutex.Wait();
        state.ReaderCount--;
        if (state.ReaderCount == 0) _db.Release();
        _mutex.Release();

        entity.Moving = true;
        MoveEntity(entity, GetRandomPosition());
        entity.Moving = false;
    }

    private void RunWriterLogic(Entity entity)
    {
        UpdateEntityState(entity, Color.White, "Thinking");
        Thread.Sleep(_random.Next(600, 1500));

        entity.Moving = true;
        MoveEntity(entity, DatabasePosition);
        entity.Moving = false;

        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait();
        _db.Wait();

        UpdateEntityState(entity, Color.Purple, "Writing");
        Thread.Sleep(_random.Next(400, 800));

        _db.Release();
        _writerQueue.Release();

        entity.Moving = true;
        MoveEntity(entity, GetRandomPosition());
        entity.Moving = false;
    }

    private void MoveEntity(Entity entity, Vector3 target)
    {
        entity.TargetPosition = target;
        while (Vector3.Distance(entity.Position, target) > 1f)
        {
            entity.Position = Vector3.Lerp(entity.Position, target, 0.1f);
            Thread.Sleep(16);
        }
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            (float)(_random.NextDouble() * 1000 - 500 + 50),
            10,
            (float)(_random.NextDouble() * 1000 - 500 + 50)
        );
    }

    private void UpdateEntityState(Entity entity, Color color, string status)
    {
        entity.CurrentColor = color;
        entity.Status = status;
    }

    public void Stop() => _running = false;
}
