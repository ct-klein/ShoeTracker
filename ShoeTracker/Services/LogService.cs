namespace ShoeTracker.Services;

public sealed class LogService
{
    private static LogService? _instance;
    public static LogService Instance => _instance ??= new LogService();

    private readonly string _logDir;
    private readonly Lock   _lock = new();

    private LogService()
    {
        _logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(_logDir);
    }

    public void Info(string message)  => Write("INFO",  message);
    public void Warn(string message)  => Write("WARN",  message);
    public void Error(string message) => Write("ERROR", message);

    public void Error(string message, Exception ex) =>
        Write("ERROR", $"{message}: {ex.GetType().Name}: {ex.Message}");

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        lock (_lock)
        {
            try
            {
                var path = Path.Combine(_logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { /* swallow log write failures */ }
        }
    }
}
