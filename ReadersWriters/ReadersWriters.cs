using System;
using System.Threading;

namespace ReadersWriters;

using System;
using System.Threading;
using System.Collections.Generic;

static class ReadersWriters
{
    private static SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
    private static SemaphoreSlim db = new SemaphoreSlim(1, 1);
    private static int rc = 0;
    private static int readerCount = 0;
    private static int writerCount = 0;
    private static bool running = true;
    private static Random random = new Random();

    public static void Reader()
    {
        while (running)
        {
            mutex.Wait(); // get exclusive access to rc
            rc++;
            if (rc == 1)
                db.Wait(); // if this is the first reader ...
            mutex.Release(); // release exclusive access to rc

            ReadDatabase(); // access the data
            Interlocked.Increment(ref readerCount);

            mutex.Wait(); // get exclusive access to rc
            rc--;
            if (rc == 0)
                db.Release(); // if this is the last reader ...
            mutex.Release(); // release exclusive access to rc

            UseDataRead(); // noncritical region

            Thread.Sleep(random.Next(500, 2000)); // Simulate random pauses
        }
    }

    public static void Writer()
    {
        while (running)
        {
            ThinkUpData(); // noncritical region
            db.Wait(); // get exclusive access
            WriteDatabase(); // update the data
            Interlocked.Increment(ref writerCount);
            db.Release(); // release exclusive access

            Thread.Sleep(random.Next(1000, 3000)); // Simulate random pauses
        }
    }

    private static void ReadDatabase()
    {
        Console.WriteLine("Reading from database...");
        Thread.Sleep(random.Next(300, 800)); // Simulate database reading
    }

    private static void UseDataRead()
    {
        Console.WriteLine("Using read data...");
        Thread.Sleep(random.Next(300, 800));
    }

    private static void ThinkUpData()
    {
        Console.WriteLine("Thinking up data...");
        Thread.Sleep(random.Next(500, 1500));
    }

    private static void WriteDatabase()
    {
        Console.WriteLine("Writing to database...");
        Thread.Sleep(random.Next(800, 1500)); // Simulate writing
    }

    public static void Example()
    {
        List<Thread> threads = new List<Thread>();
        
        for (int i = 0; i < 3; i++)
        {
            Thread readerThread = new Thread(Reader);
            threads.Add(readerThread);
            readerThread.Start();
            Thread.Sleep(random.Next(1000, 3000)); // Stagger reader starts
        }

        for (int i = 0; i < 2; i++)
        {
            Thread writerThread = new Thread(Writer);
            threads.Add(writerThread);
            writerThread.Start();
            Thread.Sleep(random.Next(3000, 5000)); // Stagger writer starts
        }
        
        Thread.Sleep(15000); // Run for 15 seconds
        running = false;

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine($"Readers executed: {readerCount}");
        Console.WriteLine($"Writers executed: {writerCount}");
    }
}
