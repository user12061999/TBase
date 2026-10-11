using System;
using System.Collections.Generic;
using Studio.Foundation.Modules;

namespace Studio.Foundation.Lifecycle
{
    internal sealed class ModuleRegistration
    {
        public readonly IModule Metadata;
        public readonly Action<ModuleContext> Configure;
        public ModuleRegistration(IModule metadata, Action<ModuleContext> configure)
        {
            Metadata = metadata;
            Configure = configure;
        }
    }

    internal static class ModuleGraph
    {
        private sealed class Node
        {
            public ModuleRegistration Registration;
            public string[] Dependencies;
            public int Remaining;
            public readonly List<string> Dependents = new List<string>();
        }

        // Snapshot first; configuration is never called from graph validation.
        public static ModuleRegistration[] Sort(List<ModuleRegistration> registrations)
        {
            var nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
            foreach (var registration in registrations)
            {
                var id = registration.Metadata.Id;
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidOperationException("Module ID must not be blank.");
                if (nodes.ContainsKey(id))
                    throw new InvalidOperationException("Duplicate module ID: " + id);
                var required = registration.Metadata.Dependencies;
                if (required == null)
                    throw new InvalidOperationException("Null dependencies for module: " + id);
                var dependencies = new string[required.Count];
                var unique = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < dependencies.Length; i++)
                {
                    var dependency = required[i];
                    if (string.IsNullOrWhiteSpace(dependency) || !unique.Add(dependency))
                        throw new InvalidOperationException("Blank or duplicate dependency for module: " + id);
                    dependencies[i] = dependency;
                }
                nodes.Add(id, new Node { Registration = registration, Dependencies = dependencies,
                    Remaining = dependencies.Length });
            }

            var ready = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var entry in nodes)
            {
                foreach (var dependency in entry.Value.Dependencies)
                {
                    if (!nodes.TryGetValue(dependency, out var target))
                        throw new InvalidOperationException("Missing module " + dependency + " required by " + entry.Key);
                    target.Dependents.Add(entry.Key);
                }
                if (entry.Value.Remaining == 0) ready.Add(entry.Key);
            }

            var ordered = new ModuleRegistration[registrations.Count];
            var count = 0;
            while (ready.Count != 0)
            {
                var id = ready.Min;
                ready.Remove(id);
                var node = nodes[id];
                ordered[count++] = node.Registration;
                foreach (var dependent in node.Dependents)
                    if (--nodes[dependent].Remaining == 0) ready.Add(dependent);
            }
            if (count != ordered.Length)
                throw new InvalidOperationException("Module dependency cycle detected.");
            return ordered;
        }
    }
}
