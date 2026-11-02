using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class MortalityTests
    {
        /// <summary>El país de prueba de CountryTests, con una tabla de mortalidad propia.</summary>
        private static string CountryWithMortality(string rows) =>
            CountryTests.CountryJson.TrimEnd().TrimEnd('}') + @", ""mortalidad"": [" + rows + "] }";

        private static ContentDatabase Content(string countryJson)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", countryJson);
            return content.Build();
        }

        private static CountryDefinition Country(string rows) =>
            Content(CountryWithMortality(rows)).Countries.Single();

        private static Character Person(Sex sex, int health) =>
            new Character(1, "nombre", "apellido", sex, new GameDate(0), new Personality(50, 50, 50, 50, 50), new Stats(health, 80, 50));

        [Fact]
        public void Tabla_EntreDosEdades_SeInterpolaEnLineaRecta()
        {
            CountryDefinition country = Country(@"
              { ""edad"": 0, ""femenino"": 0.01, ""masculino"": 0.02 },
              { ""edad"": 10, ""femenino"": 0.03, ""masculino"": 0.06 }");

            Assert.Equal(0.02, country.AnnualDeathProbability(5, Sex.Female), 6);
            Assert.Equal(0.04, country.AnnualDeathProbability(5, Sex.Male), 6);
            Assert.Equal(0.03, country.AnnualDeathProbability(10, Sex.Female), 6);
        }

        [Fact]
        public void Tabla_DespuesDeLaUltimaEdad_UsaLaUltimaFila()
        {
            CountryDefinition country = Country(@"
              { ""edad"": 0, ""femenino"": 0.01, ""masculino"": 0.02 },
              { ""edad"": 10, ""femenino"": 0.03, ""masculino"": 0.06 }");

            Assert.Equal(0.06, country.AnnualDeathProbability(90, Sex.Male), 6);
        }

        [Fact]
        public void PaisSinTabla_NadieMuereDeCausasNaturales()
        {
            CountryDefinition country = Content(CountryTests.CountryJson).Countries.Single();

            Assert.Equal(0, country.AnnualDeathProbability(80, Sex.Male));
            Assert.Equal(0, MortalitySystem.WeeklyDeathProbability(country, Person(Sex.Male, 0), new GameDate(80 * GameDate.WeeksPerYear)));
        }

        [Fact]
        public void ProbabilidadSemanal_RepetidaUnAnio_DaLaProbabilidadAnualDeLaTabla()
        {
            CountryDefinition country = Country(@"{ ""edad"": 0, ""femenino"": 0.1, ""masculino"": 0.1 }");

            double weekly = MortalitySystem.WeeklyDeathProbability(country, Person(Sex.Female, 80), new GameDate(30 * GameDate.WeeksPerYear));
            double survivesYear = Math.Pow(1 - weekly, GameDate.WeeksPerYear);

            Assert.Equal(0.1, 1 - survivesYear, 6);
        }

        [Fact]
        public void SaludMuyMala_MultiplicaElRiesgo_HastaDiezVecesConSaludCero()
        {
            Assert.Equal(1, MortalitySystem.HealthRiskMultiplier(100));
            Assert.Equal(1, MortalitySystem.HealthRiskMultiplier(40));
            Assert.Equal(5.5, MortalitySystem.HealthRiskMultiplier(20), 6);
            Assert.Equal(10, MortalitySystem.HealthRiskMultiplier(0));
        }

        [Fact]
        public void Tabla_DesordenadaOConProbabilidadMayorQueUno_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryWithMortality(@"
              { ""edad"": 10, ""femenino"": 0.01, ""masculino"": 0.02 },
              { ""edad"": 5, ""femenino"": 1.5, ""masculino"": 0.02 }"));

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("de menor a mayor"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("entre 0 y 1"));
        }

        [Fact]
        public void TablaDeChile_DaLaEsperanzaDeVidaDelINE()
        {
            CountryDefinition chile = RepoPacks.Content.GetCountry("pais.chile");

            // INE 2011: 81,4 años las mujeres y 76,0 los hombres.
            Assert.InRange(LifeExpectancy(chile, Sex.Female), 80.5, 82.5);
            Assert.InRange(LifeExpectancy(chile, Sex.Male), 75, 77);
        }

        [Fact]
        public void SaludEsperada_Es85HastaLos40_YBajaHasta40ALos90()
        {
            Assert.Equal(85, AgingSystem.ExpectedHealth(0));
            Assert.Equal(85, AgingSystem.ExpectedHealth(40));
            Assert.Equal(80, AgingSystem.ExpectedHealth(50));
            Assert.Equal(60, AgingSystem.ExpectedHealth(75));
            Assert.Equal(40, AgingSystem.ExpectedHealth(90));
            Assert.Equal(40, AgingSystem.ExpectedHealth(105));
        }

        [Fact]
        public void Salud_ConLosAnios_SeAcercaALaEsperada()
        {
            // Sin tabla de mortalidad, para que nadie muera mientras se mide.
            var session = new LifeSession(Content(CountryTests.CountryJson), 3);
            Character player = session.World.Player;
            player.Stats.Health = 20;

            session.Advance(10 * GameDate.WeeksPerYear);

            // Se recupera unos 4 puntos al año: en 10 años, cerca de 40 puntos.
            Assert.InRange(player.Stats.Health, 45, 75);
        }

        [Fact]
        public void MuerteSegura_ElJugadorYSusPadresMueren_QuedaEnElDiarioYElTiempoSeDetiene()
        {
            ContentDatabase content = Content(CountryWithMortality(@"{ ""edad"": 0, ""femenino"": 1, ""masculino"": 1 }"));
            var session = new LifeSession(content, 7);

            int weeks = session.Advance(10);

            Character player = session.World.Player;
            Assert.Equal(1, weeks);
            Assert.False(player.IsAlive);
            Assert.Equal(Character.CauseNatural, player.DeathCauseId);
            Assert.False(session.World.GetCharacter(player.MotherId!.Value).IsAlive);
            var journal = session.World.Journal.Where(e => e.CharacterId == player.Id).Select(e => e.TextKey).ToList();
            Assert.Contains("diario.muerte", journal);
            Assert.Contains("diario.muerte_madre", journal);
            Assert.Contains("diario.muerte_padre", journal);
        }

        [Fact]
        public void VidaCompleta_ConLosPaquetesReales_TerminaEnLaMuerte_YConLaMismaSemillaSeRepiteIgual()
        {
            GameDate first = LiveUntilDeath(42);
            GameDate second = LiveUntilDeath(42);

            Assert.Equal(first, second);
        }

        [Fact]
        public void MuchasVidas_ConLosPaquetesReales_LaEdadDeMuerteSePareceALaReal()
        {
            // 60 vidas completas: el promedio debe quedar cerca de los ~79 años de la tabla,
            // con margen porque son pocas vidas y la salud también influye.
            double total = 0;
            const int lives = 60;
            for (ulong seed = 1; seed <= lives; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                LiveUntilDeath(session);
                total += session.World.Player.AgeInYears(session.World.Player.DeathDate!.Value);
            }

            Assert.InRange(total / lives, 70, 86);
        }

        private static GameDate LiveUntilDeath(ulong seed)
        {
            var session = new LifeSession(RepoPacks.Content, seed);
            LiveUntilDeath(session);
            return session.World.Player.DeathDate!.Value;
        }

        /// <summary>Juega eligiendo siempre la primera opción hasta que el jugador muere.</summary>
        private static void LiveUntilDeath(LifeSession session)
        {
            int maxWeeks = 120 * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Date.WeeksSince(session.World.Player.BirthDate) < maxWeeks)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(maxWeeks);
            }

            Assert.False(session.World.Player.IsAlive);
            Assert.InRange(session.World.Player.AgeInYears(session.World.Date), 0, 111);
        }

        /// <summary>Esperanza de vida al nacer según la tabla, con la fórmula de las tablas de vida.</summary>
        private static double LifeExpectancy(CountryDefinition country, Sex sex)
        {
            double alive = 1, years = 0;
            for (int age = 0; age <= 120 && alive > 0; age++)
            {
                double dies = alive * country.AnnualDeathProbability(age, sex);
                years += alive - (dies / 2);
                alive -= dies;
            }

            return years;
        }
    }
}
