namespace Studio.Foundation.Lifecycle
{
    /// <summary>Observable state of one single-use application host.</summary>
    public enum ApplicationState
    {
        /// <summary>Accepts module definitions and pre-start owned resources.</summary>
        Created,
        /// <summary>Validating, configuring, freezing and initializing.</summary>
        Starting,
        /// <summary>Accepts tick dispatch.</summary>
        Running,
        /// <summary>Stopping participants and disposing owned resources.</summary>
        Stopping,
        /// <summary>Normal cleanup finished, possibly with reported cleanup errors.</summary>
        Stopped,
        /// <summary>Startup or tick failed; cleanup has finished.</summary>
        Faulted
    }
}
