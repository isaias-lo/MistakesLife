using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
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
        private int _newsRead;

        /// <summary>Archivo donde se guarda la partida con "g", si no se indica otro.</summary>
        public const string DefaultSavePath = "partida.json";

        public ConsoleGame(LifeSession session, TextReader input, TextWriter output, string savePath = DefaultSavePath)
        {
            _session = session;
            _session.AsksForRoutine = true;
            _input = input;
            _output = output;
            _savePath = savePath;
        }

        public void Run()
        {
            PrintBirth();
            int chapterYear = _session.World.Date.Year;
            while (true)
            {
                PrintJournal();

                // Al pasar de año, el año que termina se cierra como un capítulo del diario.
                int year = _session.World.Date.Year;
                if (year > chapterYear)
                {
                    PrintChapter(new LifePresenter(_session).Chapter(year - 1));
                }

                chapterYear = year;
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

                if (_session.World.Player.Routine is { Confirmed: false })
                {
                    _output.WriteLine();
                    _output.WriteLine(_session.Text(RoutineRules.IsSchoolYear(_session.World.Player.Routine) ? "ui.horario.nuevo_ano" : "ui.horario.nuevo"));
                    if (!AskWishes())
                    {
                        return;
                    }

                    if (!AskRoutine())
                    {
                        return;
                    }

                    continue;
                }

                _output.WriteLine();
                _output.WriteLine($"[{DateLabel()}] Enter: seguir hasta lo importante | s: una semana | a: un año | e: estado | h: horario | l: lo que quiero | r: tus reglas | k: tu curso | n: notas | d: diario | c: correo | g: guardar | v: volver al pasado | q: salir");
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
                    case "h":
                        if (_session.World.Player.Routine == null)
                        {
                            _output.WriteLine("Todavía no tienes horario: lo armas a los 6 años.");
                        }
                        else if (!AskRoutine())
                        {
                            return;
                        }

                        break;
                    case "l":
                        if (!AskWishes())
                        {
                            return;
                        }

                        break;
                    case "r":
                        if (!AskRules())
                        {
                            return;
                        }

                        break;
                    case "k":
                        PrintSchoolClass();
                        break;
                    case "n":
                        PrintReportCard();
                        break;
                    case "d":
                        PrintFullJournal();
                        break;
                    case "c":
                        PrintMail();
                        break;
                    case "g":
                        File.WriteAllText(_savePath, _session.SaveToText());
                        _output.WriteLine($"Partida guardada en {Path.GetFullPath(_savePath)}. Para seguirla: --cargar {_savePath}");
                        break;
                    case "v":
                        AskGoBack();
                        break;
                    case "q":
                        _output.WriteLine("Hasta pronto.");
                        return;
                    default:
                        _output.WriteLine("No entendí. Usa Enter, s, a, e, h, l, r, k, n, d, c, g, v o q.");
                        break;
                }
            }
        }

        /// <summary>
        /// Muestra el horario y deja cambiar bloques ("ma t" = martes tarde) hasta que el
        /// jugador lo deja así con Enter. Devuelve false si se acabó la entrada.
        /// </summary>
        private bool AskRoutine()
        {
            string[] days = { "lu", "ma", "mi", "ju", "vi", "sa", "do" };
            while (true)
            {
                Routine routine = _session.World.Player.Routine!;
                _output.WriteLine(_session.Text("ui.horario.titulo") + ":");
                for (int day = 0; day < Routine.Days; day++)
                {
                    string afternoon = SlotText(routine.Get(day, RoutineBlock.Afternoon));
                    string night = SlotText(routine.Get(day, RoutineBlock.Night));
                    _output.WriteLine($"  {days[day]}  {_session.Text("ui.horario.dia." + day),-10} {_session.Text("ui.horario.tarde")}: {afternoon,-26} {_session.Text("ui.horario.noche")}: {night}");
                }

                _output.WriteLine($"  Si sigues así: ánimo {Trend("animo")}, mente {Trend("mente")}, salud {Trend("salud")}.");
                _output.Write("Enter: así queda | <día> <t|n> para cambiar un bloque (ej. \"ma t\"): ");
                string? line = _input.ReadLine();
                if (line == null)
                {
                    return false;
                }

                string[] parts = line.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    _session.ConfirmRoutine();
                    return true;
                }

                int dayIndex = Array.IndexOf(days, parts[0]);
                if (parts.Length != 2 || dayIndex < 0 || (parts[1] != "t" && parts[1] != "n"))
                {
                    _output.WriteLine("No entendí. Escribe el día (lu, ma, mi, ju, vi, sa, do) y t (tarde) o n (noche).");
                    continue;
                }

                RoutineBlock block = parts[1] == "t" ? RoutineBlock.Afternoon : RoutineBlock.Night;
                var options = RoutineRules.Options(_session.World, _session.Content, _session.World.Player, block);
                _output.WriteLine("  0. " + _session.Text("ui.horario.libre"));
                for (int i = 0; i < options.Count; i++)
                {
                    _output.WriteLine($"  {i + 1}. {_session.Text(options[i].Id)}");
                }

                _output.Write("¿Qué pones? ");
                string? pick = _input.ReadLine();
                if (pick == null)
                {
                    return false;
                }

                if (int.TryParse(pick.Trim(), out int n) && n >= 0 && n <= options.Count)
                {
                    _session.SetRoutine(dayIndex, block, n == 0 ? null : options[n - 1].Id);
                }
            }
        }

        private string SlotText(string? activityId) => _session.Text(activityId ?? "ui.horario.libre");

        private string Trend(string key)
        {
            var world = _session.World;
            int total = RoutineRules.Total(world, _session.Content, world.Player, world.Player.Routine!, key);
            int direction = key == "animo" ? RoutineRules.MoodTarget(total) - world.Player.Stats.Happiness
                : key == "mente" ? RoutineRules.MindTarget(total) - world.Player.Stats.Smarts
                : RoutineRules.HealthStep(total);
            return _session.Text(direction > 0 ? "ui.horario.sube" : direction < 0 ? "ui.horario.baja" : "ui.horario.igual");
        }

        private bool CanGoBack() => _session.TimeMachine.Snapshots.Any(_session.TimeMachine.CanGoBackTo);

        /// <summary>
        /// Volver al pasado: muestra las fotos del mundo a las que se puede volver
        /// y vuelve a la elegida. Devuelve true si volvió.
        /// </summary>
        private bool AskGoBack()
        {
            var machine = _session.TimeMachine;
            var snapshots = machine.Snapshots.Where(machine.CanGoBackTo).ToList();
            if (snapshots.Count == 0)
            {
                _output.WriteLine(machine.Charges == 0
                    ? "No te quedan cargas para volver al pasado."
                    : "No hay un momento al que puedas volver (cada etapa de la vida se vuelve a vivir una sola vez).");
                return false;
            }

            _output.WriteLine($"Te quedan {machine.Charges} cargas para volver al pasado. Cada etapa de la vida se vuelve a vivir una sola vez.");
            for (int i = 0; i < snapshots.Count; i++)
            {
                WorldSnapshot snapshot = snapshots[i];
                string moment = snapshot.Kind == SnapshotKind.StageStart
                    ? $"inicio de la etapa: {_session.Text(snapshot.StageId)}"
                    : $"antes de decidir: {_session.Text(_session.Content.GetEvent(snapshot.EventId!).ResolvedTextKey)}";
                _output.WriteLine($"  {i + 1}. A tus {snapshot.PlayerAgeInYears} años, {moment}");
            }

            while (true)
            {
                _output.Write("¿A cuál vuelves? (número, o Enter para no volver): ");
                string? answer = _input.ReadLine();
                if (answer == null || answer.Trim().Length == 0)
                {
                    return false;
                }

                if (int.TryParse(answer.Trim(), out int number) && number >= 1 && number <= snapshots.Count)
                {
                    _session.GoBack(snapshots[number - 1].Id);
                    _output.WriteLine();
                    _output.WriteLine($"=== Vuelves al pasado: {DateLabel()} ===");
                    return true;
                }

                _output.WriteLine($"Escribe un número del 1 al {snapshots.Count}, o Enter.");
            }
        }

        /// <summary>Semanas que corre el tiempo con Enter: hasta que pase algo, máximo unos años.</summary>
        /// <summary>
        /// Lo que quiero: muestra lo de esta etapa y deja marcar con destacador lo que persigue.
        /// Devuelve false si se acabó la entrada.
        /// </summary>
        private bool AskWishes()
        {
            var presenter = new LifePresenter(_session);
            while (true)
            {
                IReadOnlyList<WishView> wishes = presenter.Wishes();
                _output.WriteLine();
                _output.WriteLine(_session.Text("ui.deseo.titulo") + ":");
                if (wishes.Count == 0)
                {
                    _output.WriteLine("  (por ahora nada en especial)");
                    return true;
                }

                for (int i = 0; i < wishes.Count; i++)
                {
                    WishView wish = wishes[i];
                    string mark = wish.Highlighted ? "[destacado] " : string.Empty;
                    string state = wish.State == null ? string.Empty : $" ({wish.State})";
                    _output.WriteLine($"  {i + 1}. {mark}{wish.Text}{state}");
                }

                _output.Write("Escribe el número de lo que persigues para marcarlo o desmarcarlo (Enter para seguir): ");
                string? answer = _input.ReadLine();
                if (answer == null)
                {
                    return false;
                }

                if (answer.Trim().Length == 0)
                {
                    return true;
                }

                if (int.TryParse(answer.Trim(), out int number) && number >= 1 && number <= wishes.Count && wishes[number - 1].Open)
                {
                    presenter.HighlightWish(wishes[number - 1].Index, !wishes[number - 1].Highlighted);
                }
                else
                {
                    _output.WriteLine("Ese no se puede marcar.");
                }
            }
        }

        /// <summary>Tus reglas: las muestra y deja borrarlas. Devuelve false si se acabó la entrada.</summary>
        private bool AskRules()
        {
            var presenter = new LifePresenter(_session);
            while (true)
            {
                IReadOnlyList<RuleView> rules = presenter.Rules();
                _output.WriteLine();
                if (rules.Count == 0)
                {
                    _output.WriteLine("No tienes reglas. En un momento, elige con ! al final (por ejemplo 1!) para hacerlo siempre.");
                    return true;
                }

                _output.WriteLine("Tus reglas:");
                for (int i = 0; i < rules.Count; i++)
                {
                    _output.WriteLine($"  {i + 1}. {rules[i].Situation} → {rules[i].Choice} (se usó sola {rules[i].TimesUsed} veces)");
                }

                _output.Write("Escribe el número de una regla para borrarla (Enter para seguir): ");
                string? answer = _input.ReadLine();
                if (answer == null)
                {
                    return false;
                }

                if (answer.Trim().Length == 0)
                {
                    return true;
                }

                if (int.TryParse(answer.Trim(), out int number) && number >= 1 && number <= rules.Count)
                {
                    presenter.RemoveRule(rules[number - 1].Key);
                }
            }
        }

        /// <summary>Muestra tu libreta: las notas de cada ramo y el promedio general.</summary>
        private void PrintReportCard()
        {
            var presenter = new LifePresenter(_session);
            ReportCardView? card = presenter.ReportCard();
            if (card != null && card.Semester && card.General == null)
            {
                // Primer semestre: todavía sin notas, pero ya con sus ramos.
                _output.WriteLine(presenter.Text("ui.notas.ramos_semestre") + ":");
                foreach (SubjectGradesView subject in card.Subjects)
                {
                    _output.WriteLine($"  {subject.Name}");
                }

                return;
            }

            if (card == null || card.General == null)
            {
                _output.WriteLine(presenter.Text("ui.notas.sin_notas"));
                return;
            }

            _output.WriteLine(presenter.Text(card.Finals ? (card.Semester ? "ui.notas.fin_semestre" : "ui.notas.fin_ano") : "ui.notas.titulo") + ":");
            foreach (SubjectGradesView subject in card.Subjects)
            {
                string marks = subject.Marks.Count == 0 ? string.Empty : "  " + string.Join("  ", subject.Marks) + "  →";
                _output.WriteLine($"  {subject.Name}:{marks} {subject.Average}{(subject.Failing ? " (rojo)" : string.Empty)}");
            }

            _output.WriteLine(presenter.Text("ui.notas.promedio", card.General));
        }

        /// <summary>Muestra tu curso: el nombre, tu profe jefe y tus compañeros, del que más quieres al que menos.</summary>
        private void PrintSchoolClass()
        {
            var presenter = new LifePresenter(_session);
            SchoolClassView? cls = presenter.SchoolClass();
            if (cls == null)
            {
                _output.WriteLine("No vas a un curso ahora.");
                return;
            }

            _output.WriteLine(presenter.Text(cls.Section ? "ui.seccion.titulo" : "ui.curso.titulo", cls.Name, cls.Students));
            if (cls.Teacher != null)
            {
                _output.WriteLine(presenter.Text(cls.Section ? "ui.seccion.profe" : "ui.curso.profe_jefe", cls.Teacher));
            }

            foreach (ClassmateView mate in cls.Classmates)
            {
                _output.WriteLine($"  - {mate.Name} (cariño {mate.Affection}){(mate.Bond == null ? string.Empty : " · " + mate.Bond)}");
                if (mate.Memory != null)
                {
                    _output.WriteLine($"      {mate.Memory}");
                }
            }

            StudyPlanView? plan = presenter.StudyPlan();
            if (plan == null)
            {
                return;
            }

            _output.WriteLine(presenter.Text("ui.malla.titulo") + ": " + plan.Career + " · " + plan.Progress);
            foreach (PlanSemesterView semester in plan.Semesters)
            {
                string state = semester.State switch
                {
                    PlanSemesterState.Approved => " [" + presenter.Text("ui.malla.aprobado") + "]",
                    PlanSemesterState.Current => " [" + presenter.Text("ui.malla.cursando") + "]",
                    PlanSemesterState.Retaking => " [" + presenter.Text("ui.malla.repites") + "]",
                    _ => string.Empty,
                };
                _output.WriteLine($"  {semester.Name}{state}: {string.Join(", ", semester.Courses)}");
                if (semester.FailedCourses.Count > 0)
                {
                    _output.WriteLine("    " + presenter.Text("ui.malla.rojos", string.Join(", ", semester.FailedCourses)));
                }
            }
        }

        private static string WithPerson(string text, Character? person) =>
            person == null ? text : text.Replace(LifePresenter.PersonMark, person.FirstName);

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
                if (CanGoBack())
                {
                    _output.Write("¿Quieres volver al pasado? (v para volver, Enter para terminar): ");
                    string? reply = _input.ReadLine();
                    return reply != null && reply.Trim().ToLowerInvariant() == "v" && AskGoBack();
                }

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
                _output.Write(CanGoBack()
                    ? "¿Con quién sigues? (número, v para volver al pasado, o q para terminar): "
                    : "¿Con quién sigues? (número, o q para terminar): ");
                string? answer = _input.ReadLine();
                if (answer == null || answer.Trim().ToLowerInvariant() == "q")
                {
                    return false;
                }

                if (answer.Trim().ToLowerInvariant() == "v" && CanGoBack())
                {
                    if (AskGoBack())
                    {
                        return true;
                    }

                    continue;
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
            Character? person = EventSystem.PersonOf(_session.World, pending);
            var presenter = new LifePresenter(_session);
            EventView? view = presenter.Snapshot().PendingEvent;
            string? moment = view == null ? null : presenter.MomentLine(view);
            if (moment != null)
            {
                _output.WriteLine($"({moment})");
            }

            SeedOriginView? origin = view?.Origin;
            if (origin != null)
            {
                _output.WriteLine($"  [{origin.Title} — {string.Join(" · ", new[] { origin.When, origin.Place, origin.Person }.Where(x => x != null))}]");
                _output.WriteLine($"  [{origin.Moment}]");
                if (origin.Choice != null)
                {
                    _output.WriteLine($"  [{origin.Choice}]");
                }
            }

            _output.WriteLine(view?.Text ?? WithPerson(_session.Text(definition.TextKeyFor(_session.World.TimesHappened(pending.EventId, pending.CharacterId))), person));
            for (int i = 0; i < pending.AvailableOptions.Count; i++)
            {
                EventOptionDefinition option = definition.Options[pending.AvailableOptions[i]];
                string intuition = _session.TimeMachine.PreviousChoice(pending) == pending.AvailableOptions[i] ? " (la otra vez elegiste esto)" : string.Empty;
                string optionText = view != null && i < view.Options.Count ? view.Options[i] : WithPerson(_session.Text(option.TextKey), person);
                _output.WriteLine($"  {i + 1}. {optionText}{intuition}");
            }

            if (view != null && view.AllowsRule)
            {
                _output.WriteLine("  (Con ! al final, por ejemplo 1!, lo haces siempre: si vuelve a pasar, no te pregunto.)");
            }

            if (origin?.GoBackLabel != null)
            {
                _output.WriteLine($"  (o escribe o: {origin.GoBackLabel})");
            }

            while (true)
            {
                _output.Write(CanGoBack() ? "Elige una opción (o v para volver al pasado): " : "Elige una opción: ");
                string? answer = _input.ReadLine();
                if (answer == null)
                {
                    return false;
                }

                if (answer.Trim().ToLowerInvariant() == "o" && origin?.GoBackLabel != null)
                {
                    presenter.GoBackToOrigin();
                    _output.WriteLine("Vuelves a ese día. Algo te dice que esta vez lo pienses mejor.");
                    return true;
                }

                if (answer.Trim().ToLowerInvariant() == "v" && CanGoBack())
                {
                    if (AskGoBack())
                    {
                        return true;
                    }

                    continue;
                }

                string picked = answer.Trim();
                bool forever = view != null && view.AllowsRule && picked.EndsWith("!", StringComparison.Ordinal);
                if (forever)
                {
                    picked = picked.Substring(0, picked.Length - 1).Trim();
                }

                if (int.TryParse(picked, out int number) && number >= 1 && number <= pending.AvailableOptions.Count)
                {
                    if (forever)
                    {
                        _session.ChooseAlways(pending.AvailableOptions[number - 1]);
                        _output.WriteLine("Anotado en tus reglas: si vuelve a pasar, haces lo mismo sin que te pregunte (r para verlas).");
                    }
                    else
                    {
                        _session.Choose(pending.AvailableOptions[number - 1]);
                    }

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
                    var company = _session.World.CompanyById(player.Job.CompanyId);
                    if (company != null)
                    {
                        string name = company.NameId == null ? string.Empty : _session.Text(company.NameId) + ", ";
                        _output.WriteLine($"Empresa: {name}{_session.Text(company.SizeId)}{(company.IsOpen ? string.Empty : " (quebró)")}.");
                    }

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

            PrintNews();
        }

        /// <summary>Los titulares nuevos del diario de noticias del país (y la ciudad) del jugador.</summary>
        private void PrintNews()
        {
            var news = _session.World.News;
            Residence? home = _session.World.Player.Residence;
            for (int i = System.Math.Min(_newsRead, news.Count); i < news.Count; i++)
            {
                NewsRecord record = news[i];
                if (home != null && record.CountryId == home.CountryId && (record.CityId == null || record.CityId == home.CityId))
                {
                    _output.WriteLine($"  Noticias ({AgeLabel(record.Date)}): {_session.Text(record.TextKey)}");
                }
            }

            _newsRead = news.Count;
        }

        private void PrintFullJournal()
        {
            foreach (ChapterView chapter in new LifePresenter(_session).Chapters())
            {
                PrintChapter(chapter);
            }
        }

        /// <summary>Un año como capítulo: el título, la edad y lo que pasó mes a mes.</summary>
        private void PrintChapter(ChapterView chapter)
        {
            _output.WriteLine();
            _output.WriteLine($"  ~ {chapter.Title} ({chapter.AgeLabel}) ~");
            if (chapter.Photo != null)
            {
                string[] parts = new[] { chapter.Photo.When, chapter.Photo.Place, chapter.Photo.Person }.Where(x => x != null).ToArray()!;
                _output.WriteLine($"    [foto: {string.Join(" · ", parts)}]");
            }

            foreach (ChapterMonthView month in chapter.Months)
            {
                _output.WriteLine($"    {month.Month}: {string.Join(" ", month.Lines)}");
            }
        }

        /// <summary>Muestra la bandeja de correo (los últimos 15) y los deja leídos.</summary>
        private void PrintMail()
        {
            Character player = _session.World.Player;
            if (player.Mail == null)
            {
                _output.WriteLine(player.Phone == null ? "No tienes celular ni correo." : "Tienes celular, pero todavía no tienes correo.");
                return;
            }

            var messages = player.Mail.Messages;
            _output.WriteLine($"Correo: {messages.Count} correos, {player.Mail.UnreadCount} sin leer.");
            for (int i = Math.Max(0, messages.Count - 15); i < messages.Count; i++)
            {
                MistakesLife.Core.Devices.MailMessage message = messages[i];
                MailDefinition? definition = _session.Content.Mail(message.MailId);
                string from = definition == null ? "?" : _session.Text(definition.Sender);
                string subject = definition == null ? message.MailId : _session.Text(definition.SubjectKey);
                _output.WriteLine($"  {(message.Read ? " " : "*")} {AgeLabel(message.Date)} · {from}: {subject}");
                if (!message.Read)
                {
                    _output.WriteLine($"      {_session.Text(message.MailId)}");
                    _session.ReadMail(i);
                }
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

        private string DateLabel()
        {
            GameDate date = _session.World.Date;
            return $"{AgeLabel(date)}, " + string.Format(System.Globalization.CultureInfo.InvariantCulture, _session.Text("ui.fecha.hoy"), date.DayOfMonth, _session.Text("ui.mes." + date.Month));
        }

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
