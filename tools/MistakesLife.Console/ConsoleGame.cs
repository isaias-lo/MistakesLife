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
        /// <summary>
        /// Con Enter el tiempo corre hasta que pasa algo, pero nunca más de estos
        /// años seguidos, para que el jugador vea pasar su vida.
        /// </summary>
        public const int MaxYearsPerStep = 5;

        private readonly LifeSession _session;
        private readonly TextReader _input;
        private readonly TextWriter _output;
        private readonly string _savePath;

        /// <summary>Archivo donde se guarda la partida con "g", si no se indica otro.</summary>
        public const string DefaultSavePath = "partida.json";

        public ConsoleGame(LifeSession session, TextReader input, TextWriter output, string savePath = DefaultSavePath)
        {
            _session = session;
            _input = input;
            _output = output;
            _savePath = savePath;
        }

        public void Run()
        {
            PrintBirth();
            while (true)
            {
                PrintJournal();
                if (!_session.World.Player.IsAlive)
                {
                    PrintEnd();
                    if (!AskHeir())
                    {
                        return;
                    }

                    continue;
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
                _output.WriteLine($"[{DateLabel()}] Enter: seguir hasta que pase algo | s: una semana | a: un año | e: estado | d: diario | g: guardar | q: salir");
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
                        _session.Advance(GameDate.WeeksPerYear);
                        break;
                    case "e":
                        PrintStatus();
                        break;
                    case "d":
                        PrintFullJournal();
                        break;
                    case "g":
                        File.WriteAllText(_savePath, _session.SaveToText());
                        _output.WriteLine($"Partida guardada en {Path.GetFullPath(_savePath)}. Para seguirla: --cargar {_savePath}");
                        break;
                    case "q":
                        _output.WriteLine("Hasta pronto.");
                        return;
                    default:
                        _output.WriteLine("No entendí. Usa Enter, s, a, e, d, g o q.");
                        break;
                }
            }
        }

        /// <summary>Semanas que corre el tiempo con Enter: hasta que pase algo, máximo unos años.</summary>
        private static int WeeksLeft() => MaxYearsPerStep * GameDate.WeeksPerYear;

        /// <summary>
        /// Legado: si el jugador murió con hijos vivos, elige con cuál seguir.
        /// Devuelve false si la partida terminó.
        /// </summary>
        private bool AskHeir()
        {
            var heirs = _session.Heirs;
            if (heirs.Count == 0)
            {
                _output.WriteLine("No dejaste hijos que continúen tu historia. Aquí termina la partida, pero lo que hiciste queda en el mundo.");
                return false;
            }

            _output.WriteLine();
            _output.WriteLine("Tu historia puede seguir con uno de tus hijos:");
            for (int i = 0; i < heirs.Count; i++)
            {
                Character heir = heirs[i];
                _output.WriteLine($"  {i + 1}. {heir.FirstName} {heir.LastName}, {heir.AgeInYears(_session.World.Date)} años.");
            }

            while (true)
            {
                _output.Write("¿Con quién sigues? (número, o q para terminar): ");
                string? answer = _input.ReadLine();
                if (answer == null || answer.Trim().ToLowerInvariant() == "q")
                {
                    return false;
                }

                if (int.TryParse(answer.Trim(), out int number) && number >= 1 && number <= heirs.Count)
                {
                    _session.ContinueWith(heirs[number - 1].Id);
                    Character player = _session.World.Player;
                    _output.WriteLine();
                    _output.WriteLine($"=== Generación {_session.World.Generation} ===");
                    _output.WriteLine($"Ahora eres {player.FirstName} {player.LastName}, {_session.PlayerAgeInYears} años.");
                    return true;
                }

                _output.WriteLine($"Escribe un número del 1 al {heirs.Count}, o q.");
            }
        }

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
            _output.WriteLine("MistakesLife (vida en texto)");
            if (player.AgeInWeeks(_session.World.Date) > 0)
            {
                // Partida cargada: no se cuenta el nacimiento de nuevo.
                _output.WriteLine($"Sigues tu vida: {player.FirstName} {player.LastName}, {_session.PlayerAgeInYears} años.");
                return;
            }

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
            var economy = player.Residence == null ? null : _session.World.EconomyOf(player.Residence.CountryId);
            if (economy != null)
            {
                // Formato chileno: punto para miles y coma para decimales.
                var culture = new System.Globalization.NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalSeparator = "," };
                _output.WriteLine($"Dinero: ${player.Money.ToString("N0", culture)}. Inflación de los últimos 12 meses: {(economy.InflationLast12Months * 100).ToString("0.0", culture)} %.");
                if (player.LastExpenses.Count > 0)
                {
                    _output.WriteLine("Gastos del último mes:");
                    foreach (var line in player.LastExpenses)
                    {
                        _output.WriteLine($"  {_session.Text(line.Id)}: -${line.Amount.ToString("N0", culture)}");
                    }
                }

                foreach (var debt in player.Debts)
                {
                    _output.WriteLine($"Deuda: {_session.Text(debt.TypeId)}, ${debt.Balance.ToString("N0", culture)} ({debt.RemainingInstallments} cuotas){(debt.MonthsLate > 0 ? $", {debt.MonthsLate} meses sin pagar" : string.Empty)}.");
                }

                if (player.Delinquent)
                {
                    _output.WriteLine("Estás en el registro de morosos.");
                }

                if (player.Pension.HasValue || player.LastPensionPayment > 0)
                {
                    _output.WriteLine($"Pensión del último mes (propia y del Estado): ${player.LastPensionPayment.ToString("N0", culture)}.");
                }
                else if (player.PensionFund > 0)
                {
                    _output.WriteLine($"Fondo de pensión ahorrado: ${MistakesLife.Core.Economy.PayrollCalculator.ToToday(player.PensionFund, economy.PriceIndex).ToString("N0", culture)}.");
                }

                if (player.Job != null)
                {
                    string kind = player.Job.Kind == MistakesLife.Core.Economy.JobKind.Freelance ? "a honorarios" : "con contrato";
                    _output.WriteLine($"Trabajas {kind}. Sueldo bruto: ${player.Job.Salary.ToString("N0", culture)}.");
                    if (player.LastPayslip != null)
                    {
                        _output.WriteLine("Última liquidación:");
                        foreach (var line in player.LastPayslip.Lines)
                        {
                            _output.WriteLine($"  {_session.Text(line.Id)}: -${line.Amount.ToString("N0", culture)}");
                        }

                        _output.WriteLine($"  Líquido: ${player.LastPayslip.Net.ToString("N0", culture)}");
                    }
                }
            }
            if (player.EducationStageId != null)
            {
                _output.WriteLine($"Etapa: {_session.Text(player.EducationStageId)}.");
            }

            if (player.Studies != null)
            {
                _output.WriteLine($"Estudias: {_session.Text(player.Studies.LevelId)} Semestres aprobados: {player.Studies.CompletedSemesters} de {player.Studies.RequiredSemesters}.");
            }

            foreach (string degree in player.Degrees)
            {
                _output.WriteLine($"Título: {_session.Text(degree + ".titulo")}.");
            }

            if (player.PartnerId.HasValue)
            {
                Character partner = _session.World.GetCharacter(player.PartnerId.Value);
                _output.WriteLine($"Pareja: {partner.FirstName} {partner.LastName}, {partner.AgeInYears(_session.World.Date)} años.");
            }

            foreach (Character child in _session.World.ChildrenOf(player.Id))
            {
                string state = child.IsAlive ? $"{child.AgeInYears(_session.World.Date)} años" : "fallecido";
                _output.WriteLine($"{(child.Sex == Sex.Female ? "Hija" : "Hijo")}: {child.FirstName} {child.LastName}, {state}.");
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
            _output.WriteLine($"Tu vida terminó a los {player.AgeInYears(_session.World.Date)} años.");
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
