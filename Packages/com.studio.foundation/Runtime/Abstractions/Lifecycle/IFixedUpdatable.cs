namespace Studio.Foundation.Lifecycle
{
    /// <summary>Optional synchronous FixedUpdate callback for an explicitly enrolled participant.</summary>
    /// <remarks>
    /// The host dispatches only while Running on its owning thread, in stable participant order.
    /// An exception propagates to the host, faults it and suppresses subsequent ticks pending cleanup.
    /// Callbacks do not own the host; stop/dispose requests during callbacks are deferred until return.
    /// Implementing this contract does not imply initialization, stop or disposal capabilities.
    /// </remarks>
    public interface IFixedUpdatable
    {
        /// <summary>Processes one scaled fixed-step delta supplied by the host, in seconds.</summary>
        /// <param name="fixedDeltaTime">Finite, non-negative elapsed time; zero is valid.</param>
        /// <remarks>The host validates the time value before dispatch.</remarks>
        void FixedUpdate(float fixedDeltaTime);
    }
}
