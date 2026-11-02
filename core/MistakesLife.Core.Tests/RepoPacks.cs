using System;
using System.IO;
using MistakesLife.Core.Content;

namespace MistakesLife.Core.Tests
{
    /// <summary>Carga los paquetes reales del repositorio (carpeta packs/).</summary>
    internal static class RepoPacks
    {
        private static readonly Lazy<ContentDatabase> Loaded = new Lazy<ContentDatabase>(() => PackLoader.LoadAll(Directory));

        public static string Directory
        {
            get
            {
                string? dir = AppContext.BaseDirectory;
                while (dir != null && !System.IO.Directory.Exists(Path.Combine(dir, "packs")))
                {
                    dir = Path.GetDirectoryName(dir);
                }

                return Path.Combine(dir ?? throw new DirectoryNotFoundException("No se encontró la carpeta packs/."), "packs");
            }
        }

        public static ContentDatabase Content => Loaded.Value;
    }
}
