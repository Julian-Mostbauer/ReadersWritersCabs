namespace ReadersWriters;

static class ReadersWriters
{
    private static SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
    private static SemaphoreSlim db = new SemaphoreSlim(1, 1);
    private static int rc = 0;

    public static void Reader()
    {
        while (true)
        {
            mutex.Wait(); // get exclusive access to rc
            rc++;
            if (rc == 1)
                db.Wait(); // if this is the first reader ...
            mutex.Release(); // release exclusive access to rc

            ReadDatabase(); // access the data

            mutex.Wait(); // get exclusive access to rc
            rc--;
            if (rc == 0)
                db.Release(); // if this is the last reader ...
            mutex.Release(); // release exclusive access to rc

            UseDataRead(); // noncritical region
        }
    }

    public static void Writer()
    {
        while (true)
        {
            ThinkUpData(); // noncritical region
            db.Wait(); // get exclusive access
            WriteDatabase(); // update the data
            db.Release(); // release exclusive access
        }
    }

    private static void ReadDatabase()
    {
        Console.WriteLine("Reading from database...");
        Thread.Sleep(500); // Simulate database reading
    }

    private static void UseDataRead()
    {
        Console.WriteLine("Using read data...");
        Thread.Sleep(500);
    }

    private static void ThinkUpData()
    {
        Console.WriteLine("Thinking up data...");
        Thread.Sleep(500);
    }

    private static void WriteDatabase()
    {
        Console.WriteLine("Writing to database...");
        Thread.Sleep(500); // Simulate writing
    }
}