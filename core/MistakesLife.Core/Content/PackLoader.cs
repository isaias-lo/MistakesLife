using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Carga paquetes desde carpetas (para pruebas y herramientas).
    /// Cada paquete tiene pack.json y, opcionalmente, country.json y las carpetas events/ y seeds/
    /// con archivos .json que contienen una lista.
    /// </summary>
    public static class PackLoader
    {
        /// <summary>Carga todos los paquetes que encuentre bajo una carpeta raíz (por ejemplo packs/).</summary>
        public static ContentDatabase LoadAll(string rootDirectory)
        {
            var content = new ContentDatabase();
            foreach (string manifest in Directory.GetFiles(rootDirectory, "pack.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                AddPack(content, Path.GetDirectoryName(manifest)!);
            }

            return content.Build();
        }

        /// <summary>Agrega un paquete a la base de contenido (sin llamar a Build).</summary>
        public static void AddPack(ContentDatabase content, string packDirectory)
        {
            string manifestPath = Path.Combine(packDirectory, "pack.json");
            string packId = (string?)JObject.Parse(File.ReadAllText(manifestPath))["id"]
                ?? throw new InvalidDataException($"{manifestPath} no tiene id.");

            string countryFile = Path.Combine(packDirectory, "country.json");
            if (File.Exists(countryFile))
            {
                content.AddCountryJson(packId, Relative(packDirectory, countryFile), File.ReadAllText(countryFile));
            }

            foreach (string file in JsonFiles(packDirectory, "seeds"))
            {
                content.AddSeedsJson(packId, Relative(packDirectory, file), File.ReadAllText(file));
            }

            foreach (string file in JsonFiles(packDirectory, "events"))
            {
                content.AddEventsJson(packId, Relative(packDirectory, file), File.ReadAllText(file));
            }
        }

        private static string[] JsonFiles(string packDirectory, string folder)
        {
            string dir = Path.Combine(packDirectory, folder);
            return Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
        }

        private static string Relative(string packDirectory, string file) =>
            Path.GetFileName(packDirectory) + "/" + file.Substring(packDirectory.Length).TrimStart(Path.DirectorySeparatorChar, '/').Replace('\\', '/');
    }
}
