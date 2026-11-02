using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class InheritanceTests
    {
        /// <summary>
        /// País de prueba sin inflación: un crédito que pasa a la herencia y otro que
        /// se extingue al morir; la pareja recibe el doble que cada hijo.
        /// </summary>
        private static ContentDatabase Content(bool limitedToEstate = true)
        {
            string economy = @", ""economia"": {
              ""inflacion"": { ""regimen_inicial"": ""economia.inflacion.cero"",
                ""regimenes"": [ { ""id"": ""economia.inflacion.cero"", ""anual_min"": 0, ""anual_max"": 0 } ] },
              ""deudas"": { ""tipos"": [
                { ""id"": ""deuda.prueba.consumo"", ""tasa_anual"": 0, ""cuotas"": 10 },
                { ""id"": ""deuda.prueba.estudios"", ""tasa_anual"": 0, ""cuotas"": 10, ""se_extingue_al_morir"": true }
              ] },
              ""herencia"": { ""beneficio_inventario"": " + (limitedToEstate ? "true" : "false") + @", ""porciones_pareja"": 2 }
            } }";
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}') + economy);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        /// <summary>El jugador muere y se reparte su herencia.</summary>
        private static void Die(World world, ContentDatabase content)
        {
            world.Player.Die(world.Date);
            InheritanceSettlement.Settle(world, content, world.Player);
        }

        private static (World World, Character Partner, Character[] Children) Family(ContentDatabase content)
        {
            World world = new LifeSession(content, 1).World;
            Run(world, content, "jugador.formar_pareja()");
            Run(world, content, "jugador.tener_hijo()");
            Run(world, content, "jugador.tener_hijo()");
            Character partner = world.GetCharacter(world.Player.PartnerId!.Value);
            return (world, partner, world.ChildrenOf(world.PlayerId).ToArray());
        }

        [Fact]
        public void Bienes_SeRepartenEntreHijosYPareja_ConElDobleParaLaPareja()
        {
            ContentDatabase content = Content();
            var (world, partner, children) = Family(content);
            world.Player.Money = 400000;

            Die(world, content);

            Assert.Equal(200000m, partner.Money);
            Assert.All(children, c => Assert.Equal(100000m, c.Money));
            Assert.Equal(0m, world.Player.Money);
            Assert.Contains(world.Journal, e => e.CharacterId == children[0].Id && e.TextKey == InheritanceSettlement.ReceivedJournal);
        }

        [Fact]
        public void SinHijosNiPareja_HeredanLosPadres()
        {
            ContentDatabase content = Content();
            World world = new LifeSession(content, 1).World;
            Character mother = world.GetCharacter(world.Player.MotherId!.Value);
            Character father = world.GetCharacter(world.Player.FatherId!.Value);
            decimal before = mother.Money + father.Money;
            world.Player.Money = 300000;

            Die(world, content);

            Assert.Equal(before + 300000m, mother.Money + father.Money);
        }

        [Fact]
        public void FondoDePension_EsParteDeLaHerencia()
        {
            ContentDatabase content = Content();
            var (world, partner, children) = Family(content);
            world.Player.PensionFund = 400000;

            Die(world, content);

            Assert.Equal(200000m, partner.Money);
            Assert.Equal(0m, world.Player.PensionFund);
        }

        [Fact]
        public void DeudasMayoresQueBienes_ConBeneficioDeInventario_NadieHeredaNiPaga()
        {
            ContentDatabase content = Content();
            var (world, partner, children) = Family(content);
            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 500000)");
            world.Player.Money = 100000;

            Die(world, content);

            Assert.Equal(0m, partner.Money);
            Assert.All(children, c => Assert.Equal(0m, c.Money));
            Assert.Empty(world.Player.Debts);
        }

        [Fact]
        public void DeudasMayoresQueBienes_SinBeneficioDeInventario_LosHerederosPaganSuParte()
        {
            ContentDatabase content = Content(limitedToEstate: false);
            var (world, partner, children) = Family(content);
            Run(world, content, "jugador.endeudarse('deuda.prueba.consumo', 500000)");
            world.Player.Money = 100000;

            Die(world, content);

            // Faltan 400.000: la pareja paga 200.000 y cada hijo 100.000.
            Assert.Equal(-200000m, partner.Money);
            Assert.All(children, c => Assert.Equal(-100000m, c.Money));
            Assert.Contains(world.Journal, e => e.CharacterId == partner.Id && e.TextKey == InheritanceSettlement.DebtsJournal);
        }

        [Fact]
        public void DeudaQueSeExtingueAlMorir_NoPasaALaHerencia()
        {
            ContentDatabase content = Content();
            var (world, partner, children) = Family(content);
            world.Player.Money = 400000;
            Run(world, content, "jugador.endeudarse('deuda.prueba.estudios', 1000000)");
            world.Player.Money = 400000;

            Die(world, content);

            Assert.Equal(200000m, partner.Money);
        }

        [Fact]
        public void Chile_ElCaeSeExtingueYLaParejaRecibeElDoble()
        {
            EconomyDefinition chile = RepoPacks.Content.GetCountry("pais.chile").Economy!;

            Assert.True(chile.Debts!.Type("deuda.chile.cae")!.EndsAtDeath);
            Assert.True(chile.Inheritance!.LimitedToEstate);
            Assert.Equal(2, chile.Inheritance.PartnerShares);
        }
    }
}
