using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Estudiar de verdad (D5a, docs/diseno-juego.md): cada ramo con preguntas en su curso tiene
    /// un dominio de 0 a 100 por año. Las clases lo suben solas hasta su base (lo que le da su
    /// inteligencia, su constancia y el estudio de su horario); estudiando una tarde se sube más
    /// arriba, y si pasan semanas sin estudiar se olvida de a poco hasta la base. Una tarde es un
    /// texto breve y tres preguntas reales del programa de su curso: el jugador responde, sus
    /// aciertos suben el dominio, y la nota de la prueba la saca el personaje con ese dominio, su
    /// inteligencia y su energía (<see cref="GradeRules.Mark"/>).
    /// </summary>
    public static class StudyRules
    {
        /// <summary>Preguntas de una tarde de estudio.</summary>
        public const int QuestionsPerSession = 3;

        /// <summary>Tardes libres que gasta una tarde de estudio.</summary>
        public const int Afternoons = 1;

        /// <summary>Base de las clases: lo que domina sin estudiar nada, con 0 en todo.</summary>
        public const double ClassFloor = 25;

        /// <summary>Lo que suma la inteligencia a la base de las clases, como máximo.</summary>
        public const double ClassSmarts = 35;

        /// <summary>Lo que suma la constancia (poner atención, hacer las tareas), como máximo.</summary>
        public const double ClassConscientiousness = 15;

        /// <summary>Lo que suma, como máximo, un horario con harto estudio.</summary>
        public const double ClassRoutine = 10;

        /// <summary>Parte de la base de las clases con que empieza cada año (lo nuevo del año todavía no se pasa).</summary>
        public const double StartFromClass = 0.6;

        /// <summary>Parte de lo que dominaba el año anterior que se arrastra al año nuevo (la base que trae).</summary>
        public const double StartFromLastYear = 0.3;

        /// <summary>Lo que acercan las clases de una semana a la base (la parte de lo que falta).</summary>
        public const double WeeklyClassPull = 0.06;

        /// <summary>Semanas sin estudiar un ramo después de las cuales se empieza a olvidar.</summary>
        public const int WeeksBeforeForgetting = 3;

        /// <summary>Cada cuántas semanas se olvida un punto, sobre la base.</summary>
        public const int WeeksPerForgottenPoint = 2;

        /// <summary>Lo que sube un acierto con el dominio bajo la mitad.</summary>
        public const int GainLow = 3;

        /// <summary>Lo que sube un acierto entre la mitad y <see cref="HighMastery"/>.</summary>
        public const int GainMid = 2;

        /// <summary>Lo que sube un acierto con el dominio sobre <see cref="HighMastery"/> (cuesta más llegar a 100).</summary>
        public const int GainHigh = 1;

        /// <summary>Ver <see cref="GainHigh"/>.</summary>
        public const int HighMastery = 80;

        /// <summary>Lo que sube un error cuando alguien te explica (la profe, o tu compañera si ella le achuntó).</summary>
        public const int GainExplained = 1;

        /// <summary>Lo que cansa una tarde de estudio.</summary>
        public const int EnergyCost = -3;

        /// <summary>Lo que sube el cariño, de los dos lados, al estudiar con alguien de tu curso.</summary>
        public const int CompanionAffection = 2;

        /// <summary>Cuántas compañeras o compañeros se ofrecen para estudiar (los que más te quieren).</summary>
        public const int CompanionsShown = 2;

        /// <summary>La clave de lo hecho este mes al estudiar con la profe (una vez al mes).</summary>
        public const string TeacherDoneKey = "estudio.profe";

        /// <summary>La clave de lo hecho este mes al estudiar solo.</summary>
        public const string StudyDoneKey = "estudio";

        /// <summary>Los ramos que tiene hoy con preguntas en su curso, en el orden de la libreta.</summary>
        public static IReadOnlyList<QuestionSetDefinition> Sets(World world, ContentDatabase content, Character character)
        {
            var sets = new List<QuestionSetDefinition>();
            EducationStageDefinition? stage = SchoolRules.ClassStage(world, content, character);
            SchoolClass? cls = character.SchoolClass;
            if (stage == null || cls == null || cls.StageId != stage.Id)
            {
                return sets;
            }

            foreach (SubjectDefinition subject in GradeRules.Subjects(stage, cls.Level))
            {
                QuestionSetDefinition? set = content.QuestionSet(stage.Id, cls.Level, subject.Id);
                if (set != null)
                {
                    sets.Add(set);
                }
            }

            return sets;
        }

        /// <summary>Las preguntas del ramo en su curso de hoy; null si ese ramo no tiene.</summary>
        public static QuestionSetDefinition? Set(World world, ContentDatabase content, Character character, string subjectId) =>
            Sets(world, content, character).FirstOrDefault(s => s.SubjectId == subjectId);

        /// <summary>
        /// La base de las clases: hasta dónde llega el dominio de un ramo sin estudiar en la casa
        /// (de 25 a 85). Sale de su inteligencia, su constancia y cuánto estudio deja su horario.
        /// </summary>
        public static int ClassLevel(World world, ContentDatabase content, Character character)
        {
            double routine = 0;
            if (character.Routine != null)
            {
                int mind = RoutineRules.Total(world, content, character, character.Routine, "mente");
                routine = Math.Max(0, Math.Min(1.0, mind / (double)GradeRules.FullStudy));
            }

            double level = ClassFloor
                + (ClassSmarts * character.Stats.Smarts / 100.0)
                + (ClassConscientiousness * character.Personality.Conscientiousness / 100.0)
                + (ClassRoutine * routine);
            return (int)Math.Round(level, MidpointRounding.AwayFromZero);
        }

        /// <summary>Cuánto domina ese ramo hoy; null si el ramo no se estudia con preguntas.</summary>
        public static int? Mastery(Character character, string subjectId) =>
            character.SchoolRecord != null && character.SchoolRecord.Mastery.TryGetValue(subjectId, out int value) ? value : (int?)null;

        /// <summary>
        /// Una semana de su vida: si empezó un año escolar nuevo, cada ramo parte desde su base (lo
        /// que dominaba el año anterior y lo que le dan las clases); en semanas de clases, las
        /// clases lo acercan a su base y, si hace semanas que no estudia un ramo, lo va olvidando.
        /// </summary>
        public static void Week(World world, ContentDatabase content, Character character)
        {
            IReadOnlyList<QuestionSetDefinition> sets = Sets(world, content, character);
            if (sets.Count == 0 || character.Residence == null)
            {
                return;
            }

            SchoolRecord record = character.SchoolRecord ??= new SchoolRecord();
            int classLevel = ClassLevel(world, content, character);
            StartYear(character, record, sets, classLevel);
            if (!content.GetCountry(character.Residence.CountryId).IsSchoolTime(world.Date))
            {
                return;
            }

            int week = world.Date.TotalWeeks;
            foreach (QuestionSetDefinition set in sets)
            {
                int mastery = record.Mastery.TryGetValue(set.SubjectId, out int m) ? m : Start(null, classLevel);
                if (mastery < classLevel)
                {
                    mastery = Math.Min(classLevel, mastery + Math.Max(1, (int)Math.Ceiling((classLevel - mastery) * WeeklyClassPull)));
                }
                else if (mastery > classLevel)
                {
                    int last = record.LastStudiedWeek.TryGetValue(set.SubjectId, out int l) ? l : int.MinValue / 2;
                    int idle = week - last;
                    if (idle >= WeeksBeforeForgetting && idle % WeeksPerForgottenPoint == 0)
                    {
                        mastery--;
                    }
                }

                record.Mastery[set.SubjectId] = mastery;
            }
        }

        /// <summary>
        /// Si cambió el año escolar, lo de este año pasa a ser "el año anterior" y cada ramo parte
        /// de nuevo (<see cref="Start"/>). Las preguntas vistas se olvidan: son de otro curso.
        /// </summary>
        public static void StartYear(Character character, SchoolRecord record, IReadOnlyList<QuestionSetDefinition> sets, int classLevel)
        {
            int year = character.SchoolClass?.SchoolYear ?? 0;
            if (record.MasteryYear == year && record.Mastery.Count > 0)
            {
                return;
            }

            if (record.Mastery.Count > 0)
            {
                record.LastMastery.Clear();
                foreach (KeyValuePair<string, int> pair in record.Mastery)
                {
                    record.LastMastery[pair.Key] = pair.Value;
                }
            }

            record.Mastery.Clear();
            record.LastStudiedWeek.Clear();
            record.SeenQuestions.Clear();
            record.StudySessions = 0;
            record.MasteryYear = year;
            foreach (QuestionSetDefinition set in sets)
            {
                int? last = record.LastMastery.TryGetValue(set.SubjectId, out int l) ? l : (int?)null;
                record.Mastery[set.SubjectId] = Start(last, classLevel);
            }
        }

        /// <summary>
        /// Con cuánto parte un ramo el año: una parte de la base de las clases (lo nuevo del año
        /// todavía no se pasa) más una parte de lo que dominaba el año anterior. Quien estudió harto
        /// el año pasado parte más arriba; a quien le fue mal le cuesta más.
        /// </summary>
        public static int Start(int? lastYear, int classLevel)
        {
            double last = lastYear ?? (StartFromClass * classLevel);
            double start = (StartFromClass * classLevel) + (StartFromLastYear * last);
            return Math.Max(0, Math.Min(100, (int)Math.Round(start, MidpointRounding.AwayFromZero)));
        }

        /// <summary>
        /// La unidad del programa que va hoy: el año escolar se reparte parejo entre las unidades,
        /// en orden (en vacaciones de invierno sigue la que iba; en verano, la última).
        /// </summary>
        public static QuestionUnitDefinition CurrentUnit(CountryDefinition country, GameDate date, QuestionSetDefinition set)
        {
            int total = Math.Max(1, country.SchoolWeeksPerYear());
            int done = SchoolWeeksSoFar(country, date);
            int index = Math.Min(set.Units.Count - 1, done * set.Units.Count / total);
            return set.Units[Math.Max(0, index)];
        }

        /// <summary>Cuántas semanas de clases van del año escolar (contando esta, si hay clases).</summary>
        public static int SchoolWeeksSoFar(CountryDefinition country, GameDate date)
        {
            if (!country.SchoolYearStartDay.HasValue)
            {
                return 0;
            }

            int startWeek = Math.Min(country.SchoolYearStartDay.Value / 7, GameDate.WeeksPerYear - 1);
            int weekOfYear = date.DayOfYear / 7;
            int since = weekOfYear >= startWeek ? weekOfYear - startWeek : weekOfYear + GameDate.WeeksPerYear - startWeek;
            int done = 0;
            for (int i = 0; i < since; i++)
            {
                if (country.IsSchoolTime(new GameDate(startWeek + i)))
                {
                    done++;
                }
            }

            return done;
        }

        /// <summary>
        /// Con quiénes de su curso puede estudiar este mes: los que más lo quieren (y que lo
        /// quieren algo), que todavía no estudiaron con él este mes.
        /// </summary>
        public static IReadOnlyList<Character> Companions(World world, Character character)
        {
            FreeTime? month = FreeTimeRules.ThisMonth(world, character);
            return SchoolRules.Classmates(world, character)
                .Select(c => (Person: c, Affection: RelationshipRules.Feelings(world, c, character).Affection))
                .Where(x => x.Affection > 0 && !(month?.Done.Contains(CompanionKey(x.Person.Id)) ?? false))
                .OrderByDescending(x => x.Affection)
                .ThenBy(x => x.Person.Id)
                .Take(CompanionsShown)
                .Select(x => x.Person)
                .ToList();
        }

        /// <summary>Su profe jefe, si puede pedirle ayuda este mes (una vez al mes).</summary>
        public static Character? Teacher(World world, Character character)
        {
            FreeTime? month = FreeTimeRules.ThisMonth(world, character);
            return month?.Done.Contains(TeacherDoneKey) ?? false ? null : SchoolRules.Teacher(world, character);
        }

        /// <summary>Si puede estudiar ese ramo ahora: tiene preguntas en su curso, hay clases y le queda una tarde libre.</summary>
        public static bool CanStudy(World world, ContentDatabase content, Character character, string subjectId) =>
            character.IsAlive && character.Routine != null && SchoolRules.InClasses(world, content, character)
            && Set(world, content, character, subjectId) != null
            && FreeTimeRules.Left(world, content, character) >= Afternoons;

        /// <summary>
        /// Empieza una tarde de estudio de ese ramo (sin nadie, con alguien de su curso o con su
        /// profe jefe): gasta la tarde y elige tres preguntas de la unidad que va, que no haya visto
        /// este año (si ya vio todas, de las unidades anteriores; recién después repite).
        /// </summary>
        /// <exception cref="InvalidOperationException">Si ahora no puede estudiar ese ramo, o esa persona no puede estudiar con él.</exception>
        public static StudySession Start(World world, ContentDatabase content, Character character, string subjectId, int? companionId, bool withTeacher)
        {
            if (!CanStudy(world, content, character, subjectId))
            {
                throw new InvalidOperationException($"Ahora no se puede estudiar '{subjectId}'.");
            }

            if (companionId.HasValue && withTeacher)
            {
                throw new InvalidOperationException("Se estudia con alguien del curso o con la profe, no con los dos.");
            }

            Character? companion = null;
            if (companionId.HasValue)
            {
                companion = Companions(world, character).FirstOrDefault(c => c.Id == companionId.Value)
                    ?? throw new InvalidOperationException($"#{companionId} no puede estudiar contigo este mes.");
            }

            if (withTeacher && Teacher(world, character) == null)
            {
                throw new InvalidOperationException("Tu profe jefe no puede ayudarte este mes.");
            }

            QuestionSetDefinition set = Set(world, content, character, subjectId)!;
            SchoolRecord record = character.SchoolRecord ??= new SchoolRecord();
            Week0(world, content, character, record);
            CountryDefinition country = content.GetCountry(character.Residence!.CountryId);
            QuestionUnitDefinition unit = CurrentUnit(country, world.Date, set);
            GameRandom random = new GameRandom(world.Seed).Fork(string.Format(CultureInfo.InvariantCulture, "estudio.{0}.{1}.{2}", world.Date.TotalWeeks, record.StudySessions, subjectId));

            var picked = Pick(set, unit, record, random);
            var questions = new List<StudyQuestion>();
            foreach (QuestionDefinition question in picked)
            {
                record.SeenQuestions.Add(question.Id);
                int[] order = Enumerable.Range(0, question.Options).ToArray();
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = random.NextInt(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                bool? companionCorrect = companion == null ? (bool?)null : random.Chance(CompanionChance(companion));
                questions.Add(new StudyQuestion(question, order, companionCorrect));
            }

            record.StudySessions++;
            record.LastStudiedWeek[subjectId] = world.Date.TotalWeeks;
            string done = companion != null ? CompanionKey(companion.Id) : withTeacher ? TeacherDoneKey : StudyDoneKey;
            FreeTimeRules.Spend(world, character, Afternoons, done);
            return new StudySession(subjectId, unit, questions, companion?.Id, withTeacher, record.Mastery[subjectId]);
        }

        /// <summary>
        /// Responde la pregunta que va (la posición que eligió entre las alternativas que ve). Un
        /// acierto sube el dominio del ramo al tiro (menos cuanto más alto está); un error no lo
        /// baja, y si alguien le explica (la profe, o su compañera si ella la acertó) sube un poco.
        /// </summary>
        public static StudyAnswer Answer(Character character, StudySession session, int position)
        {
            StudyQuestion question = session.Current ?? throw new InvalidOperationException("Ya respondió todas las preguntas de la tarde.");
            if (position < 0 || position >= question.Order.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            SchoolRecord record = character.SchoolRecord!;
            int before = record.Mastery[session.SubjectId];
            bool correct = position == question.CorrectPosition;
            int gain = correct ? Gain(before)
                : session.WithTeacher || question.CompanionCorrect == true ? GainExplained : 0;
            record.Mastery[session.SubjectId] = Math.Min(100, before + gain);
            var answer = new StudyAnswer(position, correct, question.CorrectPosition, record.Mastery[session.SubjectId] - before, question.CompanionCorrect);
            session.Answers.Add(answer);
            return answer;
        }

        /// <summary>
        /// Termina la tarde: cansa, la inteligencia sube un poco si acertó la mayoría, quien estudió
        /// con él lo quiere más (y él a esa persona), y si el ramo tiene la prueba anunciada, queda
        /// más preparada. Las preguntas que no respondió quedan sin responder (no suben nada).
        /// </summary>
        public static StudyOutcome Finish(World world, Character character, StudySession session)
        {
            if (session.Finished)
            {
                throw new InvalidOperationException("Esa tarde de estudio ya terminó.");
            }

            session.Finished = true;
            SchoolRecord record = character.SchoolRecord!;
            int correct = session.Answers.Count(a => a.Correct);
            var changes = new List<ActionChange>();
            Add(changes, "energia", Move(character.Stats.Energy, EnergyCost, v => character.Stats.Energy = v, () => character.Stats.Energy));
            if (correct * 2 > session.Questions.Count)
            {
                Add(changes, "inteligencia", Move(character.Stats.Smarts, 1, v => character.Stats.Smarts = v, () => character.Stats.Smarts));
            }

            if (session.CompanionId.HasValue && world.TryGetCharacter(session.CompanionId.Value, out Character? companion))
            {
                Relationship theirs = RelationshipRules.Edit(world, companion, character);
                int before = theirs.Affection;
                theirs.Affection += CompanionAffection;
                RelationshipRules.Edit(world, character, companion).Affection += CompanionAffection;
                Add(changes, "carino", theirs.Affection - before);
            }

            if (record.TestPending && record.TestSubjectId == session.SubjectId)
            {
                int before = record.TestPreparation;
                record.TestPreparation = Math.Min(GradeRules.MaxPreparation, before + 1);
                Add(changes, "preparar_prueba", record.TestPreparation - before);
            }

            return new StudyOutcome(correct, session.Questions.Count, session.MasteryBefore, record.Mastery[session.SubjectId], changes);
        }

        /// <summary>Lo que sube un acierto con ese dominio.</summary>
        public static int Gain(int mastery) => mastery < 50 ? GainLow : mastery < HighMastery ? GainMid : GainHigh;

        /// <summary>
        /// La probabilidad de que alguien de su curso acierte una pregunta: sale de su inteligencia
        /// y su constancia (nadie juega por esa persona, así que se estima así).
        /// </summary>
        public static double CompanionChance(Character companion) =>
            Math.Max(0.2, Math.Min(0.95, 0.35 + (0.5 * companion.Stats.Smarts / 100.0) + (0.1 * companion.Personality.Conscientiousness / 100.0)));

        /// <summary>La clave de lo hecho este mes al estudiar con esa persona.</summary>
        public static string CompanionKey(int personId) => "estudio#" + personId.ToString(CultureInfo.InvariantCulture);

        /// <summary>Se asegura de que el dominio de este año exista antes de estudiar (si el año recién empezó).</summary>
        private static void Week0(World world, ContentDatabase content, Character character, SchoolRecord record)
        {
            IReadOnlyList<QuestionSetDefinition> sets = Sets(world, content, character);
            StartYear(character, record, sets, ClassLevel(world, content, character));
            foreach (QuestionSetDefinition set in sets.Where(s => !record.Mastery.ContainsKey(s.SubjectId)))
            {
                record.Mastery[set.SubjectId] = Start(null, ClassLevel(world, content, character));
            }
        }

        private static List<QuestionDefinition> Pick(QuestionSetDefinition set, QuestionUnitDefinition unit, SchoolRecord record, GameRandom random)
        {
            int unitIndex = set.Units.IndexOf(unit);
            var pools = new List<List<QuestionDefinition>>
            {
                unit.Questions.Where(q => !record.SeenQuestions.Contains(q.Id)).ToList(),
                set.Units.Take(unitIndex).Reverse().SelectMany(u => u.Questions).Where(q => !record.SeenQuestions.Contains(q.Id)).ToList(),
                unit.Questions.ToList(),
            };
            var picked = new List<QuestionDefinition>();
            foreach (List<QuestionDefinition> pool in pools)
            {
                var left = pool.Where(q => picked.All(p => p.Id != q.Id)).ToList();
                while (picked.Count < QuestionsPerSession && left.Count > 0)
                {
                    int i = random.NextInt(left.Count);
                    picked.Add(left[i]);
                    left.RemoveAt(i);
                }
            }

            return picked;
        }

        private static void Add(List<ActionChange> changes, string key, int delta)
        {
            if (delta != 0)
            {
                changes.Add(new ActionChange(key, delta));
            }
        }

        private static int Move(int before, int amount, Action<int> set, Func<int> get)
        {
            set(before + amount);
            return get() - before;
        }
    }
}
