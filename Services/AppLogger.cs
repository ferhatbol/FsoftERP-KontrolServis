namespace FsoftERP.KontrolServis.Services;

public static class AppLogger
{
    private static readonly object _lock = new();
    private static string _logDir = Path.Combine(AppContext.BaseDirectory, "logs");
    public static event Action<string>? OnLogMessage;

    public static void Initialize(string logDir)
    {
        _logDir = Path.IsPathRooted(logDir) ? logDir : Path.Combine(AppContext.BaseDirectory, logDir);
        try
        {
            if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
            CleanupOldLogs(30);
        }
        catch { }
    }

    public static void Info(string message) => Log("INFO", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message, Exception? ex = null)
    {
        string fullMsg = ex != null ? $"{message} | Hata: {ex.Message}\n{ex.StackTrace}" : message;
        Log("ERROR", fullMsg);
    }
    public static void Success(string message) => Log("SUCCESS", message);

    private static void Log(string level, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string formatted = $"[{timestamp}] [{level}] {message}";

        try
        {
            Console.WriteLine(formatted);
        }
        catch { }

        try
        {
            OnLogMessage?.Invoke(formatted);
        }
        catch { }

        lock (_lock)
        {
            try
            {
                if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
                string fileName = $"service_{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(_logDir, fileName);
                File.AppendAllText(fullPath, formatted + Environment.NewLine);
            }
            catch { }
        }
    }

    private static void CleanupOldLogs(int retentionDays)
    {
        try
        {
            if (!Directory.Exists(_logDir)) return;
            var files = Directory.GetFiles(_logDir, "service_*.log");
            var threshold = DateTime.Now.AddDays(-retentionDays);
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.LastWriteTime < threshold)
                {
                    fi.Delete();
                }
            }
        }
        catch { }
    }
}
