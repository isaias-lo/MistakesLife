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
    /// le toca al jugador una tarea real de su cargo (de las del paquete de su carrera), con su
    /// plazo: un viernes del mes, o la semana de su día real (el IVA, el 20). Una sola, para que la
    /// pega no se coma los momentos del resto de la vida (con dos o tres eran unos 30 al año).
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
    /// <item><c>saber</c> (D5b2, solo las tareas con temas): la resuelves tú respondiendo tres
    /// preguntas reales de la carrera; sale según cuántas aciertes (<see cref="KnowResult"/>).</item>
    /// </list>
    /// Con la barra en 100 la jefa ofrece el ascenso (si el cargo siguiente ya se puede); antes,
    /// el jugador lo puede pedir y arriesgarse (<see cref="AskPromotionChance"/>).
    /// </summary>
    public static class WorkTaskRules
    {
        /// <summary>Tareas que le tocan en un mes.</summary>
        public const int PerMonth = 1;

        /// <summary>Tareas que trae como mínimo una carrera con tareas (para turnarlas mes a mes).</summary>
        public const int MinPerCareer = 3;

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
        public const string Knowing = "saber";

        /// <summary>Las formas de hacer una tarea, para <c>jugador.hacer_tarea</c>.</summary>
        public static readonly string[] Approaches = { Calmly, Quickly, WithHelp, Later, Knowing };

        /// <summary>La opción de los momentos de tareas que se resuelve respondiendo preguntas (Unity abre las preguntas).</summary>
        public const string KnowOptionKey = "opcion.tarea.saber";

        /// <summary>Preguntas para resolver una tarea con lo que sabes.</summary>
        public const int QuizQuestions = 3;

        /// <summary>Puntos con las tres preguntas bien: te luciste, mejor que con calma.</summary>
        public const int KnowAll = 4;

        /// <summary>Puntos con dos de tres: sale bien.</summary>
        public const int KnowMost = 2;

        /// <summary>Puntos con una de tres: sale, pero tu jefa tiene que corregir cosas.</summary>
        public const int KnowSome = -1;

        /// <summary>Puntos sin ninguna buena: sale con errores.</summary>
        public const int KnowNone = -3;

        /// <summary>Sin responder (el simulador): la probabilidad de acertar cada pregunta es esta más un tercio de su inteligencia (0 a 100).</summary>
        public const double GuessBase = 0.35;

        public const string QuickBadJournal = "diario.tarea.rapido_mal";
        public const string LateJournal = "diario.tarea.no_entregada";
        public const string PromotionYesJournal = "diario.pega.ascenso_pedido_si";
        public const string PromotionNoJournal = "diario.pega.ascenso_pedido_no";
        public const string KnowBadJournal = "diario.tarea.saber_mal";

        /// <summary>Las líneas del diario que escribe la pega por dentro (para revisar que tengan texto).</summary>
        public static readonly string[] Journals = { QuickBadJournal, LateJournal, PromotionYesJournal, PromotionNoJournal, KnowBadJournal };

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
        /// tarea nueva (sin repetir la del mes pasado). Solo el jugador tiene tareas.
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
            // Una tarea no vuelve el mes siguiente (solo si no hay otra de su área).
            List<CareerTaskDefinition> pool = fresh.Count > 0 ? fresh : eligible;
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

            int weeks = date.WeeksToNextMonth;
            List<CareerTaskDefinition> chosen = pool.Take(PerMonth).ToList();
            foreach (CareerTaskDefinition task in chosen)
            {
                // Sin día real, vence un viernes cualquiera del mes.
                int offset = task.Day.HasValue ? WeekOfDay(date, weeks, task.Day.Value) : random.NextInt(weeks);
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
                case Knowing:
                    {
                        int correct = task.QuizCorrect ?? Guess(world, character, task, week);
                        task.QuizCorrect = null;
                        result = KnowResult(correct);
                        if (result <= KnowNone)
                        {
                            world.WriteJournal(character.Id, KnowBadJournal);
                        }

                        break;
                    }
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

        /// <summary>Lo que suma una tarea resuelta con lo que sabes, según cuántas de las tres acertó.</summary>
        public static int KnowResult(int correct) =>
            correct >= QuizQuestions ? KnowAll : correct == QuizQuestions - 1 ? KnowMost : correct > 0 ? KnowSome : KnowNone;

        /// <summary>Si elige resolverla con lo que sabe sin responder (el simulador): acierta según su inteligencia.</summary>
        private static int Guess(World world, Character character, WorkTask task, int week)
        {
            GameRandom random = world.Random.Fork("tarea.saber." + character.Id + "." + task.Id + "." + week);
            double chance = GuessBase + (character.Stats.Smarts / 300.0);
            int correct = 0;
            for (int i = 0; i < QuizQuestions; i++)
            {
                if (random.NextDouble() < chance)
                {
                    correct++;
                }
            }

            return correct;
        }

        /// <summary>
        /// Empieza a resolver con lo que sabe la tarea que toca: tres preguntas al azar de sus
        /// temas, con las alternativas en otro orden cada vez.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si no toca una tarea, o la que toca no tiene temas.</exception>
        public static WorkQuiz StartQuiz(World world, ContentDatabase content, Character character)
        {
            int week = world.Date.TotalWeeks;
            WorkTask task = Pending(character, week) ?? throw new InvalidOperationException("No hay una tarea que toque.");
            CareerTaskDefinition definition = content.GetCareerTask(task.Id);
            List<QuestionDefinition> pool = definition.Units
                .Select(content.QuestionUnit)
                .Where(u => u != null)
                .SelectMany(u => u!.Questions)
                .ToList();
            if (pool.Count < QuizQuestions)
            {
                throw new InvalidOperationException($"La tarea '{task.Id}' no tiene preguntas.");
            }

            GameRandom random = world.Random.Fork("tarea.preguntas." + character.Id + "." + task.Id + "." + week);
            var questions = new List<Education.StudyQuestion>();
            while (questions.Count < QuizQuestions)
            {
                int i = random.NextInt(pool.Count);
                QuestionDefinition question = pool[i];
                pool.RemoveAt(i);
                int[] order = Enumerable.Range(0, question.Options).ToArray();
                for (int k = order.Length - 1; k > 0; k--)
                {
                    int j = random.NextInt(k + 1);
                    int swap = order[k];
                    order[k] = order[j];
                    order[j] = swap;
                }

                questions.Add(new Education.StudyQuestion(question, order, null));
            }

            return new WorkQuiz(task.Id, questions);
        }

        /// <summary>Responde la pregunta que va (la posición que eligió entre las alternativas que ve).</summary>
        public static Education.StudyAnswer Answer(WorkQuiz quiz, int position)
        {
            Education.StudyQuestion question = quiz.Current ?? throw new InvalidOperationException("Ya respondió las tres preguntas.");
            if (position < 0 || position >= question.Order.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            bool correct = position == question.CorrectPosition;
            var answer = new Education.StudyAnswer(position, correct, question.CorrectPosition, 0, null);
            quiz.Answers.Add(answer);
            return answer;
        }

        /// <summary>
        /// Deja anotados en la tarea los aciertos (las que no respondió cuentan como malas), para
        /// que la opción <see cref="KnowOptionKey"/> la haga con ese resultado.
        /// </summary>
        public static void FinishQuiz(Character character, WorkQuiz quiz, int week)
        {
            WorkTask task = Pending(character, week) ?? throw new InvalidOperationException("No hay una tarea que toque.");
            if (task.Id != quiz.TaskId)
            {
                throw new InvalidOperationException("Las preguntas eran de otra tarea.");
            }

            task.QuizCorrect = quiz.Answers.Count(a => a.Correct);
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
