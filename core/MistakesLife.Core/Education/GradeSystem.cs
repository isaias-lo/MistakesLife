using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Fase de sistemas: las notas del colegio del jugador.
    /// <list type="bullet">
    /// <item>Cada semana de clases, cada ramo puede tener una nota (en un año, las
    /// "notas_por_ano" del país). Esas pasan solas, sin detener el tiempo.</item>
    /// <item>Algunas veces al año se anuncia una prueba para la semana siguiente: es la que
    /// se juega como momento (prepararla o no, con <c>jugador.preparar_prueba(n)</c>), y la
    /// semana siguiente se devuelve con su nota (<c>jugador.prueba_devuelta</c>).</item>
    /// <item>Al empezar las vacaciones de verano se cierra el año: nota final por ramo,
    /// promedio general y si pasa de curso. Si no pasa, repite: el próximo año va a un curso
    /// nuevo con los de un año menos ("diario.notas.repitio").</item>
    /// </list>
    /// </summary>
    public sealed class GradeSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.notas";

        private readonly ContentDatabase _content;

        public GradeSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Residence == null)
            {
                return;
            }

            CountryDefinition country = _content.GetCountry(player.Residence.CountryId);
            GradeScaleDefinition? scale = country.GradeScale;
            EducationStageDefinition? stage = SchoolRules.ClassStage(world, _content, player);
            SchoolRecord? record = player.SchoolRecord;
            if (scale == null)
            {
                return;
            }

            if (record != null && record.Marks.Count > 0 && (stage == null || country.IsSummerBreak(context.Date)))
            {
                CloseYear(world, country, player, record, scale, stage ?? LastStage(country, player));
                return;
            }

            if (stage == null || player.SchoolClass == null || !country.IsSchoolTime(context.Date))
            {
                return;
            }

            record ??= player.SchoolRecord = new SchoolRecord();
            GameRandom random = context.RandomFor(SystemId);
            IReadOnlyList<SubjectDefinition> subjects = GradeRules.Subjects(stage, player.SchoolClass.Level);
            if (subjects.Count == 0)
            {
                return;
            }

            int week = context.Date.TotalWeeks;
            if (record.TestPending && week >= record.TestWeek)
            {
                if (!record.TestMakeup && random.Chance(GradeRules.AbsentChance(player)))
                {
                    // Faltó ese día (enfermo): la rinde la semana siguiente, como recuperativa.
                    record.TestMakeup = true;
                    record.TestWeek = week + 1;
                    world.WriteJournal(player.Id, "diario.notas.recuperativa");
                    return;
                }

                double mark = GradeRules.Mark(world, _content, player, scale, record.TestSubjectId!, record.TestPreparation, random);
                if (record.TestCheating)
                {
                    // Copió (D5a3): si lo pillan se anula con la nota mínima; si no, la nota sube,
                    // pero el dominio del ramo no, porque no aprendió.
                    record.TestCheating = false;
                    if (random.Chance(GradeRules.CaughtChance))
                    {
                        mark = scale.Min;
                        record.TestCaught = true;
                        player.SetFlag("pillado_copiando");
                        world.WriteJournal(player.Id, "diario.notas.pillado_copiando");
                    }
                    else
                    {
                        mark = GradeRules.Copied(mark, scale);
                    }
                }

                Add(record, record.TestSubjectId!, mark);
                record.TestMark = mark;
                record.TestReturnedWeek = week;
                return;
            }

            double weeks = Math.Max(1, country.SchoolWeeksPerYear());
            foreach (SubjectDefinition subject in subjects)
            {
                if (random.Chance(scale.MarksPerYear / weeks))
                {
                    Add(record, subject.Id, GradeRules.Mark(world, _content, player, scale, subject.Id, 0, random));
                }
            }

            bool canAnnounce = record.TestSubjectId == null || (!record.TestPending && week - record.TestWeek >= GradeRules.WeeksBetweenAnnouncedTests);
            var written = subjects.Where(s => s.WrittenTests).ToList();
            if (canAnnounce && written.Count > 0 && random.Chance(GradeRules.AnnouncedTestsPerYear / weeks))
            {
                record.TestSubjectId = written[random.NextInt(written.Count)].Id;
                record.TestWeek = week + 1;
                record.TestPreparation = 0;
                record.TestMark = null;
                record.TestMakeup = false;
                record.TestCheating = false;
                record.TestCaught = false;
            }
        }

        private static void Add(SchoolRecord record, string subjectId, double mark)
        {
            if (!record.Marks.TryGetValue(subjectId, out List<double>? marks))
            {
                marks = new List<double>();
                record.Marks[subjectId] = marks;
            }

            marks.Add(mark);
        }

        /// <summary>La etapa del curso que acaba de terminar (al salir de cuarto medio ya no hay etapa con curso).</summary>
        private static EducationStageDefinition? LastStage(CountryDefinition country, Character player)
        {
            string? id = player.SchoolClass?.StageId;
            return id == null ? null : country.EducationStages.Find(s => s.Id == id);
        }

        private static void CloseYear(World world, CountryDefinition country, Character player, SchoolRecord record, GradeScaleDefinition scale, EducationStageDefinition? stage)
        {
            SortedDictionary<string, double> finals = GradeRules.Finals(record, scale);
            double? general = GradeRules.General(finals, scale);
            record.Marks.Clear();
            record.LastFinals.Clear();
            foreach (var final in finals)
            {
                record.LastFinals[final.Key] = final.Value;
            }

            record.TestSubjectId = null;
            record.TestMark = null;
            record.TestPreparation = 0;
            record.TestMakeup = false;
            record.TestCheating = false;
            record.TestCaught = false;
            int absences = record.Absences;
            record.Absences = 0;
            if (!general.HasValue)
            {
                return;
            }

            record.YearAverages.Add(general.Value);
            if (stage != null && !GradeRules.MeetsAttendance(stage, country, absences))
            {
                // Faltó más de lo que permite el reglamento (la cimarra): repite aunque tenga notas para pasar.
                record.RepeatedYears++;
                player.SchoolClass = null;
                player.SetFlag("repitio_curso");
                world.WriteJournal(player.Id, "diario.notas.repitio_asistencia");
                return;
            }

            if (stage != null && !GradeRules.Passes(stage, finals, scale))
            {
                record.RepeatedYears++;
                player.SchoolClass = null;
                player.SetFlag("repitio_curso");
                world.WriteJournal(player.Id, "diario.notas.repitio");
                return;
            }

            world.WriteJournal(player.Id, GradeRules.OnSevenScale(general.Value, scale) >= 6.0 ? "diario.notas.excelente" : "diario.notas.paso");
        }
    }
}
