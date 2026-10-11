using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Studio.Foundation.Events;
using Studio.Foundation.Lifecycle;
using Studio.Foundation.Logging;
using Studio.Foundation.Modules;
using Studio.Foundation.Services;

namespace Studio.Foundation.Tests.Editor
{
    // API/boundary regression tests only. These cannot prove future implementation behavior.
    public sealed class PublicContractTests
    {
        [Test]
        public void AbstractionsExportsOnlyInterfacesAndEnumsWithoutEngineOrCoreDependencies()
        {
            var assembly = typeof(IServiceResolver).Assembly;
            var exports = assembly.GetExportedTypes();
            Assert.That(exports, Is.Not.Empty);
            Assert.That(exports.All(t => t.IsInterface || t.IsEnum), Is.True,
                "Concrete services, host state and adapters do not belong in Abstractions.");
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name.StartsWith("Unity", StringComparison.Ordinal), Is.False);
                Assert.That(reference.Name.StartsWith("Studio.Foundation.Core", StringComparison.Ordinal), Is.False);
                Assert.That(reference.Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal), Is.False);
            }
            foreach (var type in exports)
                foreach (var method in type.GetMethods())
                {
                    AssertPortableSignature(method.ReturnType, assembly);
                    foreach (var parameter in method.GetParameters())
                        AssertPortableSignature(parameter.ParameterType, assembly);
                }
        }

        private static void AssertPortableSignature(Type type, Assembly contracts)
        {
            if (type.HasElementType)
            {
                AssertPortableSignature(type.GetElementType(), contracts);
                return;
            }
            if (type.IsGenericParameter) return;
            Assert.That(type.Assembly == contracts || type.Namespace == "System" ||
                (type.Namespace != null && type.Namespace.StartsWith("System.", StringComparison.Ordinal)),
                Is.True, type.FullName);
            foreach (var argument in type.GetGenericArguments()) AssertPortableSignature(argument, contracts);
        }

        [Test]
        public void BorrowedCapabilitiesDoNotExposeRegistrationOrOwnership()
        {
            Assert.That(typeof(IServiceRegistry).IsAssignableFrom(typeof(IServiceResolver)), Is.False);
            Assert.That(typeof(IServiceResolver).IsAssignableFrom(typeof(IServiceRegistry)), Is.False);
            foreach (var type in new[] { typeof(IServiceResolver), typeof(IServiceRegistry),
                typeof(IEventPublisher), typeof(IEventSubscriber), typeof(ILogger), typeof(ILogSink), typeof(IModule) })
                Assert.That(typeof(IDisposable).IsAssignableFrom(type), Is.False, type.FullName);

            Assert.That(typeof(IEventPublisher).IsAssignableFrom(typeof(IEventBus)), Is.True);
            Assert.That(typeof(IEventSubscriber).IsAssignableFrom(typeof(IEventBus)), Is.True);
            Assert.That(typeof(IDisposable).IsAssignableFrom(typeof(IEventBus)), Is.True);
        }

        [Test]
        public void ServiceContractsRequireReferenceTypesAndExposeTypedOptionalLookup()
        {
            var register = typeof(IServiceRegistry).GetMethod("Register");
            var required = typeof(IServiceResolver).GetMethod("GetRequired");
            var optional = typeof(IServiceResolver).GetMethod("TryGet");
            foreach (var method in new[] { register, required, optional })
            {
                Assert.That(method, Is.Not.Null);
                Assert.That(method.MakeGenericMethod(typeof(ILogger)), Is.Not.Null);
                Assert.Throws<ArgumentException>(() => method.MakeGenericMethod(typeof(int)));
            }
            Assert.That(optional.ReturnType, Is.EqualTo(typeof(bool)));
            var result = optional.MakeGenericMethod(typeof(ILogger)).GetParameters().Single();
            Assert.That(result.IsOut, Is.True);
            Assert.That(result.ParameterType, Is.EqualTo(typeof(ILogger).MakeByRefType()));
        }

        [Test]
        public void EventContractsAcceptValueAndReferencePayloadsWithDisposableSubscriptions()
        {
            var publish = typeof(IEventPublisher).GetMethod("Publish");
            var subscribe = typeof(IEventSubscriber).GetMethod("Subscribe");
            foreach (var payload in new[] { typeof(int), typeof(string) })
            {
                Assert.That(publish.MakeGenericMethod(payload).GetParameters().Single().ParameterType,
                    Is.EqualTo(payload));
                var closedSubscription = subscribe.MakeGenericMethod(payload);
                Assert.That(closedSubscription.ReturnType, Is.EqualTo(typeof(IDisposable)));
                Assert.That(closedSubscription.GetParameters().Single().ParameterType,
                    Is.EqualTo(typeof(Action<>).MakeGenericType(payload)));
            }
        }

        [Test]
        public void LifecycleCapabilitiesRemainIndependentAndUseExplicitTime()
        {
            foreach (var capability in new[] { typeof(IInitializable), typeof(IStoppable),
                typeof(IUpdatable), typeof(IFixedUpdatable), typeof(ILateUpdatable) })
            {
                Assert.That(capability.GetInterfaces(), Is.Empty);
                var method = capability.GetMethods().Single();
                Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
                if (capability == typeof(IInitializable) || capability == typeof(IStoppable))
                    Assert.That(method.GetParameters(), Is.Empty);
                else
                    Assert.That(method.GetParameters().Single().ParameterType, Is.EqualTo(typeof(float)));
            }
        }

        [Test]
        public void ModuleMetadataIsReadOnlyAndDoesNotRequireRuntimeSystems()
        {
            var properties = typeof(IModule).GetProperties();
            Assert.That(properties.All(p => p.CanRead && !p.CanWrite), Is.True);
            Assert.That(typeof(IModule).GetProperty("Id").PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(typeof(IModule).GetProperty("Dependencies").PropertyType,
                Is.EqualTo(typeof(System.Collections.Generic.IReadOnlyList<string>)));
            Assert.That(typeof(IModule).GetMethods().All(m => m.IsSpecialName), Is.True);
        }

        [Test]
        public void LoggingExposesSeverityGatingAndOptionalExceptionWithoutUnityContext()
        {
            Assert.That(typeof(ILogger).GetMethod("IsEnabled").ReturnType, Is.EqualTo(typeof(bool)));
            var log = typeof(ILogger).GetMethod("Log").GetParameters();
            Assert.That(log.Select(p => p.ParameterType).ToArray(),
                Is.EqualTo(new[] { typeof(LogLevel), typeof(string), typeof(string), typeof(Exception) }));
            Assert.That(log.Last().IsOptional, Is.True);
            Assert.That(log.Last().DefaultValue, Is.Null);
            Assert.That(typeof(ILogSink).GetMethod("Write").GetParameters().Select(p => p.ParameterType).ToArray(),
                Is.EqualTo(log.Select(p => p.ParameterType).ToArray()));
            Assert.That((int)LogLevel.Trace, Is.LessThan((int)LogLevel.Debug));
            Assert.That((int)LogLevel.Debug, Is.LessThan((int)LogLevel.Information));
            Assert.That((int)LogLevel.Information, Is.LessThan((int)LogLevel.Warning));
            Assert.That((int)LogLevel.Warning, Is.LessThan((int)LogLevel.Error));
            Assert.That((int)LogLevel.Error, Is.LessThan((int)LogLevel.Critical));
            Assert.That((int)LogLevel.Critical, Is.LessThan((int)LogLevel.None));
        }
    }
}
