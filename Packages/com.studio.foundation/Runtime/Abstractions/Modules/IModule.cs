using System.Collections.Generic;

namespace Studio.Foundation.Modules
{
    /// <summary>Stable dependency metadata for an explicitly supplied module.</summary>
    /// <remarks>
    /// This contract performs no registration, initialization or discovery. The future host
    /// snapshots metadata on its owning thread and validates the whole graph before side effects.
    /// Metadata remains readable through shutdown and does not imply resource ownership.
    /// IReadOnlyList prevents mutation through this interface but is not deep immutability;
    /// implementations must keep ID and dependency contents unchanged during host composition/run.
    /// </remarks>
    public interface IModule
    {
        /// <summary>Non-whitespace, case-sensitive ordinal ID, unique within a host.</summary>
        string Id { get; }

        /// <summary>Non-null list of required module IDs; empty when there are no dependencies.</summary>
        /// <remarks>
        /// IDs must be non-whitespace and unique; self-dependencies, missing modules and cycles
        /// are invalid. Ordering in this list does not specify startup order. The host uses
        /// topological order with ordinal ID tie breaking.
        /// </remarks>
        IReadOnlyList<string> Dependencies { get; }
    }
}
