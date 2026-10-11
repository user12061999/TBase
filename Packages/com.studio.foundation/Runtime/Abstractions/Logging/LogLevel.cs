namespace Studio.Foundation.Logging
{
    /// <summary>Increasing severity; None is a filtering sentinel, never a message severity.</summary>
    public enum LogLevel
    {
        /// <summary>Fine-grained diagnostic trace.</summary>
        Trace = 0,
        /// <summary>Development diagnostics.</summary>
        Debug = 1,
        /// <summary>Normal operational information.</summary>
        Information = 2,
        /// <summary>A recoverable or unexpected condition.</summary>
        Warning = 3,
        /// <summary>An operation failed.</summary>
        Error = 4,
        /// <summary>A critical failure.</summary>
        Critical = 5,
        /// <summary>Disables output when used as a minimum threshold.</summary>
        None = 6
    }
}
