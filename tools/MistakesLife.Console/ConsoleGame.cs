using System;
using System.IO;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.ConsoleGame
{
    /// <summary>
    /// El juego en texto. Lee y escribe por TextReader/TextWriter para poder
    /// probarlo con entradas guardadas.
    /// </summary>
    public sealed class ConsoleGame
    {
        /// <summary>La fase 1 termina a esta edad.</summary>
        public const int FinalAge = 25;

        private readonly LifeSession _session;
        private readonly TextReader _input;
        private readonly TextWriter _output;

        public ConsoleGame(LifeSession session, TextReader input, TextWriter output)
        {
            _session = session;
            _input = input;
            _output = output;
        }

        public void Run()
        {
            PrintBirth();
            while (true)
            {
                PrintJournal();
                if (!_session.World.Player.IsAlive || _session.PlayerAgeInYears >= FinalAge)
                {
                    PrintEnd();
                    return;
                }

                if (_session.Pending != null)
                {
                    if (!AskEvent(_session.Pending))
                    {
                        return;
                    }

                    continue;
                }

                _output.WriteLine();
                _output.WriteLine($"[{DateLabel()}] Enter: seguir hasta que pase algo | s: una semana | a: un año | e: estado | d: diario | q: salir");
                _output.Write("> ");
                string? command = _input.ReadLine();
                if (command == null)
                {
                    return;
                }

                switch (command.Trim().ToLowerInvariant())
                {
                    case "":
                        _session.Advance(WeeksLeft());
                        break;
                    case "s":
                        _session.Advance(1);
                        break;
                    case "a":
                        _session.Advance(Math.Min(GameDate.WeeksPerYear, WeeksLeft()));
                        break;
                    case "e":
                        PrintStatus();
                        break;
                    case "d":
                        PrintFullJournal();
                        break;
                    case "q":
                        _output.WriteLine("Hasta pronto.");
                        return;
                    default:
                        _output.WriteLine("No entendí. Usa Enter, s, a, e, d o q.");
                        break;
                }
            }
        }

        /// <summary>Semanas que faltan para el final de la fase 1 (nunca menos de 1).</summary>
        private int WeeksLeft() =>
            Math.Max(1, (FinalAge * GameDate.WeeksPerYear) - _session.World.Player.AgeInWeeks(_session.World.Date));

        private bool AskEvent(PendingEvent pending)
        {
            EventDefinition definition = _session.Content.GetEvent(pending.EventId);
            _output.WriteLine();
            _output.WriteLine($"=== {DateLabel()} ===");
            _output.WriteLine(_session.Text(definition.ResolvedTextKey));
            for (int i = 0; i < pending.AvailableOptions.Count; i++)
            {
                EventOptionDefinition option = definition.Options[pending.AvailableOptions[i]];
                _output.WriteLine($"  {i + 1}. {_session.Text(option.TextKey)}");
            }

            while (true)
            {
                _output.Write("Elige una opción: ");
                string? answer = _input.ReadLine();
                if (answer == null)
                {
                    return false;
                }

                if (int.TryParse(answer.Trim(), out int number) && number >= 1 && number <= pending.AvailableOptions.Count)
                {
                    _session.Choose(pending.AvailableOptions[number - 1]);
                    return true;
                }

                _output.WriteLine($"Escribe un número del 1 al {pending.AvailableOptions.Count}.");
            }
        }

        private void PrintBirth()
        {
            Character player = _session.World.Player;
            _output.WriteLine("MistakesLife, fase 1 (vida en texto)");
            _output.WriteLine($"Naciste: {player.FirstName} {player.LastName}.");
            if (player.Residence != null)
            {
                _output.WriteLine($"Lugar: {_session.Text(player.Residence.CommuneId)}, {_session.Text(player.Residence.CityId)}, {_session.Text(player.Residence.CountryId)}.");
            }

            PrintParent("Tu mamá", player.MotherId);
            PrintParent("Tu papá", player.FatherId);
            _output.WriteLine($"Partida número {_session.World.Seed} (con este número puedes repetir esta misma vida).");
        }

        private void PrintParent(string label, int? id)
        {
            if (id.HasValue)
            {
                Character parent = _session.World.GetCharacter(id.Value);
                _output.WriteLine($"{label}: {parent.FirstName} {parent.LastName}, {parent.AgeInYears(_session.World.Date)} años.");
            }
        }

        private void PrintStatus()
        {
            Character player = _session.World.Player;
            _output.WriteLine($"{player.FirstName}, {_session.PlayerAgeInYears} años. Salud {player.Stats.Health}, felicidad {player.Stats.Happiness}, inteligencia {player.Stats.Smarts}.");
            if (player.EducationStageId != null)
            {
                _output.WriteLine($"Etapa: {_session.Text(player.EducationStageId)}.");
            }
        }

        private void PrintJournal()
        {
            foreach (JournalEntry entry in _session.TakeNewJournalEntries())
            {
                _output.WriteLine($"  Diario ({AgeLabel(entry.Date)}): {_session.Text(entry.TextKey)}");
            }
        }

        private void PrintFullJournal()
        {
            foreach (JournalEntry entry in _session.World.Journal.Where(e => e.CharacterId == _session.World.PlayerId))
            {
                _output.WriteLine($"  {AgeLabel(entry.Date)}: {_session.Text(entry.TextKey)}");
            }
        }

        private void PrintEnd()
        {
            Character player = _session.World.Player;
            _output.WriteLine();
            _output.WriteLine(player.IsAlive
                ? $"Llegaste a los {FinalAge} años. Aquí termina la fase 1 del juego."
                : "Tu vida terminó.");
            _output.WriteLine($"Eventos vividos: {_session.World.EventHistory.Count(r => r.CharacterId == player.Id)}. Semillas que despertaron: {_session.World.Seeds.All.Count(s => s.OwnerId == player.Id && s.AwakenedDate.HasValue)}.");
            PrintStatus();
        }

        private string DateLabel() => $"{AgeLabel(_session.World.Date)}, {_session.World.Date}";

        private string AgeLabel(GameDate date)
        {
            int weeks = _session.World.Player.AgeInWeeks(date);
            int years = weeks / GameDate.WeeksPerYear;
            return years == 0 ? $"{weeks} semanas" : years == 1 ? "1 año" : $"{years} años";
        }

        /// <summary>Busca la carpeta packs/ subiendo desde una carpeta.</summary>
        public static string? FindPacks(string start)
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

        public static ContentDatabase LoadContent(string packsDirectory) => PackLoader.LoadAll(packsDirectory);
    }
}
