using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Momentos frescos con amigos, pareja y papás (packs/base/events/momentos.json):
    /// cosas que pasan pocas veces en la vida y que, cuando dejan una semilla, la
    /// dejan con la persona real como testigo, para que años después vuelva ella.
    /// </summary>
    public class MomentsTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static readonly string[] NewEvents =
        {
            "evento.amigo_se_casa", "evento.ahijado", "evento.amigo_se_va_del_pais", "evento.amigo_vuelve_de_visita",
            "evento.amigo_hospital", "evento.reencuentro_curso", "evento.cumpleanos_sorpresa", "evento.amigo_pide_consejo",
            "evento.negocio_con_amigo", "evento.viaje_con_pareja", "evento.pareja_y_suegra", "evento.celos_de_amigo",
            "evento.pareja_confiesa_deuda", "evento.mama_cuenta_historia", "evento.mama_celular", "evento.papa_consejo",
        };

        private static void Apply(World world, EventOptionDefinition option)
        {
            var scope = new EventScope(world, Content, world.Player, null, readOnly: false);
            foreach (Effect effect in option.CompiledEffects)
            {
                effect.Apply(scope);
            }
        }

        private static EventOptionDefinition Option(string eventId, string optionKey) =>
            Content.GetEvent(eventId).Options.Single(o => o.TextKey == optionKey);

        [Fact]
        public void MomentosNuevos_EstanEnElPaqueteBase_YNoSeRepitenSinTope()
        {
            foreach (string id in NewEvents)
            {
                EventDefinition ev = Content.GetEvent(id);
                Assert.True(ev.SeedType != null || ev.MaxTimes != null || ev.Unique, id + " necesita unico, veces_maximas o venir de una semilla");
            }
        }

        [Fact]
        public void AmigoSeVaDelPais_DejaLaSemillaConElComoTestigo_YAlVolverSeRetomaLaAmistad()
        {
            World world = new LifeSession(Content, 3).World;
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            Effect.Parse("jugador.conocer('amistad')").Apply(new EventScope(world, Content, world.Player, null, readOnly: false));
            Character friend = Assert.Single(RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend));

            Apply(world, Option("evento.amigo_se_va_del_pais", "opcion.amigo_se_va.despedida"));

            Seed seed = Assert.Single(world.Seeds.OpenFor(world.PlayerId), s => s.TypeId == "semilla.amigo_lejos");
            Assert.True(seed.HasWitness(friend.Id));
            Assert.Empty(RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend));

            Apply(world, Option("evento.amigo_vuelve_de_visita", "opcion.amigo_vuelve.juntarse"));

            Assert.Equal(friend.Id, Assert.Single(RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend)).Id);
        }
    }
}
