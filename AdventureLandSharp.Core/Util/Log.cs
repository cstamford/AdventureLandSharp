namespace AdventureLandSharp.Core.Util;

public enum LogLevel {
    Debug,
    Info,
    Error
}

public static class Log {
    static Log() {
        Info($"Log level: {_logLevel}");
        Info($"Log path: {_logPath ?? "(memory only)"}");
    }

    public static void Error(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.Error) {
            return;
        }

        Write(ConsoleColor.Red, Message(tags, message));
    }

    public static void Info(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.Info) {
            return;
        }

        Write(ConsoleColor.White, Message(tags, message));
    }

    public static void Debug(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.Debug) {
            return;
        }

        Write(ConsoleColor.Gray, Message(tags, message));
    }

    public static void Error(string message) => Error([], message);
    public static void Info(string message) => Info([], message);
    public static void Debug(string message) => Debug([], message);

    private static readonly LogLevel _logLevel = Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("ADVENTURELAND_LOG_LEVEL") ?? "Info");
    private static readonly string? _logDir = Environment.GetEnvironmentVariable("ADVENTURELAND_LOG_DIR") ?? null;
    private static readonly string? _logPath = _logDir != null ? Path.Combine(_logDir, $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt") : null;
    private static readonly Stream _logFile = _logPath != null ? new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.Read) : new NullStream();
    private static readonly StreamWriter _logWriter = new(new BufferedStream(_logFile));
    private static readonly SemaphoreSlim _logLock = new(1, 1);

    private static string Message(IEnumerable<string> tags, string message) => 
        $"[{DateTimeOffset.UtcNow:HH:mm:ss.ffff}] " + 
        (tags.Any() ? $"{string.Join("/", tags)} {message}" : message);

    private static void Write(ConsoleColor col, string message) {
        _logLock.Wait();
        ConsoleColor lastCol = Console.ForegroundColor;
        Console.ForegroundColor = col;
        Console.WriteLine(message);
        Console.ForegroundColor = lastCol;
        _logWriter.WriteLine(message);
        _logLock.Release();
    }
}

public class Logger(params string[] tags) {
    public void Error(string message) => Log.Error(tags, message);
    public void Info(string message) => Log.Info(tags, message);
    public void Debug(string message) => Log.Debug(tags, message);
}

public class NullStream : Stream {
    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position { get => 0; set => throw new InvalidOperationException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
    public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();
    public override void SetLength(long value) => throw new InvalidOperationException();
    public override void Write(byte[] buffer, int offset, int count) { }
}
