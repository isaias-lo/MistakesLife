using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R3b: ramos con notas de 1,0 a 7,0, pruebas que se preparan, libreta y repetir.</summary>
    public class GradeTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static CountryDefinition Chile => Content.GetCountry("pais.chile");

        private static GradeScaleDefinition Scale => Chile.GradeScale!;

        private static EducationStageDefinition Basica => Chile.EducationStages.Single(s => s.Id == "etapa.chile.basica");

        private static WeekCycle School() =>
            new WeekCycle().Register(new SchoolSystem(Content)).Register(new EducationSystem(Content)).Register(new GradeSystem(Content));

        private static World LiveUntil(int age, int weekOfYear)
        {
            World world = WorldFactory.CreateNewLife(3, Content, "pais.chile");
            School().AdvanceWeeks(world, (age * GameDate.WeeksPerYear) + weekOfYear - world.Player.AgeInWeeks(world.Date), out _);
            return world;
        }

        private static SortedDictionary<string, double> Finals(params double[] values)
        {
            var finals = new SortedDictionary<string, double>();
            for (int i = 0; i < values.Length; i++)
            {
                finals["ramo." + i] = values[i];
            }

            return finals;
        }

        [Fact]
        public void Nota_SiempreVaDe1a7ConUnDecimal()
        {
            World world = LiveUntil(9, 20);
            var random = new GameRandom(5);
            for (int i = 0; i < 500; i++)
            {
                double mark = GradeRules.Mark(world, Content, world.Player, Scale, string.Empty, i % 4, random);
                Assert.InRange(mark, 1.0, 7.0);
                Assert.Equal(mark, System.Math.Round(mark, 1));
            }
        }

        [Fact]
        public void Nota_PrepararLaPruebaSubeLaNota()
        {
            World world = LiveUntil(9, 20);
            double Mean(int preparation)
            {
                var random = new GameRandom(9);
                return Enumerable.Range(0, 400).Average(_ => GradeRules.Mark(world, Content, world.Player, Scale, string.Empty, preparation, random));
            }

            Assert.True(Mean(3) - Mean(0) > 0.6, "Prepararse harto sube la nota casi un punto.");
        }

        [Theory]
        [InlineData(new[] { 4.0, 5.0, 6.0 }, true)]
        [InlineData(new[] { 3.9, 4.6, 5.0 }, true)]   // un rojo, promedio 4,5
        [InlineData(new[] { 3.9, 4.4, 5.0 }, false)]  // un rojo, promedio 4,4
        [InlineData(new[] { 3.5, 3.9, 7.0, 7.0, 7.0 }, true)]   // dos rojos, promedio 5,7
        [InlineData(new[] { 3.5, 3.9, 6.0, 6.0, 5.0 }, false)]  // dos rojos, promedio 4,9
        [InlineData(new[] { 3.0, 3.0, 3.0, 7.0, 7.0, 7.0 }, false)] // tres rojos
        public void PasarDeCurso_ComoDiceElDecreto67(double[] finals, bool passes)
        {
            Assert.Equal(passes, GradeRules.Passes(Basica, Finals(finals), Scale));
        }

        [Fact]
        public void Colegio_DuranteElAno_LlegaNotaEnCadaRamoDelCurso()
        {
            World world = LiveUntil(8, 40);
            SchoolRecord record = world.Player.SchoolRecord!;

            Assert.Equal(3, world.Player.SchoolClass!.Level);
            Assert.Contains("ramo.chile.matematica", record.Marks.Keys);
            Assert.Contains("ramo.chile.lenguaje", record.Marks.Keys);
            Assert.DoesNotContain("ramo.chile.ingles", record.Marks.Keys); // Inglés empieza en 5º
            Assert.True(record.Marks.Values.Sum(m => m.Count) > 40, "En un año hay muchas notas.");
        }

        [Fact]
        public void FinDeAno_EnVeranoSeCierraElAnoConSuPromedio()
        {
            World world = LiveUntil(10, 2);
            SchoolRecord record = world.Player.SchoolRecord!;

            Assert.Empty(record.Marks);
            Assert.Equal(4, record.YearAverages.Count); // 1º a 4º básico
            Assert.NotEmpty(record.LastFinals);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.notas.paso" || j.TextKey == "diario.notas.excelente" || j.TextKey == "diario.notas.repitio");
        }

        [Fact]
        public void Repetir_ConRojosEnVariosRamos_VuelveAlMismoCursoConLosMasChicos()
        {
            World world = LiveUntil(10, 48);
            int level = world.Player.SchoolClass!.Level;
            var oldMates = world.Player.SchoolClass.ClassmateIds.ToList();
            SchoolRecord record = world.Player.SchoolRecord!;
            foreach (var marks in record.Marks.Values)
            {
                marks.Clear();
                marks.Add(2.5);
            }

            School().AdvanceWeeks(world, 18, out _); // vacaciones y vuelta a clases en marzo

            Assert.Equal(1, record.RepeatedYears);
            Assert.True(world.Player.HasFlag("repitio_curso"));
            Assert.Contains(world.Journal, j => j.TextKey == "diario.notas.repitio");
            Assert.Equal(level, world.Player.SchoolClass!.Level);
            Assert.Equal(11, world.Player.AgeInYears(world.Date));
            Assert.Equal(10, SchoolRules.SchoolAge(world.Player, world.Date));
            Assert.DoesNotContain(world.Player.SchoolClass.ClassmateIds, id => oldMates.Contains(id));
            Assert.All(world.Player.SchoolClass.ClassmateIds, id => Assert.True(SchoolRules.SameCohort(world.Player, world.GetCharacter(id))));
        }

        [Fact]
        public void Guardado_ConservaLasNotas()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            while (session.World.Player.AgeInYears(session.World.Date) < 10 || session.World.Player.SchoolRecord?.Marks.Count == 0)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(GameDate.WeeksPerYear);
            }

            SchoolRecord before = session.World.Player.SchoolRecord!;
            SchoolRecord after = LifeSession.LoadFromBytes(Content, session.SaveToBytes()).World.Player.SchoolRecord!;

            Assert.Equal(before.YearAverages, after.YearAverages);
            Assert.Equal(before.Marks.Keys, after.Marks.Keys);
            Assert.Equal(before.Marks.Values.SelectMany(m => m), after.Marks.Values.SelectMany(m => m));
            Assert.Equal(before.LastFinals, after.LastFinals);
            Assert.Equal(before.TestSubjectId, after.TestSubjectId);
            Assert.Equal(before.TestMark, after.TestMark);
        }

        [Fact]
        public void Libreta_EscribeLasNotasComoEnChile()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            var presenter = new LifePresenter(session);

            Assert.Equal("5,8", presenter.MarkText(5.8));
            Assert.Equal("4,0", presenter.MarkText(4));
        }
    }
}
