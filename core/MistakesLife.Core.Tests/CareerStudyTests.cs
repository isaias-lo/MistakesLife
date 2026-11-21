using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>D5a2b: estudiar de verdad los ramos del primer año de la carrera (universidad, CFT e instituto).</summary>
    public class CareerStudyTests
    {
        private const string Civil = "carrera.software.nivel.ingenieria_civil_computacion";

        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en Chile a los 18 años que empieza a estudiar esa carrera.</summary>
        private static World StartStudying(string levelId = Civil, ulong seed = 3)
        {
            World world = WorldFactory.CreateNewLife(seed, Content, "pais.chile");
            new WeekCycle().AdvanceWeeks(world, (18 * GameDate.WeeksPerYear) + 10 - world.Player.AgeInWeeks(world.Date), out _);
            Scripting.Effect.Parse($"estudiar('{levelId}')").Apply(new EventScope(world, Content, world.Player, null, readOnly: false));
            world.Player.Routine = new Routine("libre");
            return world;
        }

        private static WeekCycle University() =>
            new WeekCycle().Register(new CareerSystem()).Register(new StudySystem(Content)).Register(new UniversitySystem(Content));

        [Fact]
        public void Preguntas_DeLasTresCarreras_CubrenElPrimerAnoEnCadaInstitucion()
        {
            int sets = 0;
            foreach (CareerDefinition career in Content.Careers)
            {
                foreach (CareerLevelDefinition level in career.Levels.Where(l => l.Semesters > 0))
                {
                    for (int semester = 1; semester <= System.Math.Min(2, level.Semesters); semester++)
                    {
                        foreach (CourseDefinition course in career.Courses.Where(c => !c.Last && c.In(level.Institution, semester, level.Semesters)))
                        {
                            QuestionSetDefinition? set = Content.QuestionSet(career.Id, semester, course.Id);
                            Assert.True(set != null, $"Faltan las preguntas de {course.Id} en el semestre {semester} ({level.Id}).");
                            Assert.True(set!.Units.Sum(u => u.Questions.Count) >= 20, $"{course.Id} semestre {semester}: menos de 20 preguntas.");
                            sets++;
                        }
                    }
                }
            }

            Assert.True(sets > 0);
        }

        [Theory]
        [InlineData("carrera.software")]
        public void Preguntas_DeUnaCarreraCompleta_CubrenTodosLosSemestres_MenosTalleresYElectivos(string careerId)
        {
            CareerDefinition career = Content.Careers.Single(c => c.Id == careerId);
            int sets = 0;
            foreach (CareerLevelDefinition level in career.Levels.Where(l => l.Semesters > 0))
            {
                for (int semester = 1; semester <= level.Semesters; semester++)
                {
                    foreach (CourseDefinition course in career.Courses.Where(c => c.In(level.Institution, semester, level.Semesters)))
                    {
                        QuestionSetDefinition? set = Content.QuestionSet(career.Id, semester, course.Id);
                        if (course.Last || !course.WrittenTests)
                        {
                            Assert.True(set == null, $"{course.Id} se evalúa con proyectos, no con preguntas.");
                            continue;
                        }

                        Assert.True(set != null, $"Faltan las preguntas de {course.Id} en el semestre {semester} ({level.Id}).");
                        Assert.True(set!.Units.Sum(u => u.Questions.Count) >= 20, $"{course.Id} semestre {semester}: menos de 20 preguntas.");
                        sets++;
                    }
                }
            }

            Assert.True(sets > 0);
        }

        [Fact]
        public void PreguntasDeUnRamoSinPruebaEscrita_NoCargan()
        {
            PackBundle real = PackBundle.FromDirectory(RepoPacks.Directory);
            var files = real.Files.ToDictionary(f => f.Key, f => f.Value);
            string key = files.Keys.Single(k => k.EndsWith("careers/software/career.json", System.StringComparison.Ordinal));
            files[key] = files[key].Replace("\"id\": \"ramo.software.calculo\",", "\"id\": \"ramo.software.calculo\", \"prueba_escrita\": false,");
            var error = Assert.Throws<ContentLoadException>(() => PackLoader.LoadBundle(new PackBundle(files)));
            Assert.Contains(error.Errors, e => e.Message.Contains("pruebas escritas"));
        }

        [Fact]
        public void EnLaSeccion_LosRamosDelSemestreTienenDominio_YSeEstudianConPreguntas()
        {
            World world = StartStudying();
            University().AdvanceWeeks(world, 2, out _);
            Character player = world.Player;

            var sets = StudyRules.Sets(world, Content, player);
            Assert.Equal(
                new[] { "ramo.software.algebra", "ramo.software.calculo", "ramo.software.introduccion_ingenieria", "ramo.software.programacion" },
                sets.Select(s => s.SubjectId).OrderBy(s => s, System.StringComparer.Ordinal).ToArray());
            Assert.All(sets, s => Assert.NotNull(StudyRules.Mastery(player, s.SubjectId)));
            Assert.True(StudyRules.CanStudy(world, Content, player, "ramo.software.calculo"));

            int before = StudyRules.Mastery(player, "ramo.software.calculo")!.Value;
            StudySession session = StudyRules.Start(world, Content, player, "ramo.software.calculo", null, withTeacher: true);
            Assert.Equal(StudyRules.QuestionsPerSession, session.Questions.Count);
            Assert.StartsWith("tema.software.calculo.1.", session.Unit.Id, System.StringComparison.Ordinal);
            while (session.Current != null)
            {
                StudyRules.Answer(player, session, session.Current.CorrectPosition);
            }

            StudyRules.Finish(world, player, session);
            Assert.True(StudyRules.Mastery(player, "ramo.software.calculo") > before);
        }

        [Fact]
        public void LaUnidadQueVa_AvanzaConLasSemanasDelSemestre()
        {
            World world = StartStudying();
            University().AdvanceWeeks(world, 2, out _);
            Character player = world.Player;
            QuestionSetDefinition set = StudyRules.Set(world, Content, player, "ramo.software.calculo")!;
            Assert.Equal(set.Units[0].Id, StudyRules.CurrentUnit(world, Content, player, set).Id);

            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester - 4, out _);
            Assert.Equal(set.Units[set.Units.Count - 1].Id, StudyRules.CurrentUnit(world, Content, player, set).Id);
        }

        [Fact]
        public void NotaDelSemestre_SaleDelDominio_QuienEstudioSacaMejorNota()
        {
            double Average(int mastery)
            {
                World world = StartStudying(seed: 11);
                University().AdvanceWeeks(world, 2, out _);
                Character player = world.Player;
                foreach (string subject in player.SchoolRecord!.Mastery.Keys.ToList())
                {
                    player.SchoolRecord.Mastery[subject] = mastery;
                    player.SchoolRecord.LastStudiedWeek[subject] = world.Date.TotalWeeks + CareerStudies.WeeksPerSemester;
                }

                University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);
                return player.Studies!.LastMarks.Values.Average();
            }

            Assert.True(Average(100) > Average(10) + 1.0, "dominar los ramos debería subir el promedio del semestre en más de un punto.");
        }

        [Fact]
        public void SemestreSinPreguntas_ElDominioQuedaComoAnterior_YNoCuentaEnLasNotas()
        {
            World world = StartStudying();
            University().AdvanceWeeks(world, 2, out _);
            Character player = world.Player;
            player.Stats.Smarts = 100;
            Run(world, "jugador.estudiar_semestre(3)");
            Run(world, "jugador.semestres_aprobados = 9");
            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);

            // El 11º semestre de Ingeniería Civil solo tiene la memoria, que se evalúa con un proyecto.
            Assert.Equal(11, player.SchoolClass!.Level);
            Assert.Empty(StudyRules.Sets(world, Content, player));
            Assert.Empty(player.SchoolRecord!.Mastery);
            Assert.Contains("ramo.software.calculo", player.SchoolRecord.LastMastery.Keys);
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));
    }
}
