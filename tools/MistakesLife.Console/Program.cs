using System;
using System.IO;
using System.Text;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Saving;

namespace MistakesLife.ConsoleGame
{
    public static class Program
    {
        /// <summary>
        /// Uso: dotnet run --project tools/MistakesLife.Console [-- numero_de_partida] [--packs carpeta] [--cargar archivo]
        /// </summary>
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            ulong? seed = null;
            string? packs = null;
            string? load = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--packs" && i + 1 < args.Length)
                {
                    packs = args[++i];
                }
                else if (args[i] == "--cargar" && i + 1 < args.Length)
                {
                    load = args[++i];
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

            LifeSession session;
            if (load != null)
            {
                try
                {
                    session = LifeSession.Load(content, File.ReadAllText(load));
                }
                catch (Exception ex) when (ex is SaveFormatException || ex is IOException)
                {
                    Console.Error.WriteLine("No pude cargar la partida: " + ex.Message);
                    return 1;
                }
            }
            else
            {
                // Fuera de la simulación sí se puede usar el reloj: solo elige el número de partida.
                ulong gameSeed = seed ?? (ulong)DateTime.UtcNow.Ticks;
                session = new LifeSession(content, gameSeed);
            }

            new ConsoleGame(session, Console.In, Console.Out, load ?? ConsoleGame.DefaultSavePath).Run();
            return 0;
        }
    }
}
