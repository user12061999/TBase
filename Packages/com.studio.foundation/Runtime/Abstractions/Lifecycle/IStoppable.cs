namespace Studio.Foundation.Lifecycle
{
    /// <summary>Stops activity without transferring or implying resource ownership.</summary>
    /// <remarks>
    /// The host calls Stop on its owning thread, at most once per enrolled participant.
    /// Successfully initialized participants stop in reverse initialization order.
    /// A stop-only participant becomes eligible when its enrollment is reached during startup.
    /// Stop is separate from IDisposable: it ceases activity; explicitly owned resources are
    /// disposed in a later cleanup pass. A thrown exception is collected by the host while
    /// remaining cleanup continues. Implementations should tolerate repeated direct Stop calls.
    /// </remarks>
    public interface IStoppable
    {
        /// <summary>Ceases activity and detaches subscriptions owned by the participant.</summary>
        void Stop();
    }
}
