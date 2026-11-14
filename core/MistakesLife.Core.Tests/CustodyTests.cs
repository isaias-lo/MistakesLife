using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Courts;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Los hijos después de una separación (paquete de Chile): con quién viven (el
    /// cuidado personal), la pensión de alimentos que fija el juzgado (que se paga o
    /// se recibe cada mes y se reajusta con los precios) y el Registro Nacional de
    /// Deudores de Pensiones de Alimentos si se deja de pagar.
    /// </summary>
    public class CustodyTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(World world, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static Value Eval(World world, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, Content, world.Player, null, readOnly: true));

        private static double Number(World world, string expression) => Eval(world, expression).AsNumber(expression);

        private static void Apply(World world, string eventId, string? optionKey = null)
        {
            EventDefinition ev = Content.GetEvent(eventId);
            var scope = new EventScope(world, Content, world.Player, null, readOnly: false);
            var effects = optionKey == null ? ev.CompiledEffects : ev.Options.Single(o => o.TextKey == optionKey).CompiledEffects;
            foreach (Effect effect in effects)
            {
                effect.Apply(scope);
            }
        }

        private static bool OptionOpen(World world, string eventId, string optionKey)
        {
            EventOptionDefinition option = Content.GetEvent(eventId).Options.Single(o => o.TextKey == optionKey);
            return option.CompiledConditions == null || option.CompiledConditions.IsTrue(new EventScope(world, Content, world.Player, null, readOnly: true));
        }

        /// <summary>Una vida en Chile de 30 años, con dos hijos chicos, recién separada de su pareja.</summary>
        private static LifeSession SeparatedWithTwoChildren(out Character coParent)
        {
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 30 * GameDate.WeeksPerYear);
            Run(world, "jugador.formar_pareja()");
            coParent = world.GetCharacter(world.Player.PartnerId!.Value);
            Run(world, "jugador.tener_hijo()");
            Run(world, "jugador.tener_hijo()");
            Run(world, "jugador.terminar_pareja()");
            return session;
        }

        [Fact]
        public void AlSepararse_LosHijosQuedanPorAcordar_YLaOtraParteEsSuOtroPapa()
        {
            World world = SeparatedWithTwoChildren(out Character coParent).World;

            Assert.Equal(2, Number(world, "jugador.hijos_por_acordar"));
            Assert.Equal(2, Number(world, "jugador.hijos_en_casa"));
            Assert.True(Eval(world, "jugador.coprogenitor != nada").AsBool("prueba"));
            Assert.True(Content.GetEvent("evento.chile.cuidado_hijos").CompiledConditions!.IsTrue(new EventScope(world, Content, world.Player, null, readOnly: true)));
            Assert.Equal(coParent.Id, CustodyRules.CoParent(world, world.Player)!.Id);
        }

        [Fact]
        public void QuedarseConLosHijos_SiguenEnSuCasa_YQuedanASuCargo()
        {
            LifeSession session = SeparatedWithTwoChildren(out _);
            World world = session.World;

            int before = new LifePresenter(session).Snapshot().Home.Members.Count;
            Apply(world, "evento.chile.cuidado_hijos", "opcion.chile.cuidado_hijos.conmigo");

            Assert.Equal(0, Number(world, "jugador.hijos_por_acordar"));
            Assert.Equal(2, Number(world, "jugador.hijos_en_casa"));
            Assert.Equal(2, Number(world, "jugador.hijos_a_cargo"));
            Assert.Equal(0, Number(world, "jugador.hijos_con_otro"));
            Assert.Equal(before, new LifePresenter(session).Snapshot().Home.Members.Count);
        }

        [Fact]
        public void HijosConElOtro_NoVivenEnSuCasa_NiComenDeSuCompra()
        {
            LifeSession session = SeparatedWithTwoChildren(out _);
            World world = session.World;
            decimal before = GroceryRules.MonthlyShare(world, Content, world.Player, "compra.basica");
            int members = new LifePresenter(session).Snapshot().Home.Members.Count;

            Apply(world, "evento.chile.cuidado_hijos", "opcion.chile.cuidado_hijos.con_otro");

            Assert.Equal(0, Number(world, "jugador.hijos_en_casa"));
            Assert.Equal(2, Number(world, "jugador.hijos_con_otro"));
            Assert.Equal(members - 2, new LifePresenter(session).Snapshot().Home.Members.Count);
            Assert.True(GroceryRules.MonthlyShare(world, Content, world.Player, "compra.basica") < before);
        }

        [Fact]
        public void CuidadoCompartido_VivenConLosDos()
        {
            World world = SeparatedWithTwoChildren(out _).World;

            Run(world, "jugador.cuidado_compartido()");

            Assert.Equal(2, Number(world, "jugador.hijos_en_casa"));
            Assert.Equal(0, Number(world, "jugador.hijos_con_otro"));
            Assert.Equal(0, Number(world, "jugador.hijos_a_cargo"));
            Assert.Equal(0, Number(world, "jugador.hijos_por_acordar"));
        }

        [Fact]
        public void SiMuereElPapaConQuienViven_VuelvenAVivirConElJugador()
        {
            World world = SeparatedWithTwoChildren(out Character coParent).World;
            Run(world, "jugador.hijos_con_el_otro()");

            coParent.Die(world.Date);

            Assert.Equal(2, Number(world, "jugador.hijos_en_casa"));
            Assert.Equal(0, Number(world, "jugador.hijos_con_otro"));
            Assert.Equal(0, Number(world, "jugador.hijos_a_cargo"));
        }

        [Fact]
        public void JuicioDeCuidado_ElJuezMiraLaCasaLaSaludYLaConstancia()
        {
            World world = SeparatedWithTwoChildren(out _).World;
            Run(world, "abrir_causa('causa.chile.cuidado_personal', jugador.coprogenitor)");
            Run(world, "causa('causa.chile.cuidado_personal').ir_a('audiencia_juicio')");
            Run(world, "jugador.desmarcar('vive_independiente')");

            Assert.False(OptionOpen(world, "evento.chile.cuidado_sentencia", "opcion.chile.cuidado_sentencia.conmigo"));
            Assert.True(OptionOpen(world, "evento.chile.cuidado_sentencia", "opcion.chile.cuidado_sentencia.con_otro"));

            Apply(world, "evento.chile.cuidado_sentencia", "opcion.chile.cuidado_sentencia.con_otro");

            Assert.Equal("con_otro", Eval(world, "causa('causa.chile.cuidado_personal').resultado").AsText("resultado"));
            Assert.Equal(2, Number(world, "jugador.hijos_con_otro"));
        }

        [Fact]
        public void PensionFijada_SePagaCadaMes_YSeReajustaConLosPrecios()
        {
            World world = SeparatedWithTwoChildren(out _).World;
            Run(world, "jugador.hijos_con_el_otro()");
            Run(world, "abrir_causa('causa.chile.alimentos_demandado', jugador.coprogenitor)");
            Run(world, "causa('causa.chile.alimentos_demandado').ir_a('audiencia_juicio')");
            Apply(world, "evento.chile.demandado_sentencia", "opcion.chile.demandado_sentencia.aceptar");

            // Dos hijos sin sueldo: el mínimo legal, 30 % de un ingreso mínimo por cada uno.
            double minimum = Number(world, "precio(sueldo_minimo()) * 0.6");
            double fixedAmount = Number(world, "causa('causa.chile.alimentos_demandado').monto");
            Assert.True(fixedAmount >= minimum - 1, $"pensión {fixedAmount}, mínimo {minimum}");

            new WeekCycle().Register(new EconomySystem(Content)).Register(new ExpenseSystem(Content)).AdvanceWeeks(world, 2 * GameDate.WeeksPerYear);

            PayslipLine line = Assert.Single(world.Player.LastExpenses, l => l.Id == "gasto.chile.pension_alimentos");
            double today = Number(world, "causa('causa.chile.alimentos_demandado').monto");
            Assert.True(today > fixedAmount, "la pensión se reajusta con los precios");
            Assert.Equal((decimal)today, line.Amount);
        }

        [Fact]
        public void PensionQueSeRecibe_EntraCadaMes_YSeVeEnTuDinero()
        {
            LifeSession session = SeparatedWithTwoChildren(out _);
            World world = session.World;
            Run(world, "jugador.quedarse_con_hijos()");
            Run(world, "abrir_causa('causa.chile.alimentos', jugador.coprogenitor)");
            Run(world, "causa('causa.chile.alimentos').monto = 150000");
            Run(world, "causa('causa.chile.alimentos').cerrar('pension_fijada')");
            WeekCycle cycle = new WeekCycle().Register(new ExpenseSystem(Content));
            while (!world.Date.IsFirstWeekOfMonth)
            {
                cycle.AdvanceWeek(world);
            }

            world.Player.Money = 1000000;
            cycle.AdvanceWeek(world);

            decimal paid = world.Player.LastExpenses.Where(l => l.Amount > 0).Sum(l => l.Amount);
            Assert.Equal(1000000 - paid + 150000, world.Player.Money);
            Assert.Equal(-150000m, Assert.Single(world.Player.LastExpenses, l => l.Id == "gasto.chile.pension_alimentos_recibida").Amount);
            Assert.Equal((double)paid, Number(world, "jugador.gastos"));
            Assert.Contains(new LifePresenter(session).Money().Lines, l => l.Id == "gasto.chile.pension_alimentos_recibida" && l.IsIncome && l.AmountLabel.StartsWith("+"));
        }

        [Fact]
        public void DejarDePagar_LlevaALaLiquidacion_AlRegistroDeDeudores_YALaRetencionDeFondos()
        {
            World world = SeparatedWithTwoChildren(out _).World;
            Run(world, "jugador.hijos_con_el_otro()");
            Run(world, "abrir_causa('causa.chile.alimentos_demandado', jugador.coprogenitor)");
            Run(world, "causa('causa.chile.alimentos_demandado').monto = 200000");
            Run(world, "causa('causa.chile.alimentos_demandado').cerrar('pension_fijada')");

            Apply(world, "evento.chile.pension_cuesta", "opcion.chile.pension_cuesta.dejar_de_pagar");
            Assert.True(world.Player.HasFlag("no_paga_pension"));
            new WeekCycle().Register(new CourtSystem(Content)).AdvanceWeeks(world, 17);
            Assert.True(Eval(world, "causa_en('causa.chile.cumplimiento_alimentos', 'liquidacion')").AsBool("prueba"));

            world.Player.Money = 0;
            Assert.False(OptionOpen(world, "evento.chile.liquidacion_alimentos", "opcion.chile.liquidacion_alimentos.pagar"));
            Apply(world, "evento.chile.liquidacion_alimentos", "opcion.chile.liquidacion_alimentos.no_pagar");
            Assert.True(world.Player.HasFlag("deudor_alimentos"));
            Assert.Equal(600000, Number(world, "causa('causa.chile.cumplimiento_alimentos').monto"));

            // El banco retiene lo que hay en la cuenta, hasta lo que se debe.
            world.Player.Money = 250000;
            Apply(world, "evento.chile.retencion_fondos_alimentos");
            Assert.Equal(0, world.Player.Money);
            Assert.Equal(350000, Number(world, "causa('causa.chile.cumplimiento_alimentos').monto"));

            // Vuelve a pagar la pensión de cada mes, y la próxima retención paga lo que queda.
            Apply(world, "evento.chile.retomar_pension", "opcion.chile.retomar_pension.pagar");
            Assert.False(world.Player.HasFlag("no_paga_pension"));
            world.Player.Money = 500000;
            Apply(world, "evento.chile.retencion_fondos_alimentos");
            Assert.Equal(150000, world.Player.Money);
            Assert.True(world.Player.HasFlag("deudor_alimentos"));
            Assert.True(Content.GetEvent("evento.chile.sale_registro_deudores").CompiledConditions!.IsTrue(new EventScope(world, Content, world.Player, null, readOnly: true)));
            Apply(world, "evento.chile.sale_registro_deudores");

            Assert.False(world.Player.HasFlag("deudor_alimentos"));
            Assert.False(world.Player.HasFlag("no_paga_pension"));
            Assert.Equal("pagada", Eval(world, "causa('causa.chile.cumplimiento_alimentos').resultado").AsText("resultado"));
        }

        [Fact]
        public void Guardado_ConservaConQuienVivenLosHijos()
        {
            LifeSession session = SeparatedWithTwoChildren(out _);
            Run(session.World, "jugador.hijos_con_el_otro()");

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());

            Assert.Equal(2, Number(loaded.World, "jugador.hijos_con_otro"));
            Assert.Equal(session.SaveToText(), loaded.SaveToText());
        }

        [Fact]
        public void GuardadoVersion14_ElMontoDeLaCausaPasaADineroDelInicio()
        {
            LifeSession session = SessionInChile();
            Run(session.World, "abrir_causa('causa.chile.cobro_deuda')");
            JObject save = JObject.Parse(session.SaveToText());
            save["version"] = 14;
            save["world"]!["courtCases"]![0]!["amount"] = 300000;
            foreach (JObject economy in save["world"]!["economies"]!.OfType<JObject>())
            {
                economy["priceIndex"] = 2.0;
            }

            LifeSession loaded = LifeSession.Load(Content, save.ToString());

            Assert.Equal(150000m, Assert.Single(loaded.World.CourtCases).Amount);
            Assert.Equal(300000, Number(loaded.World, "causa('causa.chile.cobro_deuda').monto"));
        }

        private static LifeSession SessionInChile()
        {
            for (ulong seed = 1; seed < 100; seed++)
            {
                var session = new LifeSession(Content, seed);
                if (session.World.Player.Residence!.CountryId == "pais.chile")
                {
                    return session;
                }
            }

            throw new Xunit.Sdk.XunitException("Ninguna vida nació en Chile.");
        }
    }
}
