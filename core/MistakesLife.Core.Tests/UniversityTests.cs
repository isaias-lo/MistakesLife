using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R5a: la universidad, el CFT y el instituto por dentro (sección, ramos y notas).</summary>
    public class UniversityTests
    {
        private const string Civil = "carrera.software.nivel.ingenieria_civil_computacion";

        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida en Chile a los 18 años que empieza a estudiar esa carrera.</summary>
        private static World StartStudying(string levelId = Civil, ulong seed = 3)
        {
            World world = WorldFactory.CreateNewLife(seed, Content, "pais.chile");
            new WeekCycle().AdvanceWeeks(world, (18 * GameDate.WeeksPerYear) + 10 - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, $"estudiar('{levelId}')");
            return world;
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static WeekCycle University() =>
            new WeekCycle().Register(new CareerSystem()).Register(new UniversitySystem(Content));

        [Fact]
        public void Seccion_AlEmpezarLaCarrera_TieneDiezCompanerosConNombreYUnProfe()
        {
            World world = StartStudying();
            University().AdvanceWeeks(world, 1, out _);
            Character player = world.Player;

            Assert.True(UniversityRules.InSection(player));
            Assert.Equal(Civil, player.SchoolClass!.StageId);
            Assert.Equal(1, player.SchoolClass.Level);
            var mates = SchoolRules.Classmates(world, player);
            Assert.Equal(UniversityRules.NamedClassmates, mates.Count);
            Assert.All(mates, m => Assert.False(string.IsNullOrEmpty(m.FirstName)));
            Assert.All(mates, m => Assert.InRange(m.AgeInYears(world.Date), 16, 31));
            Character? professor = SchoolRules.Teacher(world, player);
            Assert.NotNull(professor);
            Assert.InRange(professor!.AgeInYears(world.Date), UniversityRules.MinProfessorAge - 1, UniversityRules.MaxProfessorAge + 1);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.seccion.nueva");
        }

        [Fact]
        public void Semestre_AlTerminar_PoneNotaACadaRamoYPasaAlSiguienteConProfeNuevo()
        {
            World world = StartStudying();
            Character player = world.Player;
            player.Stats.Smarts = 100;
            University().AdvanceWeeks(world, 1, out _);
            int firstProfessor = player.SchoolClass!.TeacherId!.Value;

            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);

            CareerStudies studies = player.Studies!;
            Assert.Equal(1, studies.LastMarksSemester);
            Assert.Equal(
                new[] { "ramo.software.algebra", "ramo.software.calculo", "ramo.software.introduccion_ingenieria", "ramo.software.programacion" },
                studies.LastMarks.Keys.ToArray());
            Assert.All(studies.LastMarks.Values, m => Assert.InRange(m, 1.0, 7.0));
            Assert.Single(studies.SemesterAverages);
            Assert.Equal(studies.CompletedSemesters, studies.GradedSemesters);
            Assert.Equal(studies.CompletedSemesters + 1, player.SchoolClass.Level);
            Assert.Equal(firstProfessor, player.SchoolClass.TeacherId); // el profe cambia cada año
            Assert.InRange(SchoolRules.Classmates(world, player).Count, UniversityRules.MinNamedClassmates, UniversityRules.NamedClassmates);

            Run(world, "jugador.estudiar_semestre(3)");
            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);
            Assert.Equal(2, studies.CompletedSemesters);
            Assert.Equal(3, player.SchoolClass.Level);
            Assert.NotEqual(firstProfessor, player.SchoolClass.TeacherId);
        }

        [Fact]
        public void Semestre_ConUnRamoRojo_NoCuentaYSeVuelveACursar()
        {
            World world = StartStudying();
            Character player = world.Player;
            player.Stats.Smarts = 0;
            University().AdvanceWeeks(world, 1, out _);
            Run(world, "jugador.estudiar_semestre(-3)");

            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);

            CareerStudies studies = player.Studies!;
            Assert.Contains(studies.LastMarks.Values, m => m < 4.0);
            Assert.Equal(0, studies.CompletedSemesters);
            Assert.Equal(0, studies.GradedSemesters);
            Assert.Equal(0, studies.Preparation);
            Assert.Equal(1, player.SchoolClass!.Level);
            Assert.Equal(1, studies.SemestersTaken); // lo cursó aunque no lo aprobó
            Assert.Contains(world.Journal, j => j.TextKey == "diario.estudios.ramo_reprobado");
        }

        private static bool Holds(World world, string condition) =>
            Scripting.Expression.Parse(condition).IsTrue(new EventScope(world, Content, world.Player, null, readOnly: true));

        private static void Choose(World world, string eventId, string optionKey)
        {
            EventOptionDefinition option = Content.GetEvent(eventId).Options.Single(o => o.TextKey == optionKey);
            foreach (string effect in option.Effects)
            {
                Run(world, effect);
            }
        }

        [Fact]
        public void SinGratuidad_ConCae_DebeElArancelDeReferenciaYPagaLaDiferenciaCadaMes()
        {
            World world = StartStudying();
            decimal before = world.Player.Money;

            Choose(world, "evento.sin_gratuidad", "opcion.financiar.credito");

            Assert.True(Holds(world, "jugador.deuda_de('deuda.chile.cae') >= 3000000"));
            Assert.Equal("servicio.arancel.brecha_universidad", world.Player.Services["servicio.arancel"]);
            Assert.True(world.Player.Money < before); // la matrícula
            Assert.True(Holds(world, "tarifa('servicio.arancel.brecha_universidad') > 0"));
        }

        [Fact]
        public void Gratuidad_AlPasarseDeLaDuracionFormal_SeAcabaYLlegaElMomentoDeMarzo()
        {
            World world = StartStudying("carrera.software.nivel.analista_programador");
            Run(world, "jugador.marcar('gratuidad')");
            Run(world, "jugador.contratar('servicio.arancel.gratuidad')");
            world.Player.Stats.Smarts = 0;
            string condition = Content.GetEvent("evento.estudios.fin_gratuidad").Conditions!;
            University().AdvanceWeeks(world, 1, out _);

            // Cinco semestres cursados (la duración formal) sin haber aprobado todos.
            for (int i = 0; i < 5; i++)
            {
                Run(world, "jugador.estudiar_semestre(-3)");
                University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);
            }

            Assert.Equal(5, world.Player.Studies!.SemestersTaken);
            Assert.True(world.Player.Studies.CompletedSemesters < 5);
            Assert.True(Holds(world, condition));

            Choose(world, "evento.estudios.fin_gratuidad", "opcion.fin_gratuidad.pagar");
            Assert.Equal("servicio.arancel.mitad_instituto", world.Player.Services["servicio.arancel"]);
            Assert.False(Holds(world, condition));
        }

        [Fact]
        public void Arancel_SoloSeCobraMientrasEstudia()
        {
            World world = StartStudying();
            Run(world, "jugador.contratar('servicio.arancel.pago_universidad')");
            string charged = MistakesLife.Core.Economy.ServiceRules.ServicesFor(Content, world.Player).Single(s => s.Id == "servicio.arancel").Conditions!;
            Assert.True(Holds(world, charged));

            Run(world, "jugador.desertar()");

            Assert.False(Holds(world, charged));
        }

        [Fact]
        public void EstudiarSemestre_SumaPreparacionDeMenosTresATres()
        {
            World world = StartStudying();
            Run(world, "jugador.estudiar_semestre(2)");
            Run(world, "jugador.estudiar_semestre(2)");
            Assert.Equal(UniversityRules.MaxPreparation, world.Player.Studies!.Preparation);

            Run(world, "jugador.estudiar_semestre(-9)");
            Assert.Equal(-UniversityRules.MaxPreparation, world.Player.Studies.Preparation);
        }

        [Fact]
        public void Seccion_AlDejarDeEstudiar_SeCierraYElColegioNoLaBorraMientrasEstudia()
        {
            World world = StartStudying();
            new WeekCycle().Register(new SchoolSystem(Content)).Register(new CareerSystem()).Register(new UniversitySystem(Content)).AdvanceWeeks(world, 4, out _);
            Assert.True(UniversityRules.InSection(world.Player));

            Run(world, "jugador.desertar()");
            University().AdvanceWeeks(world, 1, out _);

            Assert.Null(world.Player.SchoolClass);
        }

        [Fact]
        public void Carreras_CadaSemestreDeCadaNivelTieneRamos()
        {
            foreach (CareerDefinition career in Content.Careers)
            {
                foreach (CareerLevelDefinition level in career.Levels)
                {
                    var studies = new CareerStudies(career.Id, level.Id, level.Institution, level.Semesters);
                    for (int semester = 1; semester <= level.Semesters; semester++)
                    {
                        Assert.NotEmpty(UniversityRules.Courses(Content, studies, semester));
                    }
                }
            }
        }

        [Fact]
        public void CarreraConRamosIncompletos_SeRechaza()
        {
            const string career = @"{ ""id"": ""carrera.prueba"", ""niveles"": [
              { ""id"": ""carrera.prueba.nivel.tecnico"", ""institucion"": ""cft"", ""semestres"": 2 } ],
              ""ramos"": [ { ""id"": ""ramo.prueba.uno"", ""desde"": 1 }, { ""id"": ""otra.cosa"", ""desde"": 0 } ] }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson);
            content.AddCareerJson("pack.prueba", "career.json", career);

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("el semestre 2 no tiene ramos"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("ramo."));
        }

        [Fact]
        public void Libreta_EnLaU_MuestraLosRamosConSuNumeroYDespuesLasNotas()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, (18 * GameDate.WeeksPerYear) + 10 - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, $"estudiar('{Civil}')");
            University().AdvanceWeeks(world, 1, out _);
            var presenter = new LifePresenter(session);

            ReportCardView card = presenter.ReportCard()!;
            Assert.False(card.Finals);
            Assert.Contains(card.Subjects, s => s.Name == "Cálculo I");
            Assert.Contains("semestre", presenter.SchoolClass()!.Name);
            Assert.Contains("Desarrollo de software", presenter.SchoolClass()!.Name);

            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester, out _);

            card = presenter.ReportCard()!;
            Assert.True(card.Finals);
            Assert.Contains(card.Subjects, s => s.Name == "Cálculo I" && s.Average.Length > 0);
            Assert.NotNull(card.General);
        }

        [Fact]
        public void Guardado_ConservaLaSeccionYLasNotasDelSemestre()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, (18 * GameDate.WeeksPerYear) + 10 - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, $"estudiar('{Civil}')");
            University().AdvanceWeeks(world, CareerStudies.WeeksPerSemester + 1, out _);
            Run(world, "jugador.estudiar_semestre(2)");
            CareerStudies before = world.Player.Studies!;

            Character after = LifeSession.LoadFromBytes(Content, session.SaveToBytes()).World.Player;

            Assert.True(UniversityRules.InSection(after));
            Assert.Equal(before.GradedSemesters, after.Studies!.GradedSemesters);
            Assert.Equal(2, after.Studies.Preparation);
            Assert.Equal(before.LastMarksSemester, after.Studies.LastMarksSemester);
            Assert.Equal(before.LastMarks, after.Studies.LastMarks);
            Assert.Equal(before.SemesterAverages, after.Studies.SemesterAverages);
            Assert.Equal(world.Player.SchoolClass!.ClassmateIds, after.SchoolClass!.ClassmateIds);
        }

        [Fact]
        public void Vida_EnLaU_SeJuegaUnSemestreConCompanerosConNombre()
        {
            // Busca una vida que entre a estudiar y la juega hasta cerrar su primer semestre.
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var session = new LifeSession(Content, seed, "pais.chile");
                session.TimeMachine.Enabled = false;
                var presenter = new LifePresenter(session);
                int withMate = 0;
                while (session.World.Player.IsAlive && session.PlayerAgeInYears < 26)
                {
                    Character player = session.World.Player;
                    if (session.Pending != null)
                    {
                        EventDefinition ev = Content.GetEvent(session.Pending.EventId);
                        if (UniversityRules.InSection(player) && (ev.With == "companero" || ev.With == "profe"))
                        {
                            Assert.NotNull(session.Pending.PersonId);
                            withMate++;
                        }

                        session.Choose(session.Pending.AvailableOptions[0]);
                        continue;
                    }

                    if (player.Studies != null && player.Studies.SemesterAverages.Count > 0)
                    {
                        Assert.InRange(presenter.SchoolClass()!.Classmates.Count, UniversityRules.MinNamedClassmates, UniversityRules.NamedClassmates);
                        Assert.True(presenter.ReportCard()!.Finals);
                        Assert.True(withMate > 0);
                        return;
                    }

                    session.Advance(1);
                }
            }

            Assert.Fail("ninguna vida entró a estudiar y cerró un semestre");
        }
    }
}
