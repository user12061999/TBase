using System;
using System.Collections.Generic;
using Studio.Foundation.Internal;

namespace Studio.Foundation.Services
{
    /// <summary>Owns registration storage and the lifetime of borrowed service capabilities.</summary>
    /// <remarks>
    /// Create, register, build and dispose on one thread. Build freezes once and exposes a resolver;
    /// the registry view cannot be cast to a resolver, builder or IDisposable. Disposal invalidates
    /// all views and releases references, but never stops or disposes registered services.
    /// Keep this builder alive until the composition scope ends.
    /// </remarks>
    public sealed class ServiceRegistryBuilder : IDisposable
    {
        private readonly ThreadGuard thread = new ThreadGuard();
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private readonly IServiceRegistry registry;
        private bool frozen;
        private bool disposed;

        /// <summary>Creates an empty composition scope bound to the current thread.</summary>
        public ServiceRegistryBuilder() { registry = new RegistryView(this); }

        /// <summary>Registration-only access; retained views also reject writes after Build.</summary>
        public IServiceRegistry Registry
        {
            get { CheckAlive(); return registry; }
        }

        /// <summary>Freezes registrations and returns a read-only resolver. May be called once.</summary>
        /// <exception cref="InvalidOperationException">Already built or accessed on the wrong thread.</exception>
        /// <exception cref="ObjectDisposedException">The scope has been disposed.</exception>
        public IServiceResolver Build()
        {
            CheckAlive();
            if (frozen) throw new InvalidOperationException("The service registry has already been built.");
            var resolver = new ResolverView(this);
            frozen = true;
            return resolver;
        }

        /// <summary>Invalidates capabilities and releases references. Idempotent on the owning thread.</summary>
        public void Dispose()
        {
            thread.Check();
            if (disposed) return;
            disposed = true;
            services.Clear();
        }

        private void CheckAlive()
        {
            thread.Check();
            if (disposed) throw new ObjectDisposedException(nameof(ServiceRegistryBuilder));
        }

        private void Register<TService>(TService instance) where TService : class
        {
            CheckAlive();
            if (frozen) throw new InvalidOperationException("Registration is frozen.");
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var contract = typeof(TService);
            if (services.ContainsKey(contract))
                throw new InvalidOperationException("Service already registered: " + contract.FullName);
            services.Add(contract, instance);
        }

        private bool TryGet<TService>(out TService service) where TService : class
        {
            CheckAlive();
            if (!frozen) throw new InvalidOperationException("Composition has not been frozen.");
            if (services.TryGetValue(typeof(TService), out var instance))
            {
                service = (TService)instance;
                return true;
            }
            service = null;
            return false;
        }

        private sealed class RegistryView : IServiceRegistry
        {
            private readonly ServiceRegistryBuilder owner;
            public RegistryView(ServiceRegistryBuilder owner) { this.owner = owner; }
            public void Register<TService>(TService instance) where TService : class => owner.Register(instance);
        }

        private sealed class ResolverView : IServiceResolver
        {
            private readonly ServiceRegistryBuilder owner;
            public ResolverView(ServiceRegistryBuilder owner) { this.owner = owner; }
            public bool TryGet<TService>(out TService service) where TService : class => owner.TryGet(out service);
            public TService GetRequired<TService>() where TService : class
            {
                if (owner.TryGet<TService>(out var service)) return service;
                throw new KeyNotFoundException("Service not registered: " + typeof(TService).FullName);
            }
        }
    }
}
