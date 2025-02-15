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

            Thread.Sleep(random.Next(50, 200)); // Simulate random pauses
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

            Thread.Sleep(random.Next(100, 300)); // Simulate random pauses
        }
    }

    private static void ReadDatabase()
    {
        Console.WriteLine("Reading from database...");
        Thread.Sleep(random.Next(30, 80)); // Simulate database reading
    }

    private static void UseDataRead()
    {
        Console.WriteLine("Using read data...");
        Thread.Sleep(random.Next(30, 80));
    }

    private static void ThinkUpData()
    {
        Console.WriteLine("Thinking up data...");
        Thread.Sleep(random.Next(50, 150));
    }

    private static void WriteDatabase()
    {
        Console.WriteLine("Writing to database...");
        Thread.Sleep(random.Next(80, 150)); // Simulate writing
    }

    public static void Example()
    {
        const int numReaders = 20;
        const int numWriters = 2;
        const int totalTime = 3000;

        List<Thread> threads = new List<Thread>();

        for (int i = 0; i < numReaders; i++)
        {
            Thread readerThread = new Thread(Reader);
            threads.Add(readerThread);
            readerThread.Start();
            
            // Stagger reader starts
            Thread.Sleep(random.Next(totalTime / (3 * numReaders), totalTime / numReaders));
        }

        for (int i = 0; i < numWriters; i++)
        {
            Thread writerThread = new Thread(Writer);
            threads.Add(writerThread);
            writerThread.Start();
            Thread.Sleep(random.Next(totalTime / (3 * numWriters), totalTime / numWriters)); // Stagger writer starts
        }

        Thread.Sleep(totalTime);
        running = false;

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine($"Readers executed: {readerCount}");
        Console.WriteLine($"Writers executed: {writerCount}");
        Console.WriteLine($"Ratio: {readerCount / writerCount}");
        Console.WriteLine($"Balanced Ratio: {numReaders / numWriters}");
    }
}