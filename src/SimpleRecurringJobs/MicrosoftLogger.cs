using System;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace SimpleRecurringJobs
{
    internal class MicrosoftLogger : IJobLogger
    {
        private readonly ILoggerFactory _factory;
        private readonly ILogger _logger;

        public MicrosoftLogger(ILoggerFactory factory)
        {
            _factory = factory;
            _logger = factory.CreateLogger(typeof(MicrosoftLogger));
        }

        private MicrosoftLogger(ILoggerFactory factory, Type type)
        {
            _factory = factory;
            _logger = factory.CreateLogger(type);
        }

        public void Log(EventLevel level, Exception? ex, string message, params object[] parameters)
        {
            var logLevel = level switch
            {
                EventLevel.Warning => LogLevel.Warning,
                EventLevel.Critical => LogLevel.Critical,
                EventLevel.Error => LogLevel.Error,
                EventLevel.Informational => LogLevel.Information,
                EventLevel.Verbose => LogLevel.Trace,
                _ => LogLevel.Information
            };

            if (ex == null)
                _logger.Log(logLevel, message, parameters);
            else
                _logger.Log(logLevel, ex, message, parameters);
        }

        public IJobLogger ForSource<T>()
        {
            return new MicrosoftLogger(_factory, typeof(T));
        }
    }
}