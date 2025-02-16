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

        // Move to the database *after* acquiring access
        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait(); // Ensures writers get priority
        _mutex.Wait();
        state.ReaderCount++;
        if (state.ReaderCount == 1)
            _db.Wait(); // Block writers if first reader
        _mutex.Release();
        _writerQueue.Release(); // Release early to allow other readers

        // Now move to the database position
        entity.Moving = true;
        MoveEntity(entity, DatabasePosition + GetRandomPosition() / 20);
        entity.Moving = false;

        UpdateEntityState(entity, Color.Purple, "Reading");
        Thread.Sleep(_random.Next(400, 1000));

        // Exit the database
        _mutex.Wait();
        state.ReaderCount--;
        if (state.ReaderCount == 0)
            _db.Release(); // Unblock writers if last reader
        _mutex.Release();

        UpdateEntityState(entity, Color.White, "Thinking"); // signal end of access before moving
        // Move away after releasing access
        entity.Moving = true;
        MoveEntity(entity, GetRandomPosition());
        entity.Moving = false;
    }

    private void RunWriterLogic(Entity entity)
    {
        UpdateEntityState(entity, Color.White, "Thinking");
        Thread.Sleep(_random.Next(600, 1500));

        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait();
        _db.Wait();

        // Move to DB AFTER acquiring access
        entity.Moving = true;
        MoveEntity(entity, DatabasePosition + GetRandomPosition() / 20);
        entity.Moving = false;

        UpdateEntityState(entity, Color.Purple, "Writing");
        Thread.Sleep(_random.Next(400, 800));

        UpdateEntityState(entity, Color.White, "Thinking"); // signal end of access before moving

        // Move away BEFORE releasing semaphores
        entity.Moving = true;
        MoveEntity(entity, GetRandomPosition());
        entity.Moving = false;

        _db.Release();
        _writerQueue.Release();
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
        Vector3 pos;
        do
        {
            pos = new Vector3(
                (float)(_random.NextDouble() * 1000 - 500),
                5,
                (float)(_random.NextDouble() * 1000 - 500)
            );
        } while (Vector3.Distance(pos with { Y = 0 }, Vector3.Zero) < 150);

        return pos;
    }

    private void UpdateEntityState(Entity entity, Color color, string status)
    {
        entity.CurrentColor = color;
        entity.Status = status;
    }

    public void Stop() => _running = false;
}