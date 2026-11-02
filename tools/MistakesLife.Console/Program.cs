using System;
using System.Text;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;

namespace MistakesLife.ConsoleGame
{
    public static class Program
    {
        /// <summary>
        /// Uso: dotnet run --project tools/MistakesLife.Console [-- numero_de_partida] [--packs carpeta]
        /// </summary>
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            ulong? seed = null;
            string? packs = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--packs" && i + 1 < args.Length)
                {
                    packs = args[++i];
                }
                else if (ulong.TryParse(args[i], out ulong parsed))
                {
                    seed = parsed;
                }
            }

            packs ??= ConsoleGame.FindPacks(Environment.CurrentDirectory) ?? ConsoleGame.FindPacks(AppContext.BaseDirectory);
            if (packs == null)
            {
                Console.Error.WriteLine("No encontré la carpeta packs/. Ejecuta desde la carpeta del repositorio o usa --packs.");
                return 1;
            }

            ContentDatabase content;
            try
            {
                content = ConsoleGame.LoadContent(packs);
            }
            catch (ContentLoadException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            // Fuera de la simulación sí se puede usar el reloj: solo elige el número de partida.
            ulong gameSeed = seed ?? (ulong)DateTime.UtcNow.Ticks;
            new ConsoleGame(new LifeSession(content, gameSeed), Console.In, Console.Out).Run();
            return 0;
        }
    }
}
