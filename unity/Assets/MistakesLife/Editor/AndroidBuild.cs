#nullable enable

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MistakesLife.Unity.Editor
{
    /// <summary>
    /// Arma el APK de prueba para Android. Lo usa el armado automático en GitHub
    /// (.github/workflows/apk.yml) y también se puede usar desde el menú
    /// "MistakesLife > Armar APK de prueba". No es parte del juego.
    /// </summary>
    public static class AndroidBuild
    {
        /// <summary>La escena principal: vacía, porque la pantalla se arma sola desde código.</summary>
        public const string MainScene = "Assets/MistakesLife/Scenes/Main.unity";

        private const string DefaultOutput = "Builds/Android/MistakesLife.apk";

        [MenuItem("MistakesLife/Armar APK de prueba")]
        public static void BuildFromMenu() => Build();

        /// <summary>Punto de entrada para el armado automático (parámetro -executeMethod).</summary>
        public static void Build()
        {
            EnsureMainScene();

            PlayerSettings.productName = "MistakesLife";
            PlayerSettings.companyName = "MistakesLife";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mistakeslife.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            string output = Argument("-customBuildPath") ?? DefaultOutput;
            if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                output = Path.Combine(output, "MistakesLife.apk");
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Armado del APK: {report.summary.result}, {report.summary.totalSize} bytes en {output}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        /// <summary>Crea la escena principal (con cámara) si todavía no existe.</summary>
        private static void EnsureMainScene()
        {
            if (File.Exists(MainScene))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(MainScene)!);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, MainScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScene, true) };
        }

        private static string? Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
