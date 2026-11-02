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
    public class DiversityTests
    {
        private const string HalfAndHalf = @", ""diversidad"": {
            ""orientaciones"": { ""heterosexual"": 1, ""homosexual"": 1 },
            ""identidades"": { ""cis"": 1, ""trans"": 1 } } }";

        private static ContentDatabase Content(string extra)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + extra);
            return content.Build();
        }

        private static LifeSession AdultSession(ContentDatabase content, ulong seed)
        {
            var session = new LifeSession(content, seed);
            session.Advance(30 * GameDate.WeeksPerYear);
            return session;
        }

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        private static bool Check(LifeSession session, string condition) =>
            Expression.Parse(condition).IsTrue(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true));

        [Fact]
        public void PaisSinDiversidad_TodosNacenHeterosexualesYConElGeneroDeSuSexo()
        {
            World world = WorldFactory.CreateNewLife(4, Content(" }"));

            Assert.All(world.Characters, c =>
            {
                Assert.Equal(Orientation.Heterosexual, c.Orientation);
                Assert.Equal(c.Gender, c.GenderIdentity);
                Assert.Equal(c.Sex == Sex.Female ? Gender.Female : Gender.Male, c.Gender);
            });
        }

        [Fact]
        public void AgregarDiversidad_NoCambiaElRestoDeLaPartida()
        {
            World plain = WorldFactory.CreateNewLife(4, Content(" }"));
            World diverse = WorldFactory.CreateNewLife(4, Content(HalfAndHalf));

            Assert.Equal(plain.Characters.Select(c => c.ToString()), diverse.Characters.Select(c => c.ToString()));
        }

        [Fact]
        public void ConLosPaquetesReales_LasProporcionesSeParecenALasEncuestasDeChile()
        {
            int people = 0, lgb = 0, notCis = 0;
            for (ulong seed = 1; seed <= 2000; seed++)
            {
                Character player = WorldFactory.CreateNewLife(seed, RepoPacks.Content).Player;
                people++;
                lgb += player.Orientation == Orientation.Heterosexual ? 0 : 1;
                notCis += player.GenderIdentity == player.Gender ? 0 : 1;
            }

            // Ipsos 2023, Chile: cerca de 9% no heterosexual y 3% trans o no binario.
            Assert.InRange((double)lgb / people, 0.06, 0.12);
            Assert.InRange((double)notCis / people, 0.01, 0.05);
        }

        [Fact]
        public void PadresDelPrimerJugador_SonHeterosexualesOBisexuales()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                World world = WorldFactory.CreateNewLife(seed, Content(HalfAndHalf));
                Assert.All(world.Characters.Where(c => c.Id != world.PlayerId), parent =>
                    Assert.True(parent.Orientation == Orientation.Heterosexual || parent.Orientation == Orientation.Bisexual));
            }
        }

        [Fact]
        public void PersonaHomosexual_FormaParejaDeSuMismoGenero_YLaParejaCalza()
        {
            LifeSession session = AdultSession(Content(HalfAndHalf), 3);
            Character player = session.World.Player;
            player.Orientation = Orientation.Homosexual;
            player.GenderIdentity = player.Gender;

            Character partner = FamilyFactory.CreatePartner(session.World, session.Content, player);

            Assert.Equal(player.Gender, partner.Gender);
            Assert.Equal(player.Sex, partner.Sex);
            Assert.True(partner.Orientation == Orientation.Homosexual || partner.Orientation == Orientation.Bisexual);
        }

        [Fact]
        public void PersonaTrans_LaParejaSeEligeSegunElGeneroQueSiente()
        {
            LifeSession session = AdultSession(Content(HalfAndHalf), 3);
            Character player = session.World.Player;
            Gender felt = player.Gender == Gender.Female ? Gender.Male : Gender.Female;
            player.GenderIdentity = felt;
            player.Orientation = Orientation.Heterosexual;

            Character partner = FamilyFactory.CreatePartner(session.World, session.Content, player);

            // Siente el otro género y le atrae el contrario a ese: el mismo sexo de su registro.
            Assert.Equal(player.Gender, partner.Gender);
        }

        [Fact]
        public void PersonaBisexual_LaMayoriaDeSusParejasSonDeOtroGenero_PeroNoTodas()
        {
            int other = 0, tries = 300;
            for (ulong seed = 1; seed <= (ulong)tries; seed++)
            {
                LifeSession session = AdultSession(Content(HalfAndHalf), seed);
                Character player = session.World.Player;
                player.Orientation = Orientation.Bisexual;
                player.GenderIdentity = player.Gender;
                other += FamilyFactory.CreatePartner(session.World, session.Content, player).Gender != player.Gender ? 1 : 0;
            }

            Assert.InRange((double)other / tries, 0.8, 0.95);
        }

        [Fact]
        public void Lenguaje_LeeOrientacionIdentidadYGenero_YFormaParejaDelGeneroPedido()
        {
            LifeSession session = AdultSession(Content(" }"), 3);
            Character player = session.World.Player;
            string gender = player.Sex == Sex.Female ? "femenino" : "masculino";

            Assert.True(Check(session, $"jugador.orientacion == 'heterosexual' && jugador.identidad == '{gender}' && jugador.genero == '{gender}'"));

            Run(session, $"jugador.formar_pareja('{gender}')");

            Assert.True(Check(session, $"jugador.pareja.genero == '{gender}' && jugador.pareja.orientacion == 'homosexual'"));
            Assert.True(Check(session, "jugador.sexo == jugador.pareja.sexo"));
        }

        [Fact]
        public void Lenguaje_FormarParejaConUnGeneroQueNoExiste_EsUnError()
        {
            LifeSession session = AdultSession(Content(" }"), 3);

            Assert.Throws<ScriptException>(() => Run(session, "jugador.formar_pareja('otro')"));
        }

        [Fact]
        public void HijoNoJugadoConParejaDelMismoSexo_NoTieneHijosBiologicos()
        {
            LifeSession session = AdultSession(Content(@", ""familia_no_jugada"": { ""pareja_anual"": 1, ""hijo_anual"": 1, ""separacion_anual"": 0 } }"), 3);
            Character child = FamilyFactory.CreateChild(session.World, session.Content, session.World.Player);
            child.Orientation = Orientation.Homosexual;

            session.Advance(25 * GameDate.WeeksPerYear);

            Character partner = session.World.GetCharacter(child.PartnerId!.Value);
            Assert.Equal(child.Sex, partner.Sex);
            Assert.Empty(session.World.ChildrenOf(child.Id));
        }

        [Fact]
        public void Diversidad_ConUnaClaveQueNoExiste_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + @", ""diversidad"": { ""orientaciones"": { ""otra"": 1 } } }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Message.Contains("'otra' no existe"));
        }
    }
}
