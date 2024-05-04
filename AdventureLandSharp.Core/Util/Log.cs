namespace AdventureLandSharp.Core.Util;

public enum LogLevel {
    DebugVerbose,
    Debug,
    Info,
    Error
}

public static class Log {
    static Log() {
        try {
            _logLevel = Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("ADVENTURELAND_LOG_LEVEL") ?? "Debug");
            _logLevelConsole = Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("ADVENTURELAND_LOG_LEVEL_CONSOLE") ?? "Info");
            _logDir = Environment.GetEnvironmentVariable("ADVENTURELAND_LOG_DIR")!;
            _logPath = Path.Combine(_logDir, $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            _logFile = new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _logWriter = new(new BufferedStream(_logFile));
        } catch (Exception ex) {
            _logLevel = LogLevel.Info;
            _logLevelConsole = LogLevel.Info;
            _logDir = null;
            _logPath = null;
            _logFile = new NullStream();
            _logWriter = new(_logFile);

            Error($"Failed to initialize logging: {ex}");
        }

        Info($"Log level: {_logLevel}");
        Info($"Log level (console): {_logLevelConsole}");
        Info($"Log path: {_logPath ?? "(memory only)"}");
    }

    public static LogLevel LogLevel => _logLevel;
    public static bool LogLevelEnabled(LogLevel level) => LogLevel <= level;

    public static void Error(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.Error) {
            return;
        }

        Write(LogLevel.Error, tags, message);
    }

    public static void Info(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.Info) {
            return;
        }

        Write(LogLevel.Info, tags, message);
    }

    public static void Debug(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.Debug) {
            return;
        }

        Write(LogLevel.Debug, tags, message);
    }

    public static void DebugVerbose(IEnumerable<string> tags, string message) {
        if (_logLevel > LogLevel.DebugVerbose) {
            return;
        }

        Write(LogLevel.DebugVerbose, tags, message);
    }

    public static void Error(string message) => Error([], message);
    public static void Info(string message) => Info([], message);
    public static void Debug(string message) => Debug([], message);
    public static void DebugVerbose(string message) => DebugVerbose([], message);

    private static readonly LogLevel _logLevel;
    private static readonly LogLevel _logLevelConsole;
    private static readonly string? _logDir;
    private static readonly string? _logPath;
    private static readonly Stream _logFile;
    private static readonly StreamWriter _logWriter;
    private static readonly SemaphoreSlim _logLock = new(1, 1);

    private static string Message(LogLevel verbosity, IEnumerable<string> tags, string message) => 
        $"{verbosity.ToString().ToUpper()} {DateTimeOffset.UtcNow:HH:mm:ss.ffff} " +
        (tags.Any() ? $"[{string.Join("/", tags)}] {message}" : message);

    private static void Write(LogLevel verbosity, IEnumerable<string> tags, string message) {
        _logLock.Wait();

        string msg = Message(verbosity, tags, message);
        WriteFile(msg);

        if (_logLevelConsole <= verbosity) {
            ConsoleColor col = verbosity switch {
                LogLevel.DebugVerbose => ConsoleColor.DarkGray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            WriteConsole(col, msg);
        }

        _logLock.Release();
    }

    private static void WriteConsole(ConsoleColor col, string message) {
        ConsoleColor lastCol = Console.ForegroundColor;
        Console.ForegroundColor = col;
        Console.WriteLine(message);
        Console.ForegroundColor = lastCol;
    }

    private static void WriteFile(string message) {
        _logWriter.WriteLine(message);
    }
}

public class Logger(params string[] tags) {
    public void Error(string message) => Log.Error(tags, message);
    public void Error(IEnumerable<string> otherTags, string message) => Log.Error(tags.Concat(otherTags), message);
    public void Info(string message) => Log.Info(tags, message);
    public void Info(IEnumerable<string> otherTags, string message) => Log.Info(tags.Concat(otherTags), message);
    public void Debug(string message) => Log.Debug(tags, message);
    public void Debug(IEnumerable<string> otherTags, string message) => Log.Debug(tags.Concat(otherTags), message);
    public void DebugVerbose(string message) => Log.DebugVerbose(tags, message);
    public void DebugVerbose(IEnumerable<string> otherTags, string message) => Log.DebugVerbose(tags.Concat(otherTags), message);
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
