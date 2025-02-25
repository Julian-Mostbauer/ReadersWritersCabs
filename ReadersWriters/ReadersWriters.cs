namespace ReadersWriters;

public static class ReadersWriters
{
    // Semaphore für den exklusiven Zugriff auf den Leserzähler
    private static SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
    
    // Semaphore für den exklusiven Zugriff auf die Datenbank
    private static SemaphoreSlim db = new SemaphoreSlim(1, 1);
    
    // Semaphore, um sicherzustellen, dass wartende Schreiber Vorrang haben
    private static SemaphoreSlim writerQueue = new SemaphoreSlim(1, 1);

    // Leserzähler, um festzustellen, ob Leser auf die Datenbank zugreifen
    private static int rc = 0;

    // Zähler für die Gesamtanzahl der Leser
    private static int readerCount = 0;

    // Zähler für die Gesamtanzahl der Schreiber
    private static int writerCount = 0;

    // Variable zur Steuerung der Threads (läuft oder stoppt)
    private static bool running = true;

    // Zufallsgenerator für Verzögerungen
    private static Random random = new Random();

    // Verzögerung für das Schreiben in Millisekunden
    private const int writerDelay = 100;

    // Verzögerung für das Lesen in Millisekunden
    private const int readerDelay = 50;

    // Pause zwischen den Zyklen der Schreiber
    private const int writerPause = 300;

    // Pause zwischen den Zyklen der Leser
    private const int readerPause = 200;

    private static void Reader()
    {
        while (running)
        {
            mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            rc++; // Erhöhe die Anzahl der aktiven Leser
            if (rc == 1)
                db.Wait(); // Erster Leser sperrt die Datenbank
            mutex.Release(); // Freigabe der Sperre

            ReadDatabase(); // Lese die Datenbank
            Interlocked.Increment(ref readerCount); // Erhöhe die Gesamtanzahl der Leser

            mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            rc--; // Reduziere die Anzahl der aktiven Leser
            if (rc == 0)
                db.Release(); // Letzter Leser gibt die Datenbank frei
            mutex.Release(); // Freigabe der Sperre

            UseDataRead(); // Nutze die gelesenen Daten (nicht-kritischer Bereich)

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(random.Next((int)(readerPause * 0.8), (int)(readerPause * 1.2))); 
        }
    }

    private static void Writer()
    {
        while (running)
        {
            ThinkUpData(); // Generiere Daten (nicht-kritischer Bereich)
            db.Wait(); // Sperre für exklusiven Zugriff auf die Datenbank
            WriteDatabase(); // Schreibe die Daten in die Datenbank
            Interlocked.Increment(ref writerCount); // Erhöhe die Gesamtanzahl der Schreiber
            db.Release();

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(random.Next((int)(writerPause * 0.8), (int)(writerPause * 1.2))); 
        }
    }

    private static void BalancedReader()
    {
        while (running)
        {
            writerQueue.Wait(); // Verhindere das Lesen, wenn Schreiber warten
            mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            rc++; // Erhöhe die Anzahl der aktiven Leser
            if (rc == 1)
                db.Wait(); // Erster Leser sperrt die Datenbank
            mutex.Release(); // Freigabe der Sperre
            writerQueue.Release(); // Erlaube anderen Lesern oder Schreibern den Zugriff

            ReadDatabase(); // Lese die Datenbank
            Interlocked.Increment(ref readerCount); // Erhöhe die Gesamtanzahl der Leser

            mutex.Wait(); // Sperre für exklusiven Zugriff auf rc
            rc--; // Reduziere die Anzahl der aktiven Leser
            if (rc == 0)
                db.Release(); // Letzter Leser gibt die Datenbank frei
            mutex.Release();

            UseDataRead(); // Nutze die gelesenen Daten (nicht-kritischer Bereich)

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(random.Next((int)(readerPause * 0.8), (int)(readerPause * 1.2))); 
        }
    }

    private static void BalancedWriter()
    {
        while (running)
        {
            ThinkUpData(); // Generiere Daten (nicht-kritischer Bereich)
            writerQueue.Wait(); // Stelle sicher, dass der Schreiber Vorrang hat
            db.Wait(); // Sperre für exklusiven Zugriff auf die Datenbank
            WriteDatabase(); // Schreibe die Daten in die Datenbank
            Interlocked.Increment(ref writerCount); // Erhöhe die Gesamtanzahl der Schreiber
            db.Release(); // Gib die Sperre frei
            writerQueue.Release(); // Erlaube anderen Lesern oder Schreibern den Zugriff

            // Pause mit zufälliger Dauer innerhalb eines Bereichs
            Thread.Sleep(random.Next((int)(writerPause * 0.8), (int)(writerPause * 1.2))); 
        }
    }

    private static void ReadDatabase()
    {
        Console.WriteLine("Reading from database..."); // Ausgabe zum Lesen
        Thread.Sleep(random.Next((int)(readerDelay * 0.8), (int)(readerDelay * 1.2))); // Simuliere Lesezeit
    }

    private static void UseDataRead()
    {
        Console.WriteLine("Using read data..."); // Ausgabe zur Nutzung der gelesenen Daten
        Thread.Sleep(random.Next(30, 80)); // Simuliere Verarbeitungszeit
    }

    private static void ThinkUpData()
    {
        Console.WriteLine("Thinking up data..."); // Ausgabe zur Datenerzeugung
        Thread.Sleep(random.Next(50, 150)); // Simuliere Erzeugungszeit
    }

    private static void WriteDatabase()
    {
        Console.WriteLine("Writing to database..."); // Ausgabe zum Schreiben
        Thread.Sleep(random.Next((int)(writerDelay * 0.8), (int)(writerDelay * 1.2))); // Simuliere Schreibzeit
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
            Thread.Sleep(random.Next(totalTime / (3 * numReaders), totalTime / numReaders)); // Verzögere den Start, um die Threads zu verteilen
        }

        for (int i = 0; i < numWriters; i++)
        {
            var writerThread = new Thread(BalancedWriter); // Erstelle einen Schreiber-Thread
            threads.Add(writerThread); // Füge ihn der Liste hinzu
            writerThread.Start(); // Starte den Thread
            Thread.Sleep(random.Next(totalTime / (3 * numWriters), totalTime / numWriters)); // Verzögere den Start
        }

        Thread.Sleep(totalTime); // Lasse die Threads für die festgelegte Zeit laufen
        running = false; // Beende die Schleifen durch Setzen von running auf false

        foreach (var thread in threads)
        {
            thread.Join(); // Warte, bis alle Threads beendet sind
        }

        // Gebe die Ergebnisse aus
        Console.WriteLine($"Readers executed: {readerCount}");
        Console.WriteLine($"Writers executed: {writerCount}");
        Console.WriteLine($"Ratio: {(writerCount == 0 ? 0 : (double)readerCount / writerCount)}");
        Console.WriteLine($"Balanced Ratio: {(writerDelay * numReaders) / (readerDelay * numWriters)}");
        Console.WriteLine($"Accesses per second: {(readerCount + writerCount) / (totalTime / 1000)}");
    }
}
