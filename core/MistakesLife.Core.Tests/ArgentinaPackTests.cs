using MistakesLife.Core.Analysis;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Pruebas del paquete real de Argentina (Buenos Aires, La Matanza). Mientras
    /// no tenga sus eventos de vida (colegio, primer trabajo), nadie nace ahí por
    /// azar: se juega solo eligiéndolo.
    /// </summary>
    public class ArgentinaPackTests
    {
        private const string Argentina = "pais.argentina";

        private static bool Check(World world, string condition) =>
            Expression.Parse(condition).IsTrue(new EventScope(world, RepoPacks.Content, world.Player, null, readOnly: true));

        [Fact]
        public void Argentina_AlElegirla_NaceEnLaMatanza()
        {
            var session = new LifeSession(RepoPacks.Content, 1, Argentina);

            Assert.Equal(Argentina, session.World.Player.Residence!.CountryId);
            Assert.Equal("comuna.la_matanza", session.World.Player.Residence.CommuneId);
            Assert.Equal("La Matanza", session.Text("comuna.la_matanza"));
        }

        [Fact]
        public void Argentina_PorAzar_NadieNaceAhiTodavia()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                Assert.Equal("pais.chile", WorldFactory.CreateNewLife(seed, RepoPacks.Content).Player.Residence!.CountryId);
            }
        }

        [Fact]
        public void Argentina_TieneSusLeyes()
        {
            World world = new LifeSession(RepoPacks.Content, 1, Argentina).World;

            Assert.True(Check(world, "ley('ley.edad_voto') == 16"));
            Assert.True(Check(world, "ley('ley.aborto') == 'a_peticion'"));
            Assert.True(Check(world, "ley('ley.genero_registral_no_binario')"));
            Assert.True(Check(world, "ley('ley.edad_jubilacion_mujeres') == 60"));
        }

        [Fact]
        public void Argentina_HablaComoEnArgentina()
        {
            var argentina = new LifeSession(RepoPacks.Content, 1, Argentina);
            var chile = new LifeSession(RepoPacks.Content, 1);

            Assert.Contains("changa", argentina.Text("evento.pololito_vejez"));
            Assert.Contains("pololito", chile.Text("evento.pololito_vejez"));
            Assert.Contains("ANSES", argentina.Text("evento.jubilacion"));
        }

        [Fact]
        public void Argentina_JubilacionDeRepartoYPensionUniversalSoloSinPensionPropia()
        {
            PensionsDefinition pensions = RepoPacks.Content.GetCountry(Argentina).Economy!.Pensions!;

            Assert.NotNull(pensions.PayAsYouGo);
            Assert.Null(pensions.IndividualAccount);
            Assert.Equal(30, pensions.PayAsYouGo!.MinimumYears);
            Assert.Equal(pensions.UniversalPension!.Amount, Economy.PensionCalculator.UniversalPension(0, pensions.UniversalPension));
            Assert.Equal(0m, Economy.PensionCalculator.UniversalPension(pensions.PayAsYouGo.Minimum, pensions.UniversalPension));
        }

        [Fact]
        public void Argentina_VidasHastaLos20_SinErroresYConInflacionAlta()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 10, finalAge: 20, countryId: Argentina);

            Assert.Equal(10, report.Lives);
            Assert.True(report.AverageAnnualInflation > 0.15, "La inflación de Argentina debería ser alta: " + report.AverageAnnualInflation);
        }

        [Fact]
        public void Argentina_GuardarYCargar_SigueEnArgentinaConSuSueldoPromedio()
        {
            var session = new LifeSession(RepoPacks.Content, 3, Argentina);
            session.World.Player.PensionSalaryBase = 654321.5m;

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.Equal(Argentina, loaded.World.Player.Residence!.CountryId);
            Assert.Equal(654321.5m, loaded.World.Player.PensionSalaryBase);
            Assert.Contains("changa", loaded.Text("evento.pololito_vejez"));
        }
    }
}
