using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Studio.Foundation.Tests.Editor
{
    public sealed class PackageStructureTests
    {
        private const string PackageId = "com.studio.foundation";
        private const string AssetRoot = "Packages/" + PackageId;

        [Serializable]
        private sealed class Definition
        {
            public string name = "";
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
            public string[] defineConstraints = Array.Empty<string>();
            public bool noEngineReferences = false;
            public bool autoReferenced = true;
        }

        private static PackageInfo FindPackage()
        {
            var package = PackageInfo.FindForAssetPath(AssetRoot + "/package.json");
            Assert.That(package, Is.Not.Null, "Package must be resolved by UPM.");
            return package;
        }

        private static Definition ReadDefinition(string relative)
        {
            return JsonUtility.FromJson<Definition>(File.ReadAllText(
                Path.Combine(FindPackage().resolvedPath, relative)));
        }

        [Test]
        public void PackageIsRecognizedWithNoMandatoryDependencies()
        {
            var package = FindPackage();
            Assert.That(package.name, Is.EqualTo(PackageId));
            Assert.That(package.version, Is.EqualTo("0.1.0-preview.1"));
            Assert.That(package.dependencies, Is.Empty);
        }

        [Test]
        public void RuntimeDefinitionsEnforceAcyclicPureBoundaries()
        {
            var abstractions = ReadDefinition("Runtime/Abstractions/Studio.Foundation.Abstractions.asmdef");
            var core = ReadDefinition("Runtime/Core/Studio.Foundation.Core.asmdef");
            Assert.That(abstractions.name, Is.EqualTo("Studio.Foundation.Abstractions"));
            Assert.That(core.name, Is.EqualTo("Studio.Foundation.Core"));
            Assert.That(abstractions.references, Is.Empty);
            Assert.That(core.references, Is.EqualTo(new[] { abstractions.name }));
            foreach (var definition in new[] { abstractions, core })
            {
                Assert.That(definition.noEngineReferences, Is.True);
                Assert.That(definition.autoReferenced, Is.False);
                Assert.That(definition.includePlatforms, Is.Empty);
            }
        }

        [Test]
        public void BothRuntimeAssembliesHaveActualCompiledOutputs()
        {
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            foreach (var name in new[] { "Studio.Foundation.Abstractions", "Studio.Foundation.Core" })
            {
                var assembly = assemblies.Single(a => a.name == name);
                Assert.That(assembly.sourceFiles, Is.Not.Empty);
                Assert.That(File.Exists(assembly.outputPath), Is.True, assembly.outputPath);
                Assert.That(assembly.compiledAssemblyReferences.Any(p =>
                    Path.GetFileName(p).StartsWith("UnityEngine", StringComparison.Ordinal) ||
                    Path.GetFileName(p).StartsWith("UnityEditor", StringComparison.Ordinal)), Is.False);
            }
        }

        [Test]
        public void UnityAdapterDependsInwardAndCompilesWithoutEditorReferences()
        {
            var definition = ReadDefinition("Runtime/Unity/Studio.Foundation.Unity.asmdef");
            Assert.That(definition.references, Is.EqualTo(new[] { "Studio.Foundation.Abstractions", "Studio.Foundation.Core" }));
            Assert.That(definition.noEngineReferences, Is.False);
            Assert.That(definition.autoReferenced, Is.False);
            Assert.That(definition.includePlatforms, Is.Empty);
            var assembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Single(a => a.name == "Studio.Foundation.Unity");
            Assert.That(File.Exists(assembly.outputPath), Is.True);
            // Unity's Editor compiler supplies engine/editor reference candidates. Inspect the
            // emitted assembly to distinguish actual dependencies from those compiler inputs.
            Assert.That(System.Reflection.Assembly.Load(definition.name).GetReferencedAssemblies()
                .Any(a => a.Name.StartsWith("UnityEditor", StringComparison.Ordinal)), Is.False);
            Assert.That(CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Any(a => a.name == definition.name), Is.True);
        }

        [Test]
        public void TestsAreConstrainedAndExcludedFromOrdinaryPlayerCompilation()
        {
            var editor = ReadDefinition("Tests/Editor/Studio.Foundation.Tests.Editor.asmdef");
            var runtime = ReadDefinition("Tests/Runtime/Studio.Foundation.Tests.Runtime.asmdef");
            Assert.That(editor.includePlatforms, Is.EqualTo(new[] { "Editor" }));
            Assert.That(runtime.includePlatforms, Is.Empty);
            Assert.That(runtime.references.Any(r => r.StartsWith("UnityEditor", StringComparison.Ordinal)), Is.False);
            foreach (var definition in new[] { editor, runtime })
            {
                Assert.That(definition.defineConstraints, Does.Contain("UNITY_INCLUDE_TESTS"));
                Assert.That(definition.autoReferenced, Is.False);
            }
            var playerNames = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Select(a => a.name).ToArray();
            Assert.That(playerNames, Does.Not.Contain(editor.name));
            Assert.That(playerNames, Does.Not.Contain(runtime.name));
            Assert.That(playerNames, Does.Contain("Studio.Foundation.Core"));
            Assert.That(playerNames, Does.Contain("Studio.Foundation.Abstractions"));
        }
    }
}
