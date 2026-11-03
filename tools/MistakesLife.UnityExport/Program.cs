using System;
using System.IO;
using MistakesLife.Core.Content;

namespace MistakesLife.UnityExport
{
    public static class Program
    {
        /// <summary>
        /// Uso (desde la carpeta del repositorio): dotnet run --project tools/MistakesLife.UnityExport
        /// Hay que correrlo cada vez que cambie el núcleo o los paquetes.
        /// </summary>
        public static int Main()
        {
            string? root = FindRepoRoot(Environment.CurrentDirectory) ?? FindRepoRoot(AppContext.BaseDirectory);
            if (root == null)
            {
                Console.Error.WriteLine("No encontré las carpetas packs/ y unity/. Ejecuta desde la carpeta del repositorio.");
                return 1;
            }

            try
            {
                UnityExporter.Export(Path.Combine(root, "packs"), Path.Combine(root, "unity"));
            }
            catch (ContentLoadException ex)
            {
                Console.Error.WriteLine("Los paquetes tienen errores; no se exportó nada.");
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            Console.WriteLine("Listo: núcleo y paquetes copiados en unity/. Vuelve a Unity para que los cargue.");
            return 0;
        }

        private static string? FindRepoRoot(string start)
        {
            string? dir = start;
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir, "packs")) && Directory.Exists(Path.Combine(dir, "unity")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }
    }
}
