using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cheyne.LogEngine
{
    public class Log
    {
        public static event EventHandler<LogEventArgs> MessageCaptured;

        public static void WriteLine(uint severity, string message)
        {
            MessageCaptured?.Invoke(typeof(Log), new LogEventArgs(message, severity));
        }

        public static void WriteLine( Severity severity, string message)
        {
            WriteLine((uint)severity, message);
        }

        /// <summary>
        /// Helper function to log a debug message. Excluded at compile time for release builds.
        /// </summary>
        /// <param name="message">message, will be prepended with "info: "</param>
        public static void Debug(string message)
        {
#if DEBUG
            WriteLine(Severity.Debug, "debug: " + message);
#endif
        }

        /// <summary>
        /// Helper function to log an informational message.
        /// </summary>
        /// <param name="message">message, will be prepended with "info: "</param>
        public static void Info(string message)
        {
            WriteLine(Severity.Info, "info: " + message);
        }

        /// <summary>
        /// Helper function to log a warning message.
        /// </summary>
        /// <param name="message">message, will be prepended with "warning: "</param>
        public static void Warning(string message)
        {
            WriteLine(Severity.Warning, "warning: " + message);
        }

        /// <summary>
        /// Helper function to log an error message.
        /// </summary>
        /// <param name="message">message, will be prepended with "error: "</param>
        public static void Error(string message)
        {
            WriteLine(Severity.Error, "error: " + message);
        }

        /// <summary>
        /// Get a filtering object which will only raise events of the specified severity or higher.
        /// </summary>
        /// <param name="highPassFilter">a severity number below which logs will not be reported</param>
        /// <returns></returns>
        public static LogFilter GetFilter(uint highPassFilter)
        {
            return new LogFilter(highPassFilter);
        }

    }
}
