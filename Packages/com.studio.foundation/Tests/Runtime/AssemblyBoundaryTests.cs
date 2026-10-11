using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Studio.Foundation.Tests.Runtime
{
    public sealed class AssemblyBoundaryTests
    {
        [TestCase("Studio.Foundation.Abstractions")]
        [TestCase("Studio.Foundation.Core")]
        public void CompiledAssemblyHasPackageProvenanceAndNoEngineOrGameReferences(string name)
        {
            var assembly = Assembly.Load(name);
            Assert.That(assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Any(a => a.Key == "UnityPackage" && a.Value == "com.studio.foundation"), Is.True);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name.StartsWith("UnityEngine", StringComparison.Ordinal), Is.False);
                Assert.That(reference.Name.StartsWith("UnityEditor", StringComparison.Ordinal), Is.False);
                Assert.That(reference.Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal), Is.False);
                Assert.That(reference.Name.StartsWith("TBase", StringComparison.Ordinal), Is.False);
            }
        }
    }
}
