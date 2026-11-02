using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class LegacyTests
    {
        private const string PriorLife = @", ""vida_previa"": [ { ""edad"": 18, ""marcas"": [""egresado_media""] } ] }";

        /// <summary>Partida de prueba (sin tabla de mortalidad) con el jugador de 30 años.</summary>
        private static LifeSession AdultSession()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + PriorLife);
            var session = new LifeSession(content.Build(), 3);
            session.Advance(30 * GameDate.WeeksPerYear);
            return session;
        }

        private static void KillPlayer(LifeSession session) => session.World.Player.Die(session.World.Date);

        [Fact]
        public void MientrasElJugadorVive_NoHayHerederos()
        {
            LifeSession session = AdultSession();
            FamilyFactory.CreateChild(session.World, session.Content, session.World.Player);

            Assert.Empty(session.Heirs);
            Assert.False(session.IsOver);
            Assert.Throws<InvalidOperationException>(() => session.ContinueWith(session.World.PlayerId));
        }

        [Fact]
        public void MuereSinHijos_LaPartidaTermina()
        {
            LifeSession session = AdultSession();
            KillPlayer(session);

            Assert.Empty(session.Heirs);
            Assert.True(session.IsOver);
        }

        [Fact]
        public void MuereConHijos_SeSigueConElHijoElegido_EnElMismoMundo()
        {
            LifeSession session = AdultSession();
            Character parent = session.World.Player;
            Character first = FamilyFactory.CreateChild(session.World, session.Content, parent);
            session.Advance(2 * GameDate.WeeksPerYear);
            Character second = FamilyFactory.CreateChild(session.World, session.Content, parent);
            KillPlayer(session);

            Assert.Equal(new[] { first, second }, session.Heirs);
            Assert.False(session.IsOver);

            session.ContinueWith(second.Id);

            Assert.Equal(second.Id, session.World.PlayerId);
            Assert.Equal(2, session.World.Generation);
            Assert.Equal(new[] { parent.Id }, session.World.FormerPlayers);
            Assert.True(session.World.TryGetCharacter(parent.Id, out _));
            Assert.Contains(session.World.Journal, e => e.CharacterId == second.Id && e.TextKey == "diario.legado");

            int weeks = session.Advance(10);
            Assert.Equal(10, weeks);
            Assert.True(session.World.Player.IsAlive);
        }

        [Fact]
        public void Legado_LasSemillasHeredablesPasanAlHijo_YLasDemasSeQuedan()
        {
            LifeSession session = AdultSession();
            Character parent = session.World.Player;
            Character child = FamilyFactory.CreateChild(session.World, session.Content, parent);
            var heritable = new SeedDefinition("semilla.deuda_familiar", SeedPolarity.Negative, 50, 1000) { Heritable = true };
            var personal = new SeedDefinition("semilla.secreto", SeedPolarity.Negative, 50, 1000);
            Seed debt = session.World.Seeds.Plant(heritable, parent.Id, session.World.Date, 30);
            Seed secret = session.World.Seeds.Plant(personal, parent.Id, session.World.Date, 30);
            KillPlayer(session);

            session.ContinueWith(child.Id);

            Assert.Equal(child.Id, debt.OwnerId);
            Assert.Equal(parent.Id, secret.OwnerId);
        }

        [Fact]
        public void HijoAdulto_RecibeLaVidaPreviaDeSuPais_YUnNinoNo()
        {
            LifeSession session = AdultSession();
            Character parent = session.World.Player;
            Character adult = FamilyFactory.CreateChild(session.World, session.Content, parent);
            session.Advance(20 * GameDate.WeeksPerYear);
            Character young = FamilyFactory.CreateChild(session.World, session.Content, parent);
            KillPlayer(session);

            session.ContinueWith(adult.Id);

            Assert.True(adult.HasFlag("egresado_media"));
            Assert.False(young.HasFlag("egresado_media"));
        }

        [Fact]
        public void VidaPrevia_SinMarcas_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + @", ""vida_previa"": [ { ""edad"": 18, ""marcas"": [] } ] }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Message.Contains("al menos una marca"));
        }

        [Fact]
        public void LinajeConLosPaquetesReales_VariasGeneraciones_SeRepiteIgualConLaMismaSemilla()
        {
            Assert.Equal(Lineage(7), Lineage(7));
        }

        /// <summary>Juega hasta 3 generaciones eligiendo siempre la primera opción y el hijo mayor.</summary>
        private static string Lineage(ulong seed)
        {
            var session = new LifeSession(RepoPacks.Content, seed);
            int weeks = 0;
            while (!session.IsOver && session.World.Generation <= 3 && weeks < 400 * GameDate.WeeksPerYear)
            {
                if (!session.World.Player.IsAlive)
                {
                    session.ContinueWith(session.Heirs[0].Id);
                }
                else if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }
                else
                {
                    weeks += session.Advance(10 * GameDate.WeeksPerYear);
                }
            }

            return string.Join(" / ", session.World.FormerPlayers.Append(session.World.PlayerId).Select(id => session.World.GetCharacter(id).ToString()));
        }
    }
}
