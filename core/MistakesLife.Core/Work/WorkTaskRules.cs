using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// La pega por dentro (D5b, Isaias eligió "mezcla" y "las dos cosas" el 22-11-2026). Cada mes
    /// le tocan al jugador dos o tres tareas reales de su cargo (las del paquete de su carrera),
    /// cada una con su plazo: un viernes del mes, o la semana de su día real (el IVA, el 20).
    /// Cuando vence, es un momento donde elige cómo hacerla
    /// (<c>jugador.hacer_tarea('calma')</c>), y eso mueve cómo lo ve su jefa: el desempeño de
    /// <see cref="Job.Performance"/>, de 0 a 100, que se muestra como barra con una frase.
    /// <list type="bullet">
    /// <item><c>calma</c>: sale bien (te quedas hasta tarde).</item>
    /// <item><c>rapido</c>: si te la puedes, sale bien igual; si no, sale con errores. Depende de
    /// tu inteligencia.</item>
    /// <item><c>ayuda</c>: con un colega sale bien, pero tu jefa nota que no fue solo tuyo.</item>
    /// <item><c>dejar</c>: la pasas a la semana siguiente (ya se nota); una segunda vez no se
    /// puede: si no la haces, queda como no entregada.</item>
    /// </list>
    /// Con la barra en 100 la jefa ofrece el ascenso (si el cargo siguiente ya se puede); antes,
    /// el jugador lo puede pedir y arriesgarse (<see cref="AskPromotionChance"/>).
    /// </summary>
    public static class WorkTaskRules
    {
        /// <summary>Tareas que le tocan como mínimo en un mes.</summary>
        public const int MinPerMonth = 2;

        /// <summary>Tareas que le tocan como máximo en un mes (la mitad de los meses).</summary>
        public const int MaxPerMonth = 3;

        /// <summary>Puntos de cómo te ve tu jefa con una tarea hecha con calma.</summary>
        public const int Calm = 2;

        /// <summary>Puntos de una tarea hecha rápido que salió bien.</summary>
        public const int QuickGood = 2;

        /// <summary>Puntos de una tarea hecha rápido que salió con errores.</summary>
        public const int QuickBad = -3;

        /// <summary>Puntos de una tarea hecha con ayuda de un colega.</summary>
        public const int Help = 1;

        /// <summary>Puntos que se pierden al dejarla para la semana siguiente.</summary>
        public const int Postpone = -1;

        /// <summary>Puntos que se pierden con una tarea que dejó para después y no entregó.</summary>
        public const int Late = -4;

        /// <summary>Lo que le cuesta al jugador salir bien rápido: esta probabilidad más la mitad de su inteligencia (0 a 100).</summary>
        public const double QuickBase = 0.3;

        /// <summary>Cómo te ve tu jefa en el cargo nuevo, justo después de ascender: de nuevo tienes que mostrarte.</summary>
        public const int AfterPromotion = 60;

        /// <summary>Con esta barra (o más) la jefa ofrece el ascenso sola.</summary>
        public const int OfferAt = 100;

        public const string Calmly = "calma";
        public const string Quickly = "rapido";
        public const string WithHelp = "ayuda";
        public const string Later = "dejar";

        /// <summary>Las formas de hacer una tarea, para <c>jugador.hacer_tarea</c>.</summary>
        public static readonly string[] Approaches = { Calmly, Quickly, WithHelp, Later };

        public const string QuickBadJournal = "diario.tarea.rapido_mal";
        public const string LateJournal = "diario.tarea.no_entregada";
        public const string PromotionYesJournal = "diario.pega.ascenso_pedido_si";
        public const string PromotionNoJournal = "diario.pega.ascenso_pedido_no";

        /// <summary>Las líneas del diario que escribe la pega por dentro (para revisar que tengan texto).</summary>
        public static readonly string[] Journals = { QuickBadJournal, LateJournal, PromotionYesJournal, PromotionNoJournal };

        /// <summary>Hasta dónde llega cada frase de la barra: "ui.jefa.vista.0" hasta 29, "ui.jefa.vista.1" hasta 49...</summary>
        private static readonly int[] PhraseLimits = { 29, 49, 69, 89, 99, 100 };

        /// <summary>Las claves de las frases de la barra, de peor a mejor.</summary>
        public static IEnumerable<string> PhraseKeys => Enumerable.Range(0, PhraseLimits.Length).Select(i => "ui.jefa.vista." + i);

        /// <summary>La frase que explica cómo te ve tu jefa con esa barra.</summary>
        public static string PhraseKey(int performance)
        {
            int index = 0;
            while (index < PhraseLimits.Length - 1 && performance > PhraseLimits[index])
            {
                index++;
            }

            return "ui.jefa.vista." + index;
        }

        /// <summary>La carrera de su cargo, si trae tareas; null si no (un oficio o una carrera sin tareas).</summary>
        public static CareerDefinition? CareerWithTasks(ContentDatabase content, Job? job)
        {
            if (job?.Position == null || !content.TryGetCareerArea(job.Position.AreaId, out CareerAreaDefinition? area))
            {
                return null;
            }

            CareerDefinition career = content.GetCareer(area!.CareerId);
            return career.Tasks.Count > 0 ? career : null;
        }

        /// <summary>
        /// Empieza el mes: lo que dejó para después y no entregó le baja la barra, y le tocan las
        /// tareas nuevas (sin repetir las del mes pasado si alcanzan). Solo el jugador tiene tareas.
        /// </summary>
        public static void StartMonth(World world, ContentDatabase content, Character character, GameDate date)
        {
            Job? job = character.Job;
            if (job == null)
            {
                return;
            }

            foreach (WorkTask late in job.Tasks.Where(t => !t.Done && t.Postponed))
            {
                job.Performance += Late;
                world.WriteJournal(character.Id, LateJournal);
            }

            var previous = new HashSet<string>(job.Tasks.Select(t => t.Id), StringComparer.Ordinal);
            job.Tasks.Clear();
            CareerDefinition? career = CareerWithTasks(content, job);
            if (career == null)
            {
                return;
            }

            var scope = new EventScope(world, content, character, null, readOnly: true);
            List<CareerTaskDefinition> eligible = career.Tasks
                .Where(t => t.CompiledConditions == null || t.CompiledConditions.IsTrue(scope))
                .ToList();
            List<CareerTaskDefinition> fresh = eligible.Where(t => !previous.Contains(t.Id)).ToList();
            List<CareerTaskDefinition> pool = fresh.Count >= MinPerMonth ? fresh : eligible;
            if (pool.Count == 0)
            {
                return;
            }

            GameRandom random = world.Random.Fork("tareas." + character.Id + "." + date.TotalWeeks);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                CareerTaskDefinition swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }

            int count = Math.Min(pool.Count, random.NextDouble() < 0.5 ? MaxPerMonth : MinPerMonth);
            int weeks = date.WeeksToNextMonth;
            List<CareerTaskDefinition> chosen = pool.Take(count).ToList();
            int spread = 0;
            foreach (CareerTaskDefinition task in chosen)
            {
                int offset = task.Day.HasValue ? WeekOfDay(date, weeks, task.Day.Value) : spread++ * weeks / count;
                job.Tasks.Add(new WorkTask(task.Id, date.TotalWeeks + offset));
            }

            job.Tasks.Sort((a, b) => a.DueWeek != b.DueWeek ? a.DueWeek.CompareTo(b.DueWeek) : string.CompareOrdinal(a.Id, b.Id));
        }

        /// <summary>La semana del mes (contando desde esta) que tiene ese día.</summary>
        private static int WeekOfDay(GameDate date, int weeks, int day)
        {
            int offset = 0;
            for (int k = 1; k < weeks; k++)
            {
                if (date.AddWeeks(k).DayOfMonth <= day)
                {
                    offset = k;
                }
            }

            return offset;
        }

        /// <summary>La tarea que ya venció y no ha hecho (la de plazo más cercano), o null.</summary>
        public static WorkTask? Pending(Character character, int week) =>
            character.Job?.Tasks.Where(t => !t.Done && t.DueWeek <= week).OrderBy(t => t.DueWeek).FirstOrDefault();

        /// <summary>Hace la tarea que toca de esa forma (<see cref="Approaches"/>) y mueve cómo lo ve su jefa.</summary>
        public static void Do(World world, Character character, string approach)
        {
            int week = world.Date.TotalWeeks;
            WorkTask task = Pending(character, week) ?? throw new InvalidOperationException("No hay una tarea que toque.");
            Job job = character.Job!;
            int result;
            switch (approach)
            {
                case Calmly:
                    result = Calm;
                    break;
                case Quickly:
                    double chance = QuickBase + (character.Stats.Smarts / 200.0);
                    bool good = world.Random.Fork("tarea.rapido." + character.Id + "." + task.Id + "." + week).NextDouble() < chance;
                    result = good ? QuickGood : QuickBad;
                    if (!good)
                    {
                        world.WriteJournal(character.Id, QuickBadJournal);
                    }

                    break;
                case WithHelp:
                    result = Help;
                    break;
                case Later:
                    if (task.Postponed)
                    {
                        result = Late;
                        world.WriteJournal(character.Id, LateJournal);
                        break;
                    }

                    task.Postponed = true;
                    task.DueWeek = week + 1;
                    job.Performance += Postpone;
                    return;
                default:
                    throw new ArgumentException($"Forma de hacer la tarea desconocida: '{approach}'.", nameof(approach));
            }

            task.Result = result;
            job.Performance += result;
        }

        /// <summary>
        /// La probabilidad de que la jefa diga que sí al pedir el ascenso antes de llegar a 100:
        /// desde un 15 % con lo justo que pide el cargo, subiendo en línea recta hasta un 85 %
        /// justo antes de 100, más o menos según cuánto te respeta (50 es neutro).
        /// </summary>
        public static double AskPromotionChance(int performance, int minPerformance, int bossRespect)
        {
            double span = Math.Max(1, OfferAt - minPerformance);
            double above = Math.Max(0, Math.Min(1, (performance - minPerformance) / span));
            double chance = 0.15 + (0.7 * above) + ((bossRespect - 50) / 250.0);
            return Math.Max(0.05, Math.Min(0.9, chance));
        }
    }
}
