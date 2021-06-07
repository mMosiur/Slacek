using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Slacek.Server
{
    public class ConsoleLogger : ILogger
    {
        private static readonly Dictionary<LogLevel, string> _names = new()
        {
            [LogLevel.Trace] = "TRACE",
            [LogLevel.Debug] = "DEBUG",
            [LogLevel.Information] = "INFO",
            [LogLevel.Warning] = "WARNING",
            [LogLevel.Error] = "ERROR",
            [LogLevel.Critical] = "CATASTROPHIC FAILURE",
            [LogLevel.None] = ""
        };

        private const LogLevel DefaultMinLogLevel = LogLevel.Information;
        private readonly LogLevel _minLogLevel;

        public ConsoleLogger(LogLevel minLogLevel = DefaultMinLogLevel)
        {
            _minLogLevel = minLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel) || logLevel == LogLevel.None)
            {
                return;
            }
            Console.WriteLine($"[{_names[logLevel]}] {formatter(state, exception)}");
        }
    }
}
