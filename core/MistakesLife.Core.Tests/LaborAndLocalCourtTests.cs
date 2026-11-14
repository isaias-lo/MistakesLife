using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Juzgados, parte 3 (paquete de Chile): el finiquito y el Juzgado del Trabajo
    /// después de un despido (con la antigüedad del trabajo), la posesión efectiva de
    /// una herencia y las multas del Juzgado de Policía Local.
    /// </summary>
    public class LaborAndLocalCourtTests
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

        /// <summary>Una vida en Chile de 30 años que lleva tres años con contrato en el mismo trabajo.</summary>
        private static LifeSession WorkerWithThreeYears()
        {
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 27 * GameDate.WeeksPerYear);
            Run(world, "jugador.marcar('trabajando')");
            Run(world, "jugador.trabajar(800000)");
            new WeekCycle().Register(new EconomySystem(Content)).Register(new PayrollSystem(Content)).AdvanceWeeks(world, 3 * GameDate.WeeksPerYear);
            return session;
        }

        [Fact]
        public void Antiguedad_SumaUnMesPorCadaMesTrabajado_YQuedaAlDejarElTrabajo()
        {
            World world = WorkerWithThreeYears().World;

            double months = Number(world, "jugador.meses_en_trabajo");
            Assert.InRange(months, 35, 37);
            double salary = Number(world, "jugador.sueldo");

            Run(world, "jugador.dejar_trabajo()");

            Assert.Equal(0, Number(world, "jugador.meses_en_trabajo"));
            Assert.Equal(months, Number(world, "jugador.meses_ultimo_trabajo"));
            Assert.Equal(salary, Number(world, "jugador.ultimo_sueldo"));
            Assert.Equal("dependiente", Eval(world, "jugador.tipo_ultimo_trabajo").AsText("tipo"));
        }

        [Fact]
        public void DespidoPorNecesidadesDeLaEmpresa_ElFiniquitoPagaUnSueldoPorAnoMasElAviso()
        {
            World world = WorkerWithThreeYears().World;
            Apply(world, "evento.perder_trabajo", "opcion.perder_trabajo.buscar");
            Assert.True(world.Player.HasFlag("despedido_necesidades"));
            double salary = Number(world, "jugador.ultimo_sueldo");
            double years = Number(world, "jugador.meses_ultimo_trabajo") / 12;
            decimal before = world.Player.Money;

            Apply(world, "evento.chile.finiquito", "opcion.chile.finiquito.firmar");

            Assert.Equal(salary * years + salary, (double)(world.Player.Money - before), 0);
            Assert.False(world.Player.HasFlag("despedido_necesidades"));
        }

        [Fact]
        public void SinContrato_NoHayFiniquito()
        {
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 30 * GameDate.WeeksPerYear);
            Run(world, "jugador.trabajar(500000, 'honorarios')");
            Apply(world, "evento.perder_trabajo", "opcion.perder_trabajo.buscar");

            Assert.False(OptionOpen(world, "evento.chile.finiquito", "opcion.chile.finiquito.firmar"));
            Assert.True(OptionOpen(world, "evento.chile.finiquito", "opcion.chile.finiquito.sin_contrato"));
        }

        [Fact]
        public void DespidoDisciplinario_DemandaConRecargo_YLaSentenciaPaga()
        {
            World world = WorkerWithThreeYears().World;
            Run(world, "jugador.marcar('despedido_disciplinario')");
            Run(world, "jugador.dejar_trabajo()");
            double salary = Number(world, "jugador.ultimo_sueldo");
            double years = Number(world, "jugador.meses_ultimo_trabajo") / 12;

            Apply(world, "evento.chile.despido_disciplinario", "opcion.chile.despido_disciplinario.demandar");
            double claim = Number(world, "causa('causa.chile.despido').monto");
            Assert.Equal(salary * years * 1.8 + salary, claim, 0);

            Run(world, "causa('causa.chile.despido').ir_a('audiencia_juicio')");
            bool wins = world.Player.Personality.Conscientiousness >= 35;
            decimal before = world.Player.Money;
            Apply(world, "evento.chile.despido_sentencia", wins ? "opcion.chile.despido_sentencia.acogida" : "opcion.chile.despido_sentencia.rechazada");

            Assert.Equal(wins ? claim : 0, (double)(world.Player.Money - before), 0);
            Assert.False(Eval(world, "causa('causa.chile.despido').abierta").AsBool("abierta"));
        }

        [Fact]
        public void HerenciaEnChile_QuedaPendienteHastaLaPosesionEfectiva()
        {
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 40 * GameDate.WeeksPerYear);
            Character mother = world.GetCharacter(world.Player.MotherId!.Value);
            Character? father = world.Player.FatherId.HasValue ? world.GetCharacter(world.Player.FatherId.Value) : null;
            father?.Die(world.Date);
            mother.Money = 5000000;
            mother.Die(world.Date);
            decimal before = world.Player.Money;

            InheritanceSettlement.Settle(world, Content, mother);

            Assert.Equal(before, world.Player.Money);
            double pending = Number(world, "jugador.herencia_pendiente");
            Assert.True(pending > 0);

            Apply(world, "evento.chile.posesion_efectiva", "opcion.chile.posesion_efectiva.tramitar");
            new WeekCycle().Register(new Courts.CourtSystem(Content)).AdvanceWeeks(world, 13);
            Assert.True(Eval(world, "causa_en('causa.chile.posesion_efectiva', 'inscripcion')").AsBool("lista"));
            decimal beforeReceiving = world.Player.Money;
            Apply(world, "evento.chile.posesion_efectiva_lista");

            Assert.Equal(0, Number(world, "jugador.herencia_pendiente"));
            Assert.True(world.Player.Money > beforeReceiving);
            Assert.True(world.Player.HasFlag("heredo_algo"));
        }

        [Fact]
        public void PoliciaLocal_LaPrimeraVezConDescargosAbsuelve_YSinIrQuedaLaMultaImpaga()
        {
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            Run(world, "jugador.marcar('vive_independiente')");

            Apply(world, "evento.chile.fiesta_ruidos", "opcion.chile.fiesta_ruidos.seguir");
            Run(world, "causa('causa.chile.policia_local').ir_a('audiencia')");
            Assert.True(OptionOpen(world, "evento.chile.policia_local_audiencia", "opcion.chile.policia_local.descargos_primera"));
            Apply(world, "evento.chile.policia_local_audiencia", "opcion.chile.policia_local.descargos_primera");
            Assert.Equal("absuelto", Eval(world, "causa('causa.chile.policia_local').resultado").AsText("resultado"));

            Apply(world, "evento.chile.tomar_en_la_calle", "opcion.chile.tomar_en_la_calle.plaza");
            Run(world, "causa('causa.chile.policia_local').ir_a('audiencia')");
            Assert.False(OptionOpen(world, "evento.chile.policia_local_audiencia", "opcion.chile.policia_local.descargos_primera"));
            Apply(world, "evento.chile.policia_local_audiencia", "opcion.chile.policia_local.no_ir");
            Assert.True(world.Player.HasFlag("multa_impaga"));
        }

        [Fact]
        public void Guardado_ConservaLaAntiguedadElUltimoTrabajoYLaHerencia()
        {
            LifeSession session = WorkerWithThreeYears();
            World world = session.World;
            Run(world, "jugador.trabajar(900000)");
            world.Player.PendingInheritance = 1234;

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());

            Assert.Equal(world.Player.Job!.MonthsInJob, loaded.World.Player.Job!.MonthsInJob);
            Assert.Equal(1234m, loaded.World.Player.PendingInheritance);
            Assert.Equal(session.SaveToText(), loaded.SaveToText());
        }

        [Fact]
        public void GuardadoVersion15_LaAntiguedadPartePorLaDelCargo()
        {
            LifeSession session = WorkerWithThreeYears();
            JObject save = JObject.Parse(session.SaveToText());
            save["version"] = 15;
            JObject player = save["world"]!["characters"]!.OfType<JObject>().Single(c => c.Value<int>("id") == session.World.PlayerId);
            JObject job = (JObject)player["job"]!;
            job.Remove("monthsInJob");
            job["monthsInPosition"] = 7;
            player.Remove("lastJob");
            player.Remove("pendingInheritance");

            LifeSession loaded = LifeSession.Load(Content, save.ToString());

            Assert.Equal(7, loaded.World.Player.Job!.MonthsInJob);
            Assert.Null(loaded.World.Player.LastJob);
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
