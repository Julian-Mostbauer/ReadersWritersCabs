namespace ReadersWriters;

public static class ReadersWriters
{
    // Semaphore für den exklusiven Zugriff auf den Leserzähler
    private static readonly SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);
    
    // Semaphore für den exklusiven Zugriff auf die Datenbank
    private static readonly SemaphoreSlim Db = new SemaphoreSlim(1, 1);
    
    // Semaphore, um sicherzustellen, dass wartende Schreiber Vorrang haben
    private static readonly SemaphoreSlim WriterQueue = new SemaphoreSlim(1, 1);

    // Leserzähler, um festzustellen, ob Leser auf die Datenbank zugreifen
    private static int _rc = 0;

    // Zähler für die Gesamtanzahl der Leser
    private static int _readerCount = 0;

    // Zähler für die Gesamtanzahl der Schreiber
    private static int _writerCount = 0;

    // Variable zur Steuerung der Threads (läuft oder stoppt)
    private static bool _running = true;

    // Zufallsgenerator für Verzögerungen
    private static readonly Random Random = new Random();

    // Verzögerung für das Schreiben in Millisekunden
    private const int WriterDelay = 100;

    // Verzögerung für das Lesen in Millisekunden
    private const int ReaderDelay = 50;

    // Pause zwischen den Zyklen der Schreiber
    private const int WriterPause = 300;

    // Pause zwischen den Zyklen der Leser
    private const int ReaderPause = 200;

    private static void Reader()
    {
        while (_running)
        {
            Mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            _rc++; // Erhöhe die Anzahl der aktiven Leser
            if (_rc == 1)
                Db.Wait(); // Erster Leser sperrt die Datenbank
            Mutex.Release(); // Freigabe der Sperre

            ReadDatabase(); // Lese die Datenbank
            Interlocked.Increment(ref _readerCount); // Erhöhe die Gesamtanzahl der Leser

            Mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            _rc--; // Reduziere die Anzahl der aktiven Leser
            if (_rc == 0)
                Db.Release(); // Letzter Leser gibt die Datenbank frei
            Mutex.Release(); // Freigabe der Sperre

            UseDataRead(); // Nutze die gelesenen Daten (nicht-kritischer Bereich)

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(Random.Next((int)(ReaderPause * 0.8), (int)(ReaderPause * 1.2))); 
        }
    }

    private static void Writer()
    {
        while (_running)
        {
            ThinkUpData(); // Generiere Daten (nicht-kritischer Bereich)
            Db.Wait(); // Sperre für exklusiven Zugriff auf die Datenbank
            WriteDatabase(); // Schreibe die Daten in die Datenbank
            Interlocked.Increment(ref _writerCount); // Erhöhe die Gesamtanzahl der Schreiber
            Db.Release();

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(Random.Next((int)(WriterPause * 0.8), (int)(WriterPause * 1.2))); 
        }
    }

    private static void BalancedReader()
    {
        while (_running)
        {
            WriterQueue.Wait(); // Verhindere das Lesen, wenn Schreiber warten
            Mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            _rc++; // Erhöhe die Anzahl der aktiven Leser
            if (_rc == 1)
                Db.Wait(); // Erster Leser sperrt die Datenbank
            Mutex.Release(); // Freigabe der Sperre
            WriterQueue.Release(); // Erlaube anderen Lesern oder Schreibern den Zugriff

            ReadDatabase(); // Lese die Datenbank
            Interlocked.Increment(ref _readerCount); // Erhöhe die Gesamtanzahl der Leser

            Mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            _rc--; // Reduziere die Anzahl der aktiven Leser
            if (_rc == 0)
                Db.Release(); // Letzter Leser gibt die Datenbank frei
            Mutex.Release();

            UseDataRead(); // Nutze die gelesenen Daten (nicht-kritischer Bereich)

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(Random.Next((int)(ReaderPause * 0.8), (int)(ReaderPause * 1.2))); 
        }
    }

    private static void BalancedWriter()
    {
        while (_running)
        {
            ThinkUpData(); // Generiere Daten (nicht-kritischer Bereich)
            WriterQueue.Wait(); // Stelle sicher, dass der Schreiber Vorrang hat
            Db.Wait(); // Sperre für exklusiven Zugriff auf die Datenbank
            WriteDatabase(); // Schreibe die Daten in die Datenbank
            Interlocked.Increment(ref _writerCount); // Erhöhe die Gesamtanzahl der Schreiber
            Db.Release(); // Gib die Sperre frei
            WriterQueue.Release(); // Erlaube anderen Lesern oder Schreibern den Zugriff

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(Random.Next((int)(WriterPause * 0.8), (int)(WriterPause * 1.2))); 
        }
    }

    private static void ReadDatabase()
    {
        Console.WriteLine("Reading from database..."); // Ausgabe zum Lesen
        Thread.Sleep(Random.Next((int)(ReaderDelay * 0.8), (int)(ReaderDelay * 1.2))); // Simuliere Lesezeit
    }

    private static void UseDataRead()
    {
        Console.WriteLine("Using read data..."); // Ausgabe zur Nutzung der gelesenen Daten
        Thread.Sleep(Random.Next(30, 80)); // Simuliere Verarbeitungszeit
    }

    private static void ThinkUpData()
    {
        Console.WriteLine("Thinking up data..."); // Ausgabe zur Datenerzeugung
        Thread.Sleep(Random.Next(50, 150)); // Simuliere Erzeugungszeit
    }

    private static void WriteDatabase()
    {
        Console.WriteLine("Writing to database..."); // Ausgabe zum Schreiben
        Thread.Sleep(Random.Next((int)(WriterDelay * 0.8), (int)(WriterDelay * 1.2))); // Simuliere Schreibzeit
    }

    public static void Example()
    {
        const int numReaders = 200; // Anzahl der Leser-Threads
        const int numWriters = 2; // Anzahl der Schreiber-Threads
        const int totalTime = 5000; // Gesamtlaufzeit in Millisekunden

        var threads = new List<Thread>(); // Liste zur Speicherung der Threads

        for (int i = 0; i < numReaders; i++)
        {
            var readerThread = new Thread(BalancedReader); // Erstelle einen Leser-Thread
            threads.Add(readerThread); // Füge ihn der Liste hinzu
            readerThread.Start(); // Starte den Thread
            Thread.Sleep(Random.Next(totalTime / (3 * numReaders), totalTime / numReaders)); // Verzögere den Start, um die Threads zu verteilen
        }

        for (int i = 0; i < numWriters; i++)
        {
            var writerThread = new Thread(BalancedWriter); // Erstelle einen Schreiber-Thread
            threads.Add(writerThread); // Füge ihn der Liste hinzu
            writerThread.Start(); // Starte den Thread
            Thread.Sleep(Random.Next(totalTime / (3 * numWriters), totalTime / numWriters)); // Verzögere den Start
        }

        Thread.Sleep(totalTime); // Lasse die Threads für die festgelegte Zeit laufen
        _running = false; // Beende die Schleifen durch Setzen von running auf false

        foreach (var thread in threads)
        {
            thread.Join(); // Warte, bis alle Threads beendet sind
        }

        // Gebe die Ergebnisse aus
        Console.WriteLine($"Readers executed: {_readerCount}");
        Console.WriteLine($"Writers executed: {_writerCount}");
        Console.WriteLine($"Ratio: {(_writerCount == 0 ? 0 : (double)_readerCount / _writerCount)}");
        Console.WriteLine($"Balanced Ratio: {(WriterDelay * numReaders) / (ReaderDelay * numWriters)}");
        Console.WriteLine($"Accesses per second: {(_readerCount + _writerCount) / (totalTime / 1000)}");
    }
}
