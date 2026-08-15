namespace ILToCSConverter.Core.Conversion;

public interface IConversionLogger
{
    void Info(string message);
    void Warn(string message);
    void Success(string message);
    void Error(string message);
}

public sealed class CallbackLogger : IConversionLogger
{
    private readonly Action<string, string> _emit;
    private readonly object _gate = new();

    public CallbackLogger(Action<string, string> emit)
    {
        _emit = emit;
    }

    public void Info(string message) => Write("info", message);
    public void Warn(string message) => Write("warn", message);
    public void Success(string message) => Write("success", message);
    public void Error(string message) => Write("error", message);

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            _emit(level, message);
        }
    }
}

public sealed class ConsoleConversionLogger : IConversionLogger
{
    private readonly object _gate = new();

    public void Info(string message) => Write("BİLGİ", message, ConsoleColor.Gray);
    public void Warn(string message) => Write("UYARI", message, ConsoleColor.Yellow);
    public void Success(string message) => Write("BAŞARILI", message, ConsoleColor.Green);
    public void Error(string message) => Write("HATA", message, ConsoleColor.Red);

    private void Write(string level, string message, ConsoleColor color)
    {
        lock (_gate)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{level}] {message}");
            Console.ForegroundColor = previous;
        }
    }
}
