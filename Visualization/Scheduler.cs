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
    private readonly SpeedControl _speedControl = new();

    private int RandomDeviation(int average, float percent = 0.5f) =>
        (int)(average * (1 + (float)(_random.NextDouble() * 2 - 1) * percent));

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
            Position = GetRandomPosition(),
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
        Thread.Sleep(RandomDeviation(_speedControl.AverageReaderThinkTime));

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
        Thread.Sleep(RandomDeviation(_speedControl.AverageReaderReadTime));

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
        Thread.Sleep(RandomDeviation(_speedControl.AverageWriterThinkTime));

        UpdateEntityState(entity, Color.Yellow, "Waiting");
        _writerQueue.Wait();
        _db.Wait();

        // Move to DB AFTER acquiring access
        entity.Moving = true;
        MoveEntity(entity, DatabasePosition + GetRandomPosition() / 20);
        entity.Moving = false;

        UpdateEntityState(entity, Color.Purple, "Writing");
        Thread.Sleep(RandomDeviation(_speedControl.AverageWriterWriteTime));

        UpdateEntityState(entity, Color.White, "Thinking"); // signal end of access before moving

        // Move away BEFORE releasing semaphores
        entity.Moving = true;
        MoveEntity(entity, GetRandomPosition());
        entity.Moving = false;

        _db.Release();
        _writerQueue.Release();
    }

    private static void MoveEntity(Entity entity, Vector3 target)
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
        } while (Vector3.Distance(pos with { Y = 0 }, Vector3.Zero) < 70);

        return pos;
    }

    private static void UpdateEntityState(Entity entity, Color color, string status)
    {
        entity.Status = status;
        entity.SmoothUpdateColor(color);
    }

    public void Stop() => _running = false;

    public void SpeedUp() => _speedControl.SpeedUp();
    public void SlowDown() => _speedControl.SlowDown();
    public void ResetSpeed() => _speedControl.ResetSpeed();

    private class SpeedControl
    {
        private const float DefaultAverageReaderThinkTime = 750f;
        private const float DefaultAverageReaderReadTime = 700f;
        private const float DefaultAverageWriterThinkTime = 1200f;
        private const float DefaultAverageWriterWriteTime = 600f;

        private float _averageReaderThinkTime = DefaultAverageReaderThinkTime;
        private float _averageReaderReadTime = DefaultAverageReaderReadTime;
        private float _averageWriterThinkTime = DefaultAverageWriterThinkTime;
        private float _averageWriterWriteTime = DefaultAverageWriterWriteTime;

        public int AverageReaderThinkTime => (int)_averageReaderThinkTime;
        public int AverageReaderReadTime => (int)_averageReaderReadTime;
        public int AverageWriterThinkTime => (int)_averageWriterThinkTime;
        public int AverageWriterWriteTime => (int)_averageWriterWriteTime;

        public void SpeedUp()
        {
            _averageReaderReadTime /= 1.2f;
            _averageReaderThinkTime /= 1.2f;
            _averageWriterWriteTime /= 1.2f;
            _averageWriterThinkTime /= 1.2f;
        }

        public void SlowDown()
        {
            _averageReaderReadTime *= 1.2f;
            _averageReaderThinkTime *= 1.2f;
            _averageWriterWriteTime *= 1.2f;
            _averageWriterThinkTime *= 1.2f;
        }

        public void ResetSpeed()
        {
            _averageReaderReadTime = DefaultAverageReaderReadTime;
            _averageReaderThinkTime = DefaultAverageReaderThinkTime;
            _averageWriterWriteTime = DefaultAverageWriterWriteTime;
            _averageWriterThinkTime = DefaultAverageWriterThinkTime;
        }
    }
}