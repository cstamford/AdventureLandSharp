namespace AdventureLandSharp.Core.Util;

public static class Log {
    public static void Error(string message) {
        _logLock.Wait();
        _logWriter.WriteLine($"[ERROR] {message}");
        Console.WriteLine($"[ERROR] {message}");
        _logLock.Release();
    }

    public static void Info(string message) {
        _logLock.Wait();
        _logWriter.WriteLine($"[INFO] {message}");
        Console.WriteLine($"[INFO] {message}");
        _logLock.Release();
    }

    public static void Debug(string message) {
        _logLock.Wait();
        _logWriter.WriteLine($"[DEBUG] {message}");
        _logLock.Release();
    }

    static Log() {
        Console.WriteLine($"Logging to {_logPath}");
    }
 
    private static readonly string _logDir = Environment.GetEnvironmentVariable("ADVENTURELAND_LOG_DIR") ?? Environment.CurrentDirectory;
    private static readonly string _logPath = Path.Combine(_logDir, $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
    private static readonly FileStream _logFile = new(_logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
    private static readonly StreamWriter _logWriter = new(new BufferedStream(_logFile));
    private static readonly SemaphoreSlim _logLock = new(1, 1);
}
