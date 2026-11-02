using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class AdoptionTests
    {
        private static LifeSession AdultSession(string extra = " }", ulong seed = 3)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + extra);
            var session = new LifeSession(content.Build(), seed);
            session.Advance(30 * GameDate.WeeksPerYear);
            return session;
        }

        /// <summary>El jugador con una pareja de su mismo género.</summary>
        private static Character SameGenderPartner(LifeSession session)
        {
            Character player = session.World.Player;
            return FamilyFactory.CreatePartner(session.World, session.Content, player, player.Gender);
        }

        [Fact]
        public void ParejaDelMismoGenero_Adopta_YAmbasPersonasSonSusPadres()
        {
            LifeSession session = AdultSession();
            Character player = session.World.Player;
            Character partner = SameGenderPartner(session);

            Character child = FamilyFactory.Adopt(session.World, session.Content, player);

            Assert.True(child.Adopted);
            Assert.True(child.IsChildOf(player.Id));
            Assert.True(child.IsChildOf(partner.Id));
            Assert.NotNull(child.SecondParentId);
            Assert.InRange(child.AgeInYears(session.World.Date), 0, FamilyFactory.MaxAdoptedChildAge);
            Assert.Equal(player.LastName.Split(' ')[0] + " " + partner.LastName.Split(' ')[0], child.LastName);
            Assert.Equal(new[] { child }, session.World.ChildrenOf(player.Id));
            Assert.Equal(new[] { child }, session.World.ChildrenOf(partner.Id));
        }

        [Fact]
        public void AdoptarSinPareja_LlevaLosApellidosDeQuienAdopta()
        {
            LifeSession session = AdultSession();

            Character child = FamilyFactory.Adopt(session.World, session.Content, session.World.Player);

            Assert.Equal(session.World.Player.LastName, child.LastName);
            Assert.Null(child.SecondParentId);
        }

        [Fact]
        public void HijoAdoptado_PuedeSerElHeredero()
        {
            LifeSession session = AdultSession();
            SameGenderPartner(session);
            Character child = FamilyFactory.Adopt(session.World, session.Content, session.World.Player);
            session.World.Player.Die(session.World.Date);

            Assert.Equal(new[] { child }, session.Heirs);
            session.ContinueWith(child.Id);
            Assert.Equal(child.Id, session.World.PlayerId);
        }

        [Fact]
        public void Lenguaje_Adoptar_EscribeEnElDiario_YElHijoSabeQueEsAdoptado()
        {
            LifeSession session = AdultSession();
            SameGenderPartner(session);
            var scope = new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false);

            Effect.Parse("jugador.adoptar()").Apply(scope);

            Character child = Assert.Single(session.World.ChildrenOf(session.World.PlayerId));
            Assert.Contains(session.World.Journal, e => e.TextKey == "diario.adopcion_hija" || e.TextKey == "diario.adopcion_hijo");
            Assert.True(Expression.Parse("jugador.hijos == 1").IsTrue(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true)));
            Assert.True(Expression.Parse("jugador.adoptado").IsTrue(new EventScope(session.World, session.Content, child, null, readOnly: true)));
        }

        [Fact]
        public void HijoNoJugadoConParejaDelMismoSexo_Adopta_YElJugadorConoceASuNieto()
        {
            LifeSession session = AdultSession(@", ""familia_no_jugada"": { ""pareja_anual"": 1, ""hijo_anual"": 1, ""separacion_anual"": 0, ""adopcion_anual"": 1, ""max_hijos"": 2 } }");
            Character child = FamilyFactory.CreateChild(session.World, session.Content, session.World.Player);
            child.Orientation = Orientation.Homosexual;

            session.Advance(25 * GameDate.WeeksPerYear);

            Assert.Equal(2, session.World.ChildrenOf(child.Id).Count);
            Assert.All(session.World.ChildrenOf(child.Id), grandchild => Assert.True(grandchild.Adopted));
            Assert.Contains(session.World.Journal, e => e.CharacterId == session.World.PlayerId && (e.TextKey == "diario.llega_nieta" || e.TextKey == "diario.llega_nieto"));
        }

        [Fact]
        public void ParejaDelMismoSexoEnChile_ConLosEventosReales_PuedeAdoptar()
        {
            int families = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                // Cis y gay o lesbiana: la pareja es de su mismo sexo y no pueden tener hijos biológicos.
                session.World.Player.Orientation = Orientation.Homosexual;
                session.World.Player.GenderIdentity = session.World.Player.Gender;
                int target = 50 * GameDate.WeeksPerYear;
                while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
                {
                    if (session.Pending != null)
                    {
                        session.Choose(session.Pending.AvailableOptions[0]);
                        continue;
                    }

                    session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
                }

                families += session.World.ChildrenOf(session.World.PlayerId).Any(c => c.Adopted) ? 1 : 0;
                Assert.DoesNotContain(session.World.ChildrenOf(session.World.PlayerId), c => !c.Adopted);
            }

            Assert.True(families >= 5, $"Solo {families} de 10 adoptaron.");
        }
    }
}
