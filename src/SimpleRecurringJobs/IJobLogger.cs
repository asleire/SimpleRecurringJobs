using System;
using System.Diagnostics.Tracing;

namespace SimpleRecurringJobs
{
    public interface IJobLogger
    {
        void Log(EventLevel level, Exception? ex, string message, params object[] parameters);

        void Log(EventLevel level, string message, params object[] parameters)
        {
            Log(level, null, message, parameters);
        }

        void LogError(Exception ex, string message, params object[] parameters)
        {
            Log(EventLevel.Error, ex, message, parameters);
        }

        void LogError(string message, params object[] parameters)
        {
            Log(EventLevel.Error, message, parameters);
        }


        void LogInfo(Exception ex, string message, params object[] parameters)
        {
            Log(EventLevel.Informational, ex, message, parameters);
        }

        void LogInfo(string message, params object[] parameters)
        {
            Log(EventLevel.Informational, message, parameters);
        }


        void LogVerbose(Exception ex, string message, params object[] parameters)
        {
            Log(EventLevel.Verbose, ex, message, parameters);
        }

        void LogVerbose(string message, params object[] parameters)
        {
            Log(EventLevel.Verbose, message, parameters);
        }

        IJobLogger ForSource<T>();
    }
}