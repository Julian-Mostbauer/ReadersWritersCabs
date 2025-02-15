namespace ReadersWriters;

static class Program
{
    public static void Main()
    {
        Thread readerThread = new Thread(ReadersWriters.Reader);
        Thread writerThread = new Thread(ReadersWriters.Writer);

        readerThread.Start();
        writerThread.Start();
    }
}