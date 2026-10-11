using System;

namespace Studio.Foundation.Logging
{
    internal static class LogValidation
    {
        public static void Level(LogLevel level, bool allowNone)
        {
            if (level < LogLevel.Trace || level > (allowNone ? LogLevel.None : LogLevel.Critical))
                throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid log severity.");
        }

        public static void Entry(LogLevel level, string category, string message)
        {
            Level(level, false);
            if (category == null) throw new ArgumentNullException(nameof(category));
            if (message == null) throw new ArgumentNullException(nameof(message));
        }
    }
}
