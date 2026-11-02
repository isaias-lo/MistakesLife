using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class FamilyTests
    {
        private static ContentDatabase Content(string? mortalityRows = null)
        {
            string json = mortalityRows == null
                ? CountryTests.CountryJson
                : CountryTests.CountryJson.TrimEnd().TrimEnd('}') + @", ""mortalidad"": [" + mortalityRows + "] }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", json);
            return content.Build();
        }

        /// <summary>Un mundo donde el jugador ya es adulto (30 años).</summary>
        private static (World World, ContentDatabase Content) AdultLife(ulong seed = 3)
        {
            ContentDatabase content = Content();
            var session = new LifeSession(content, seed);
            session.Advance(30 * GameDate.WeeksPerYear);
            return (session.World, content);
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static bool Check(World world, ContentDatabase content, string condition) =>
            Expression.Parse(condition).IsTrue(new EventScope(world, content, world.Player, null, readOnly: true));

        [Fact]
        public void FormarPareja_CreaUnaPersonaDelMismoLugarYLosUneEnAmbosSentidos()
        {
            (World world, ContentDatabase content) = AdultLife();
            Character player = world.Player;

            Character partner = FamilyFactory.CreatePartner(world, content, player);

            Assert.Equal(partner.Id, player.PartnerId);
            Assert.Equal(player.Id, partner.PartnerId);
            Assert.NotEqual(player.Sex, partner.Sex);
            Assert.Equal(player.Residence!.CommuneId, partner.Residence!.CommuneId);
            Assert.InRange(partner.AgeInYears(world.Date), 26, 35);
        }

        [Fact]
        public void TerminarPareja_SeparaAAmbos_YLaOtraPersonaSigueEnElMundo()
        {
            (World world, ContentDatabase content) = AdultLife();
            Character partner = FamilyFactory.CreatePartner(world, content, world.Player);

            FamilyFactory.EndPartnership(world, world.Player);

            Assert.Null(world.Player.PartnerId);
            Assert.Null(partner.PartnerId);
            Assert.True(world.TryGetCharacter(partner.Id, out _));
        }

        [Fact]
        public void TenerHijo_ConPareja_LlevaElApellidoDelPadreYLuegoElDeLaMadre()
        {
            (World world, ContentDatabase content) = AdultLife();
            Character partner = FamilyFactory.CreatePartner(world, content, world.Player);

            Character child = FamilyFactory.CreateChild(world, content, world.Player);

            Character mother = world.Player.Sex == Sex.Female ? world.Player : partner;
            Character father = world.Player.Sex == Sex.Male ? world.Player : partner;
            Assert.Equal(mother.Id, child.MotherId);
            Assert.Equal(father.Id, child.FatherId);
            Assert.Equal(father.LastName.Split(' ')[0] + " " + mother.LastName.Split(' ')[0], child.LastName);
            Assert.Equal(world.Date, child.BirthDate);
            Assert.Equal(world.Player.Residence!.CommuneId, child.Residence!.CommuneId);
            Assert.Equal(new[] { child }, world.ChildrenOf(world.Player.Id));
        }

        [Fact]
        public void TenerHijo_SinPareja_LlevaLosApellidosDeQuienLoTiene()
        {
            (World world, ContentDatabase content) = AdultLife();

            Character child = FamilyFactory.CreateChild(world, content, world.Player);

            Assert.Equal(world.Player.LastName, child.LastName);
            Assert.True(child.MotherId == world.Player.Id || child.FatherId == world.Player.Id);
        }

        [Fact]
        public void Familia_ConLaMismaSemilla_SeRepiteIgual()
        {
            string Family(ulong seed)
            {
                (World world, ContentDatabase content) = AdultLife(seed);
                Character partner = FamilyFactory.CreatePartner(world, content, world.Player);
                Character child = FamilyFactory.CreateChild(world, content, world.Player);
                return $"{partner.FirstName} {partner.LastName} {partner.BirthDate} / {child.FirstName} {child.Sex} {child.Personality.Openness}";
            }

            Assert.Equal(Family(9), Family(9));
        }

        [Fact]
        public void PersonalidadHeredada_SeParecealPromedioDeLosPadresPeroNoEsCopia()
        {
            var high = new Personality(100, 100, 100, 100, 100);
            var random = new GameRandom(5);
            double sum = 0;
            for (int i = 0; i < 500; i++)
            {
                sum += Personality.Inherit(high, high, random).Openness;
            }

            // Mitad herencia (100) y mitad azar (en promedio 50): cerca de 75.
            Assert.InRange(sum / 500, 70, 80);
        }

        [Fact]
        public void Lenguaje_ParejaEHijos_SeLeenYSeCambianDesdeLosEventos()
        {
            (World world, ContentDatabase content) = AdultLife();

            Assert.True(Check(world, content, "jugador.pareja == nada && jugador.hijos == 0"));

            Run(world, content, "jugador.formar_pareja()");
            Run(world, content, "jugador.tener_hijo()");

            Assert.True(Check(world, content, "jugador.pareja != nada && jugador.pareja.edad >= 18 && jugador.hijos == 1"));
            Assert.Contains(world.Journal, e => e.CharacterId == world.Player.Id && (e.TextKey == "diario.nace_hija" || e.TextKey == "diario.nace_hijo"));

            Run(world, content, "jugador.terminar_pareja()");
            Assert.True(Check(world, content, "jugador.pareja == nada"));
        }

        [Fact]
        public void Lenguaje_FormarParejaEnUnaCondicion_EsUnError()
        {
            (World world, ContentDatabase content) = AdultLife();

            Assert.Throws<ScriptException>(() => Check(world, content, "jugador.formar_pareja() == nada"));
        }

        [Fact]
        public void MuerteDeLaPareja_QuedaEnElDiario_YElJugadorQuedaSinPareja()
        {
            // Solo mueren las mujeres: buscamos una partida donde el jugador sea hombre.
            ContentDatabase content = Content(@"{ ""edad"": 0, ""femenino"": 0, ""masculino"": 0 }, { ""edad"": 20, ""femenino"": 0, ""masculino"": 0 }, { ""edad"": 21, ""femenino"": 1, ""masculino"": 0 }");
            for (ulong seed = 1; seed < 50; seed++)
            {
                var session = new LifeSession(content, seed);
                if (session.World.Player.Sex != Sex.Male)
                {
                    continue;
                }

                // La madre (mayor de 20) muere apenas parte la vida; luego, a los 25, el jugador forma pareja.
                session.Advance(25 * GameDate.WeeksPerYear);
                session.Advance(25 * GameDate.WeeksPerYear);
                Character partner = FamilyFactory.CreatePartner(session.World, content, session.World.Player);
                Assert.True(partner.AgeInYears(session.World.Date) >= 21);

                session.Advance(10);

                Assert.False(partner.IsAlive);
                Assert.Null(session.World.Player.PartnerId);
                Assert.Contains(session.World.Journal, e => e.CharacterId == session.World.Player.Id && e.TextKey == "diario.muerte_pareja");
                return;
            }

            Assert.Fail("Ninguna partida tuvo un jugador hombre.");
        }

        [Fact]
        public void EventosDeFamiliaDeChile_EnMuchasVidas_DanHijosYParejasRazonables()
        {
            int lives = 80, withChildren = 0, totalChildren = 0;
            for (ulong seed = 1; seed <= (ulong)lives; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                var chooser = new GameRandom(seed).Fork("prueba.decisiones");
                int target = 50 * GameDate.WeeksPerYear;
                while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
                {
                    if (session.Pending != null)
                    {
                        session.Choose(chooser.Pick(session.Pending.AvailableOptions));
                        continue;
                    }

                    session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
                }

                int children = session.World.ChildrenOf(session.World.Player.Id).Count;
                totalChildren += children;
                withChildren += children > 0 ? 1 : 0;
            }

            // Chile: 1,16 hijos por mujer (INE 2023) y cerca de 3 de cada 4 personas tienen hijos.
            Assert.InRange((double)totalChildren / lives, 0.8, 1.8);
            Assert.InRange((double)withChildren / lives, 0.5, 0.85);
        }
    }
}
