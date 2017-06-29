using System;

namespace Pocket
{
    internal static partial class Log
    {
        public static void Info(
            string message,
            params object[] args) =>
            Logger.Default.Info(message, args);

        public static void Warning(
            string message,
            Exception exception = null,
            params object[] args) =>
            Logger.Default.Warning(message, exception, args);

        public static void Error(
            string message,
            Exception exception = null,
            params object[] args) =>
            Logger.Default.Error(message, exception, args);
    }
}
