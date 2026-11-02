using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MistakesLife.Core.Analysis;
using MistakesLife.Core.Content;

namespace MistakesLife.Simulator
{
    public static class Program
    {
        /// <summary>
        /// Uso: dotnet run --project tools/MistakesLife.Simulator -- [vidas] [edad_final] [--out informe.md] [--packs carpeta]
        /// Por defecto: 1000 vidas hasta los 25 años, informe en la terminal.
        /// </summary>
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            int lives = 1000;
            int finalAge = 25;
            string? output = null;
            string? packs = null;
            int position = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--out" && i + 1 < args.Length)
                {
                    output = args[++i];
                }
                else if (args[i] == "--packs" && i + 1 < args.Length)
                {
                    packs = args[++i];
                }
                else if (int.TryParse(args[i], out int number))
                {
                    if (position++ == 0)
                    {
                        lives = number;
                    }
                    else
                    {
                        finalAge = number;
                    }
                }
            }

            packs ??= FindPacks(Environment.CurrentDirectory) ?? FindPacks(AppContext.BaseDirectory);
            if (packs == null)
            {
                Console.Error.WriteLine("No encontré la carpeta packs/. Usa --packs.");
                return 1;
            }

            ContentDatabase content;
            try
            {
                content = PackLoader.LoadAll(packs);
            }
            catch (ContentLoadException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            var watch = Stopwatch.StartNew();
            SimulationReport report = new LifeSimulator(content).Run(lives, finalAge);
            string markdown = report.ToMarkdown() + $"\n_Tiempo: {watch.Elapsed.TotalSeconds:0.0} s._\n";
            if (output != null)
            {
                File.WriteAllText(output, markdown);
                Console.WriteLine($"Informe escrito en {output}");
            }
            else
            {
                Console.WriteLine(markdown);
            }

            return 0;
        }

        private static string? FindPacks(string start)
        {
            string? dir = start;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "packs");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }
    }
}
