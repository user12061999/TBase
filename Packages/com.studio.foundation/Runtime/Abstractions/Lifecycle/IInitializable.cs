namespace Studio.Foundation.Lifecycle
{
    /// <summary>One-time synchronous initialization of an explicitly enrolled participant.</summary>
    /// <remarks>
    /// The host calls Initialize once on its owning thread after registry freeze, in dependency order.
    /// Receive dependencies through construction, not a global resolver. A throwing initializer
    /// must undo its own partial work; the host stops only successfully initialized participants.
    /// Initialization exceptions propagate to the host and trigger startup rollback.
    /// This interface grants neither resource ownership nor disposal responsibility.
    /// </remarks>
    public interface IInitializable
    {
        /// <summary>Acquires the participant's active state; does not return until initialization finishes.</summary>
        void Initialize();
    }
}
