using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Carga paquetes desde carpetas (pruebas y herramientas) o desde un <see cref="PackBundle"/> (Unity).
    /// Cada paquete tiene pack.json y, opcionalmente, country.json, career.json y las carpetas events/, news/, seeds/, scene/, apps/, mail/ y lang/
    /// con archivos .json que contienen una lista.
    /// Las dos formas pasan por el mismo camino, así una partida es igual en la consola y en el celular.
    /// </summary>
    public static class PackLoader
    {
        /// <summary>Carga todos los paquetes que encuentre bajo una carpeta raíz (por ejemplo packs/).</summary>
        public static ContentDatabase LoadAll(string rootDirectory) => LoadBundle(PackBundle.FromDirectory(rootDirectory));

        /// <summary>Carga todos los paquetes de un archivo de paquetes.</summary>
        public static ContentDatabase LoadBundle(PackBundle bundle)
        {
            var content = new ContentDatabase();
            foreach (string manifest in bundle.Files.Keys.Where(IsManifest))
            {
                AddPack(content, bundle, PackDirectoryOf(manifest));
            }

            return content.Build();
        }

        /// <summary>Agrega un paquete a la base de contenido (sin llamar a Build).</summary>
        public static void AddPack(ContentDatabase content, string packDirectory)
        {
            string directory = Path.GetFullPath(packDirectory);
            string parent = Path.GetDirectoryName(directory) ?? directory;
            var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories)
                .Select(path => new KeyValuePair<string, string>(path.Substring(parent.Length), File.ReadAllText(path)));
            AddPack(content, new PackBundle(files), Path.GetFileName(directory));
        }

        private static void AddPack(ContentDatabase content, PackBundle bundle, string packDirectory)
        {
            string prefix = packDirectory.Length == 0 ? string.Empty : packDirectory + "/";
            string manifestPath = prefix + "pack.json";
            string packId = (string?)JObject.Parse(bundle.Files[manifestPath])["id"]
                ?? throw new InvalidDataException($"{manifestPath} no tiene id.");

            if (bundle.Files.TryGetValue(prefix + "country.json", out string? country))
            {
                content.AddCountryJson(packId, Source(packDirectory, "country.json"), country);
            }

            if (bundle.Files.TryGetValue(prefix + "career.json", out string? career))
            {
                content.AddCareerJson(packId, Source(packDirectory, "career.json"), career);
            }

            foreach (string file in JsonFiles(bundle, prefix, "seeds"))
            {
                content.AddSeedsJson(packId, Source(packDirectory, file.Substring(prefix.Length)), bundle.Files[file]);
            }

            foreach (string file in JsonFiles(bundle, prefix, "scene"))
            {
                content.AddSceneJson(packId, Source(packDirectory, file.Substring(prefix.Length)), bundle.Files[file]);
            }

            foreach (string file in JsonFiles(bundle, prefix, "apps"))
            {
                content.AddAppsJson(packId, Source(packDirectory, file.Substring(prefix.Length)), bundle.Files[file]);
            }

            foreach (string file in JsonFiles(bundle, prefix, "mail"))
            {
                content.AddMailJson(packId, Source(packDirectory, file.Substring(prefix.Length)), bundle.Files[file]);
            }

            foreach (string file in JsonFiles(bundle, prefix, "news"))
            {
                content.AddNewsJson(packId, Source(packDirectory, file.Substring(prefix.Length)), bundle.Files[file]);
            }

            foreach (string file in JsonFiles(bundle, prefix, "events"))
            {
                content.AddEventsJson(packId, Source(packDirectory, file.Substring(prefix.Length)), bundle.Files[file]);
            }

            foreach (string file in JsonFiles(bundle, prefix, "lang"))
            {
                string language = Path.GetFileNameWithoutExtension(file);
                content.AddTextsJson(packId, Source(packDirectory, file.Substring(prefix.Length)), language, bundle.Files[file]);
            }
        }

        private static bool IsManifest(string path) => path == "pack.json" || path.EndsWith("/pack.json", StringComparison.Ordinal);

        private static string PackDirectoryOf(string manifest) =>
            manifest == "pack.json" ? string.Empty : manifest.Substring(0, manifest.Length - "/pack.json".Length);

        private static IEnumerable<string> JsonFiles(PackBundle bundle, string prefix, string folder) =>
            bundle.Files.Keys.Where(k => k.StartsWith(prefix + folder + "/", StringComparison.Ordinal) && k.EndsWith(".json", StringComparison.Ordinal));

        /// <summary>Nombre corto del archivo para los mensajes de error: carpeta del paquete + ruta interna.</summary>
        private static string Source(string packDirectory, string relativeFile) =>
            (packDirectory.Length == 0 ? string.Empty : packDirectory.Substring(packDirectory.LastIndexOf('/') + 1) + "/") + relativeFile;
    }
}
