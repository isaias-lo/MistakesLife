using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Todos los paquetes juntos en un solo archivo: un mapa "ruta relativa → contenido".
    /// Existe porque en Android los archivos del juego van comprimidos dentro de la app
    /// y no se pueden recorrer como carpetas; un solo archivo sí se puede leer.
    /// Las herramientas lo arman desde la carpeta packs/ y Unity lo lee entero.
    /// </summary>
    public sealed class PackBundle
    {
        private readonly SortedDictionary<string, string> _files;

        public PackBundle(IEnumerable<KeyValuePair<string, string>> files)
        {
            _files = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                _files[Normalize(file.Key)] = file.Value;
            }
        }

        /// <summary>Archivos del paquete, con rutas relativas a packs/ separadas por "/" y en orden fijo.</summary>
        public IReadOnlyDictionary<string, string> Files => _files;

        /// <summary>Lee todos los .json bajo una carpeta (por ejemplo packs/).</summary>
        public static PackBundle FromDirectory(string rootDirectory)
        {
            string root = Path.GetFullPath(rootDirectory);
            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                .Select(path => new KeyValuePair<string, string>(path.Substring(root.Length), File.ReadAllText(path)));
            return new PackBundle(files);
        }

        /// <summary>Lee un paquete armado con <see cref="ToJson"/>.</summary>
        public static PackBundle FromJson(string json)
        {
            Dictionary<string, string>? files;
            try
            {
                files = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("El archivo de paquetes no es un JSON válido: " + ex.Message, ex);
            }

            return new PackBundle(files ?? throw new InvalidDataException("El archivo de paquetes está vacío."));
        }

        /// <summary>El paquete como un solo JSON (siempre igual para los mismos archivos).</summary>
        public string ToJson() => JsonConvert.SerializeObject(_files, Formatting.None);

        private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
    }
}
