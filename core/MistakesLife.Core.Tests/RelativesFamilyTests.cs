using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class RelativesFamilyTests
    {
        private const string SureFamily = @", ""familia_no_jugada"": { ""pareja_anual"": 1, ""hijo_anual"": 1, ""separacion_anual"": 0, ""max_hijos"": 2 } }";

        /// <summary>Partida de prueba (sin muertes) con el jugador de 30 años y un hijo recién nacido.</summary>
        private static (LifeSession Session, Character Child) SessionWithChild(string familyJson)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + familyJson);
            var session = new LifeSession(content.Build(), 3);
            session.Advance(30 * GameDate.WeeksPerYear);
            Character child = FamilyFactory.CreateChild(session.World, session.Content, session.World.Player);
            return (session, child);
        }

        [Fact]
        public void HijoNoJugado_AlSerAdulto_FormaParejaYTieneHijos_YElJugadorTieneNietos()
        {
            (LifeSession session, Character child) = SessionWithChild(SureFamily);

            session.Advance(17 * GameDate.WeeksPerYear);
            Assert.Null(child.PartnerId);

            session.Advance(3 * GameDate.WeeksPerYear);

            Assert.NotNull(child.PartnerId);
            Assert.Equal(2, session.World.ChildrenOf(child.Id).Count);
            Assert.Contains(session.World.Journal, e => e.CharacterId == session.World.PlayerId && (e.TextKey == "diario.nace_nieta" || e.TextKey == "diario.nace_nieto"));
        }

        [Fact]
        public void Descendientes_IncluyenHijosYNietos_PeroNoAlJugadorNiALasParejas()
        {
            (LifeSession session, Character child) = SessionWithChild(SureFamily);
            session.Advance(20 * GameDate.WeeksPerYear);

            var descendants = RelativesFamilySystem.NonPlayedDescendants(session.World);

            Assert.Contains(child, descendants);
            Assert.All(session.World.ChildrenOf(child.Id), grandchild => Assert.Contains(grandchild, descendants));
            Assert.DoesNotContain(session.World.Player, descendants);
            Assert.DoesNotContain(session.World.GetCharacter(child.PartnerId!.Value), descendants);
        }

        [Fact]
        public void PaisSinFamiliaNoJugada_LosHijosNoFormanFamilia()
        {
            (LifeSession session, Character child) = SessionWithChild(" }");

            session.Advance(30 * GameDate.WeeksPerYear);

            Assert.Null(child.PartnerId);
            Assert.Empty(session.World.ChildrenOf(child.Id));
        }

        [Fact]
        public void FamiliaNoJugada_ConProbabilidadMayorQueUno_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + @", ""familia_no_jugada"": { ""pareja_anual"": 2 } }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("familia_no_jugada"));
        }

        [Fact]
        public void LegadoConLosPaquetesReales_ElHijoAdultoPuedeLlegarConSuPropiaFamilia()
        {
            // En varias vidas, al menos un heredero adulto llega con pareja o hijos.
            bool someHeirHasFamily = false;
            for (ulong seed = 1; seed <= 20 && !someHeirHasFamily; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                while (session.World.Player.IsAlive)
                {
                    if (session.Pending != null)
                    {
                        session.Choose(session.Pending.AvailableOptions[0]);
                        continue;
                    }

                    session.Advance(10 * GameDate.WeeksPerYear);
                }

                Character? heir = session.Heirs.FirstOrDefault(h => h.AgeInYears(session.World.Date) >= 30);
                someHeirHasFamily = heir != null && (heir.PartnerId.HasValue || session.World.ChildrenOf(heir.Id).Count > 0);
            }

            Assert.True(someHeirHasFamily);
        }
    }
}
