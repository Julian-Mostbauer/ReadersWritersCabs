using System;
using System.Threading;
using System.Collections.Generic;

namespace ReadersWriters;

public static class ReadersWriters
{
    private static SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
    private static SemaphoreSlim db = new SemaphoreSlim(1, 1);
    private static SemaphoreSlim writerQueue = new SemaphoreSlim(1, 1);

    private static int rc = 0;
    private static int readerCount = 0;
    private static int writerCount = 0;
    private static bool running = true;
    private static Random random = new Random();

    private const int writerDelay = 100;
    private const int readerDelay = 50;

    private const int writerPause = 300;
    private const int readerPause = 200;

    public static void Reader()
    {
        while (running)
        {
            writerQueue.Wait(); // Prevent readers if writers are waiting
            mutex.Wait(); // get exclusive access to rc
            rc++;
            if (rc == 1)
                db.Wait(); // if this is the first reader ...
            mutex.Release(); // release exclusive access to rc
            writerQueue.Release(); // Allow other readers or writers

            ReadDatabase(); // access the data
            Interlocked.Increment(ref readerCount);

            mutex.Wait(); // get exclusive access to rc
            rc--;
            if (rc == 0)
                db.Release(); // if this is the last reader ...
            mutex.Release(); // release exclusive access to rc

            UseDataRead(); // noncritical region

            Thread.Sleep(random.Next((int)(readerPause * 0.8), (int)(readerPause * 1.2))); // Simulate random pauses
        }
    }

    public static void Writer()
    {
        while (running)
        {
            ThinkUpData(); // noncritical region
            writerQueue.Wait(); // Ensure writer gets priority
            db.Wait(); // get exclusive access
            WriteDatabase(); // update the data
            Interlocked.Increment(ref writerCount);
            db.Release(); // release exclusive access
            writerQueue.Release(); // Allow others

            Thread.Sleep(random.Next((int)(writerPause * 0.8), (int)(writerPause * 1.2))); // Simulate random pauses
        }
    }

    private static void ReadDatabase()
    {
        Console.WriteLine("Reading from database...");
        Thread.Sleep(random.Next((int)(readerDelay * 0.8), (int)(readerDelay * 1.2))); // Simulate database reading
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
        Thread.Sleep(random.Next((int)(writerDelay * 0.8), (int)(writerDelay * 1.2))); // Simulate writing
    }

    public static void Example()
    {
        const int numReaders = 20;
        const int numWriters = 2;
        const int totalTime = 3000;

        var threads = new List<Thread>();

        for (int i = 0; i < numReaders; i++)
        {
            Thread readerThread = new Thread(Reader);
            threads.Add(readerThread);
            readerThread.Start();
            Thread.Sleep(random.Next(totalTime / (3 * numReaders), totalTime / numReaders)); // Stagger reader starts
        }

        for (int i = 0; i < numWriters; i++)
        {
            Thread writerThread = new Thread(Writer);
            threads.Add(writerThread);
            writerThread.Start();
            Thread.Sleep(random.Next(totalTime / (3 * numWriters), totalTime / numWriters)); // Stagger writer starts
        }

        Thread.Sleep(totalTime); // Run for totalTime duration
        running = false;

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine($"Readers executed: {readerCount}");
        Console.WriteLine($"Writers executed: {writerCount}");
        Console.WriteLine($"Ratio: {(writerCount == 0 ? 0 : (double)readerCount / writerCount)}");
        Console.WriteLine($"Balanced Ratio: {(writerDelay * numReaders) / (readerDelay * numWriters)}");
    }
}