using System.Numerics;
using Raylib_cs;

namespace ReadersWriters.Visualization;

public static class VisualReadersWriters
{
    // Existing synchronization primitives and counters
    private static readonly SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);
    private static readonly SemaphoreSlim Db = new SemaphoreSlim(1, 1);
    private static readonly SemaphoreSlim WriterQueue = new SemaphoreSlim(1, 1);
    private static int _rc = 0;
    private static volatile bool _running = true;
    private static readonly Random Random = new Random();

    // Visualization integration
    private const int NumReaders = 50;
    private const int NumWriters = 10;
    private static readonly List<Entity> Entities = new List<Entity>();

    class Entity
    {
        public int Id;
        public bool IsReader;
        public float Angle;
        public Vector3 Position;
        public Vector3 TargetPosition; // Add this property
        public bool IsAccessing;
        public Color Color;
    }

    public static void Simulation()
    {
        Raylib.InitWindow(800, 600, "Readers-Writers Visualization");
        Raylib.SetTargetFPS(60);

        // Create visualization entities
        CreateEntities();

        // Start simulation thread
        Thread simulationThread = new Thread(RunSimulation);
        simulationThread.Start();

        // Main visualization loop
        while (!Raylib.WindowShouldClose())
        {
            UpdateVisualization();
        }

        _running = false;
        simulationThread.Join();
        Raylib.CloseWindow();
    }

    private static void CreateEntities()
    {
        const float readerRadius = 200;
        const float writerRadius = 100; // Smaller radius for writers
        Vector3 center = new Vector3(0, 0, 0);

        // Arrange readers in a circle
        for (int i = 0; i < NumReaders; i++)
        {
            float angle = (float)(2 * Math.PI * i / NumReaders);
            lock (Entities)
            {
                Entities.Add(new Entity
                {
                    Id = i,
                    IsReader = true,
                    Angle = angle,
                    Position = center + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * readerRadius,
                    TargetPosition =
                        center + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) *
                        readerRadius, // Initialize TargetPosition
                    Color = Color.Red
                });
            }
        }

        // Arrange writers in a different circle
        for (int i = 0; i < NumWriters; i++)
        {
            float angle = (float)(2 * Math.PI * i / NumWriters);
            lock (Entities)
            {
                Entities.Add(new Entity
                {
                    Id = NumReaders + i,
                    IsReader = false,
                    Angle = angle,
                    Position = center + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * writerRadius,
                    TargetPosition =
                        center + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) *
                        writerRadius, // Initialize TargetPosition
                    Color = Color.Blue
                });
            }
        }
    }

    private static void UpdateVisualization()
    {
        Camera3D camera = new Camera3D
        {
            Position = new Vector3(0, 500, 500),
            Target = new Vector3(0, 0, 0),
            Up = new Vector3(0, 1, 0),
            FovY = 45.0f,
            Projection = CameraProjection.Perspective,
        };

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(135, 206, 235, 255)); // SkyBlue color

        Raylib.BeginMode3D(camera);

        // Draw green floor
        Raylib.DrawPlane(new Vector3(0, 0, 0), new Vector2(1000, 1000), Color.Green);

        // Draw database sphere
        Raylib.DrawSphere(new Vector3(0, 0, 0), 30, Color.DarkGray);

        // Draw sun representation (no actual lighting, just a visual)
        Vector3 lightPos = new Vector3(0, 500, 0);
        Raylib.DrawSphere(lightPos, 10, Color.Yellow);

        lock (Entities)
        {
            foreach (var entity in Entities)
            {
                // Smoothly move towards the target position
                entity.Position = Vector3.Lerp(entity.Position, entity.TargetPosition, 0.1f);

                // Draw entity as a sphere
                Raylib.DrawSphere(entity.Position, 10, entity.Color);

                // Draw simulated shadow on the ground
                Vector3 shadowPos = new Vector3(entity.Position.X, 0, entity.Position.Z);
                float shadowRadius = 10 * (1 - entity.Position.Y / 500); // Adjust shadow size based on height
                Raylib.DrawCircle3D(shadowPos, shadowRadius, new Vector3(1, 0, 0), 90, new Color(0, 0, 0, 128));
            }
        }

        Raylib.EndMode3D();
        Raylib.EndDrawing();
    }

    private static void RunSimulation()
    {
        var threads = new List<Thread>();

        // Start readers
        for (int i = 0; i < NumReaders; i++)
        {
            int id = i;
            threads.Add(new Thread(() => ReaderLogic(id)));
        }

        // Start writers
        for (int i = 0; i < NumWriters; i++)
        {
            int id = NumReaders + i;
            threads.Add(new Thread(() => WriterLogic(id)));
        }

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
    }

    private static void ReaderLogic(int id)
    {
        while (_running)
        {
            // Thinking phase
            Thread.Sleep(Random.Next(500, 1000)); // Increased sleep duration

            // Trying to access
            WriterQueue.Wait();
            Mutex.Wait();
            _rc++;
            if (_rc == 1) Db.Wait();
            Mutex.Release();
            WriterQueue.Release();

            // Accessing DB
            UpdateAccessState(id, true);
            Thread.Sleep(Random.Next(800, 1400)); // Increased sleep duration
            UpdateAccessState(id, false);

            // Releasing
            Mutex.Wait();
            _rc--;
            if (_rc == 0) Db.Release();
            Mutex.Release();
        }
    }

    private static void WriterLogic(int id)
    {
        while (_running)
        {
            // Thinking phase
            Thread.Sleep(Random.Next(600, 1500)); // Increased sleep duration

            // Trying to access
            WriterQueue.Wait();
            Db.Wait();

            // Accessing DB
            UpdateAccessState(id, true);
            Thread.Sleep(Random.Next(400, 800)); // Increased sleep duration
            UpdateAccessState(id, false);

            // Releasing
            Db.Release();
            WriterQueue.Release();
        }
    }

    private static void UpdateAccessState(int id, bool accessing)
    {
        lock (Entities)
        {
            var entity = Entities.Find(e => e.Id == id);
            if (entity != null)
            {
                entity.IsAccessing = accessing;
                if (accessing)
                {
                    // Set target position to the database
                    entity.TargetPosition = new Vector3(0, 0, 0);
                }
                else
                {
                    // Set target position to the original position
                    float angle = entity.Angle;
                    float radius = entity.IsReader ? 200 : 100;
                    entity.TargetPosition = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * radius;
                }
            }
        }
    }
}