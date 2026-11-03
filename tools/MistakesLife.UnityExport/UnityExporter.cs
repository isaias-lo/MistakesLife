using System.IO;
using MistakesLife.Core.Content;

namespace MistakesLife.UnityExport
{
    /// <summary>
    /// Deja el proyecto de Unity al día con el núcleo y el contenido:
    /// copia la biblioteca del núcleo (la misma que usan las pruebas) y arma el archivo único de paquetes.
    /// Unity no compila el núcleo: lo usa ya compilado, así nunca hay dos versiones de las reglas.
    /// </summary>
    public static class UnityExporter
    {
        /// <summary>Dónde queda la biblioteca del núcleo dentro de unity/.</summary>
        public const string CoreDllPath = "Assets/Plugins/MistakesLife/MistakesLife.Core.dll";

        /// <summary>Dónde queda el archivo de paquetes dentro de unity/ (Unity lo lee con Resources.Load).</summary>
        public const string BundlePath = "Assets/MistakesLife/Resources/packs.bundle.json";

        /// <summary>Exporta desde la carpeta packs/ hacia la carpeta del proyecto de Unity.</summary>
        public static void Export(string packsDirectory, string unityDirectory)
        {
            // Primero cargar: si un paquete tiene errores, no se exporta nada.
            PackBundle bundle = PackBundle.FromDirectory(packsDirectory);
            PackLoader.LoadBundle(bundle);

            string bundleFile = Path.Combine(unityDirectory, BundlePath);
            Directory.CreateDirectory(Path.GetDirectoryName(bundleFile)!);
            File.WriteAllText(bundleFile, bundle.ToJson());

            string dllFile = Path.Combine(unityDirectory, CoreDllPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dllFile)!);
            File.Copy(typeof(PackBundle).Assembly.Location, dllFile, overwrite: true);
        }
    }
}
