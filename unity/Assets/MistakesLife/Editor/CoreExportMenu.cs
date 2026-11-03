using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MistakesLife.Unity.Editor
{
    /// <summary>
    /// Menú "MistakesLife > Actualizar núcleo y paquetes": corre tools/MistakesLife.UnityExport
    /// (necesita el SDK de .NET 8 instalado) y recarga los archivos.
    /// No es parte del juego: solo ayuda dentro del editor.
    /// </summary>
    public static class CoreExportMenu
    {
        [MenuItem("MistakesLife/Actualizar núcleo y paquetes")]
        public static void Export()
        {
            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var start = new ProcessStartInfo("dotnet", "run --project tools/MistakesLife.UnityExport")
            {
                WorkingDirectory = repo,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            try
            {
                using Process process = Process.Start(start)!;
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log(output);
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError(output + errors);
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Debug.LogError("No encontré el comando dotnet. Instala el SDK de .NET 8 desde https://dotnet.microsoft.com/download.");
            }
        }
    }
}
