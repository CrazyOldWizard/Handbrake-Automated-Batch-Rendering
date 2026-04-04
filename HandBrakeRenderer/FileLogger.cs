using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace HandBrakeRenderer;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider()
    {
        var rootFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        _logFilePath = Path.Combine(rootFolder, "EncodeLog.txt");
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logFilePath));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logFilePath;
    private static readonly object _lock = new();

    public FileLogger(string categoryName, string logFilePath)
    {
        _categoryName = categoryName;
        _logFilePath = logFilePath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {formatter(state, exception)}";
        
        if (exception != null)
        {
            message += $"{Environment.NewLine}{exception}";
        }

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, message + Environment.NewLine);
        }
    }
}
