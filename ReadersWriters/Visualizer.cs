namespace ReadersWriters;

using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

public static class Visualizer
{
    static SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
    static SemaphoreSlim db = new SemaphoreSlim(1, 1);
    static SemaphoreSlim writerQueue = new SemaphoreSlim(1, 1);
    static int rc = 0;
    static bool running = true;
    static Random random = new Random();
    
    static List<Reader> readers = new List<Reader>();
    static List<Writer> writers = new List<Writer>();
    static string databaseState = "Idle";

    static void ReaderTask()
    {
        while (running)
        {
            writerQueue.Wait();
            mutex.Wait();
            rc++;
            if (rc == 1)
                db.Wait();
            mutex.Release();
            writerQueue.Release();

            databaseState = "Reading...";
            Thread.Sleep(random.Next(1000, 3000));
            databaseState = "Idle";
            
            mutex.Wait();
            rc--;
            if (rc == 0)
                db.Release();
            mutex.Release();
            
            Thread.Sleep(random.Next(500, 2000));
        }
    }

    static void WriterTask()
    {
        while (running)
        {
            writerQueue.Wait();
            db.Wait();

            databaseState = "Writing...";
            Thread.Sleep(random.Next(2000, 4000));
            databaseState = "Idle";
            
            db.Release();
            writerQueue.Release();
            
            Thread.Sleep(random.Next(3000, 5000));
        }
    }
    
    public static void Simulation01()
    {
        Raylib.InitWindow(800, 600, "Readers-Writers Visualization");
        Raylib.SetTargetFPS(60);
        
        for (int i = 0; i < 3; i++)
        {
            Reader reader = new Reader(new Vector2(100 + i * 100, 200));
            readers.Add(reader);
            Thread thread = new Thread(ReaderTask);
            thread.Start();
        }
        
        for (int i = 0; i < 2; i++)
        {
            Writer writer = new Writer(new Vector2(200 + i * 150, 400));
            writers.Add(writer);
            Thread thread = new Thread(WriterTask);
            thread.Start();
        }
        
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            
            Raylib.DrawText(databaseState, 350, 50, 20, Color.Black);
            
            foreach (var reader in readers)
                reader.Draw();
            foreach (var writer in writers)
                writer.Draw();
            
            Raylib.EndDrawing();
        }
        
        running = false;
        Raylib.CloseWindow();
    }
}

class Reader
{
    public Vector2 Position;
    public Reader(Vector2 position) { Position = position; }
    public void Draw() { Raylib.DrawCircleV(Position, 20, Color.Blue); }
}

class Writer
{
    public Vector2 Position;
    public Writer(Vector2 position) { Position = position; }
    public void Draw() { Raylib.DrawRectangleV(Position, new Vector2(40, 40), Color.Red); }
}
