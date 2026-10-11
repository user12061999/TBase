using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Studio.Foundation.Services;

namespace Studio.Foundation.Tests.Editor
{
    public sealed class ServiceRegistryTests
    {
        private interface IService { }
        private interface IAlias { }
        private sealed class Service : IService, IAlias, IDisposable
        {
            public int DisposeCalls;
            public void Dispose() { DisposeCalls++; }
        }

        [Test]
        public void BuildResolvesExactContractAndReturnsSameInstance()
        {
            using (var builder = new ServiceRegistryBuilder())
            {
                var service = new Service();
                builder.Registry.Register<IService>(service);
                var resolver = builder.Build();
                Assert.That(resolver.GetRequired<IService>(), Is.SameAs(service));
                Assert.That(resolver.TryGet<IService>(out var resolved), Is.True);
                Assert.That(resolved, Is.SameAs(service));
                Assert.That(resolver.TryGet<Service>(out var concrete), Is.False);
                Assert.That(concrete, Is.Null);
                Assert.That(resolver.TryGet<IAlias>(out var alias), Is.False);
                Assert.That(alias, Is.Null);
            }
        }

        [Test]
        public void MissingRequiredServiceNamesContractAndOptionalDoesNotThrow()
        {
            using (var builder = new ServiceRegistryBuilder())
            {
                var resolver = builder.Build();
                var error = Assert.Throws<KeyNotFoundException>(() => resolver.GetRequired<IService>());
                Assert.That(error.Message, Does.Contain(typeof(IService).FullName));
                Assert.That(resolver.TryGet<IService>(out var result), Is.False);
                Assert.That(result, Is.Null);
            }
        }

        [Test]
        public void NullAndDuplicateRegistrationFailWithoutChangingOriginal()
        {
            using (var builder = new ServiceRegistryBuilder())
            {
                Assert.Throws<ArgumentNullException>(() => builder.Registry.Register<IService>(null));
                var first = new Service();
                builder.Registry.Register<IService>(first);
                Assert.Throws<InvalidOperationException>(() => builder.Registry.Register<IService>(new Service()));
                Assert.Throws<InvalidOperationException>(() => builder.Registry.Register<IService>(first));
                Assert.That(builder.Build().GetRequired<IService>(), Is.SameAs(first));
            }
        }

        [Test]
        public void BuildFreezesRetainedRegistryAndCannotBeRepeated()
        {
            using (var builder = new ServiceRegistryBuilder())
            {
                var registry = builder.Registry;
                builder.Build();
                Assert.Throws<InvalidOperationException>(() => registry.Register<IService>(new Service()));
                Assert.Throws<InvalidOperationException>(() => builder.Build());
            }
        }

        [Test]
        public void CapabilitiesCannotBeCastToBuilderDisposalOrOppositeCapability()
        {
            using (var builder = new ServiceRegistryBuilder())
            {
                var registry = builder.Registry;
                Assert.That(registry, Is.Not.InstanceOf<IServiceResolver>());
                Assert.That(registry, Is.Not.InstanceOf<IDisposable>());
                Assert.That(registry, Is.Not.InstanceOf<ServiceRegistryBuilder>());
                var resolver = builder.Build();
                Assert.That(resolver, Is.Not.InstanceOf<IServiceRegistry>());
                Assert.That(resolver, Is.Not.InstanceOf<IDisposable>());
                Assert.That(resolver, Is.Not.InstanceOf<ServiceRegistryBuilder>());
            }
        }

        [Test]
        public void DisposalInvalidatesAllViewsAndNeverDisposesAliasedServices()
        {
            var builder = new ServiceRegistryBuilder();
            var registry = builder.Registry;
            var service = new Service();
            registry.Register<IService>(service);
            registry.Register<IAlias>(service);
            var resolver = builder.Build();
            Assert.That(resolver.GetRequired<IAlias>(), Is.SameAs(service));
            builder.Dispose();
            builder.Dispose();
            Assert.That(service.DisposeCalls, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => registry.Register<IService>(service));
            Assert.Throws<ObjectDisposedException>(() => resolver.GetRequired<IService>());
            Assert.Throws<ObjectDisposedException>(() => resolver.TryGet<IService>(out _));
            Assert.Throws<ObjectDisposedException>(() => builder.Build());
            Assert.Throws<ObjectDisposedException>(() => { var unused = builder.Registry; });
        }

        [Test]
        public void DisposalBeforeBuildClosesScopeWithoutDisposingServices()
        {
            var builder = new ServiceRegistryBuilder();
            var service = new Service();
            builder.Registry.Register<IService>(service);
            builder.Dispose();
            Assert.Throws<ObjectDisposedException>(() => builder.Build());
            Assert.That(service.DisposeCalls, Is.Zero);
        }

        [Test]
        public void IndependentScopesNeverShareRegistrations()
        {
            using (var first = new ServiceRegistryBuilder())
            using (var second = new ServiceRegistryBuilder())
            {
                first.Registry.Register<IService>(new Service());
                Assert.That(first.Build().TryGet<IService>(out _), Is.True);
                Assert.That(second.Build().TryGet<IService>(out _), Is.False);
            }
        }

        [Test]
        public void WrongThreadOperationsFailWithoutMutatingTheScope()
        {
            using (var builder = new ServiceRegistryBuilder())
            {
                var registry = builder.Registry;
                Assert.That(OnOtherThread(() => registry.Register<IService>(new Service())), Is.TypeOf<InvalidOperationException>());
                Assert.That(OnOtherThread(() => builder.Build()), Is.TypeOf<InvalidOperationException>());
                Assert.That(OnOtherThread(() => builder.Dispose()), Is.TypeOf<InvalidOperationException>());
                registry.Register<IService>(new Service());
                var resolver = builder.Build();
                Assert.That(OnOtherThread(() => resolver.GetRequired<IService>()), Is.TypeOf<InvalidOperationException>());
                Assert.That(OnOtherThread(() => resolver.TryGet<IService>(out _)), Is.TypeOf<InvalidOperationException>());
                Assert.That(resolver.GetRequired<IService>(), Is.Not.Null);
            }
        }

        private static Exception OnOtherThread(Action action)
        {
            Exception error = null;
            var worker = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
            worker.IsBackground = true;
            worker.Start();
            Assert.That(worker.Join(5000), Is.True, "Worker did not finish.");
            return error;
        }
    }
}
