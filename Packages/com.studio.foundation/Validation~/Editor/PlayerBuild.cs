using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Studio.Foundation.Sample;

namespace Studio.Foundation.Validation
{
    public static class PlayerBuild
    {
        public static void Mono() { Build(ScriptingImplementation.Mono2x, "Mono"); }
        public static void Il2Cpp() { Build(ScriptingImplementation.IL2CPP, "IL2CPP"); }
        public static void RequireFixture()
        {
            if (!Application.isBatchMode || !File.Exists(".foundation-validation-fixture"))
                throw new InvalidOperationException("Use a marked disposable batchmode fixture only.");
        }
        private static void Build(ScriptingImplementation backend, string label)
        {
            RequireFixture();
            var originalBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Sample application").AddComponent<MinimalBootstrap>();
                new GameObject("Smoke assertions").AddComponent<PlayerSmoke>();
                new GameObject("Camera").AddComponent<Camera>();
                new GameObject("Light").AddComponent<Light>().type = LightType.Directional;
                EditorSceneManager.SaveScene(scene, "Assets/PlayerSmoke.unity");
                Directory.CreateDirectory("Builds/" + label);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/PlayerSmoke.unity" },
                    locationPathName = "Builds/" + label + "/FoundationSmoke.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                File.WriteAllText("Builds/" + label + "/build-result.txt", report.summary.result + "; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
                if (report.summary.result != BuildResult.Succeeded) throw new Exception(label + " player build failed.");
            }
            finally { PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, originalBackend); }
        }
    }
}
