using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class RelationshipTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static World NewWorld(ulong seed = 1) => new LifeSession(Content, seed).World;

        private static void Run(World world, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static Value Eval(World world, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, Content, world.Player, null, readOnly: true));

        private static double Number(World world, string expression) => Eval(world, expression).AsNumber("prueba");

        private static Character OnlyWithBond(World world, RelationshipBond bond) =>
            Assert.Single(RelationshipRules.WithBond(world, world.Player, bond));

        private static void AdvanceRelationships(World world, int weeks) =>
            new WeekCycle().Register(new RelationshipSystem()).AdvanceWeeks(world, weeks);

        [Fact]
        public void Familia_SinNadaGuardado_EmpiezaConCarinoDeFamilia()
        {
            World world = NewWorld();

            Assert.Equal(RelationshipRules.ParentChild.Affection, Number(world, "jugador.madre.carino"));
            Assert.Equal(RelationshipRules.ParentChild.Trust, Number(world, "jugador.padre.confianza"));
            Assert.Equal(0, Number(world, "jugador.madre.rencor"));
            Assert.Empty(world.Player.Relationships);
        }

        [Fact]
        public void Sentimientos_SeCambianConEfectosYQuedanEntre0y100()
        {
            World world = NewWorld();

            Run(world, "jugador.madre.rencor += 150");
            Run(world, "jugador.madre.carino -= 200");
            Run(world, "jugador.madre.respeto += 10");

            Assert.Equal(100, Number(world, "jugador.madre.rencor"));
            Assert.Equal(0, Number(world, "jugador.madre.carino"));
            Assert.Equal(RelationshipRules.ParentChild.Respect + 10, Number(world, "jugador.madre.respeto"));
        }

        [Fact]
        public void Sentimientos_NoSonSimetricos()
        {
            World world = NewWorld();
            Character mother = world.GetCharacter(world.Player.MotherId!.Value);

            Run(world, "jugador.madre.rencor += 40");

            // La madre guarda rencor; el hijo no siente rencor por ella.
            Assert.Equal(40, RelationshipRules.Feelings(world, mother, world.Player).Resentment);
            Assert.Equal(0, RelationshipRules.Feelings(world, world.Player, mother).Resentment);
        }

        [Fact]
        public void Carino_LeidoEnElMismoJugador_EsUnErrorClaro()
        {
            World world = NewWorld();

            var ex = Assert.Throws<ScriptException>(() => Eval(world, "jugador.carino"));

            Assert.Contains("jugador.madre.carino", ex.Message);
        }

        [Fact]
        public void Plantar_ConTestigo_DiceQuienLoVaANotarSinAplicarNada()
        {
            World world = NewWorld();
            new WeekCycle().AdvanceWeeks(world, 20 * GameDate.WeeksPerYear);
            var scope = new EventScope(world, Content, world.Player, null, readOnly: true);
            Effect plant = Effect.Parse("plantar('semilla.presto_plata_amigo', jugador.amigo)");

            // Sin amigo todavía: el testigo es "nada".
            Assert.True(plant.PlantsSeed);
            Assert.Equal(ValueKind.Null, Assert.Single(plant.Witnesses(scope)).Kind);

            Run(world, "jugador.conocer('amistad')");
            Assert.Equal(ValueKind.Object, Assert.Single(plant.Witnesses(scope)).Kind);
            Assert.DoesNotContain(world.Seeds.All, seed => seed.TypeId == "semilla.presto_plata_amigo");
        }

        [Fact]
        public void Efecto_SinPlantar_NoSeRecuerdaNiTieneTestigos()
        {
            World world = NewWorld();
            var scope = new EventScope(world, Content, world.Player, null, readOnly: true);

            Effect effect = Effect.Parse("jugador.felicidad -= 5");
            Assert.False(effect.PlantsSeed);
            Assert.Empty(effect.Witnesses(scope));
            Assert.True(Effect.Parse("plantar('semilla.presto_plata_amigo')").PlantsSeed);
            Assert.Empty(Effect.Parse("plantar('semilla.presto_plata_amigo')").Witnesses(scope));
        }

        [Fact]
        public void Conocer_Amistad_CreaUnAmigoDelMismoLugarYEdadParecida()
        {
            World world = NewWorld();
            new WeekCycle().AdvanceWeeks(world, 10 * GameDate.WeeksPerYear);

            Run(world, "jugador.conocer('amistad')");

            Character friend = OnlyWithBond(world, RelationshipBond.Friend);
            Assert.Equal(1, Number(world, "jugador.amigos"));
            Assert.InRange(friend.AgeInYears(world.Date), 9, 11);
            Assert.Equal(world.Player.Residence, friend.Residence);
            Assert.Equal(RelationshipBond.Friend, friend.RelationshipWith(world.Player.Id)!.Bond);
            Assert.Equal(RelationshipRules.Initial(RelationshipBond.Friend).Affection, Number(world, "jugador.amigo.carino"));
            Assert.Equal("amistad", Eval(world, "jugador.amigo.vinculo").AsText("prueba"));
        }

        [Fact]
        public void Conocer_ConUnLazoQueNoExiste_EsUnError()
        {
            World world = NewWorld();

            Assert.Throws<ScriptException>(() => Run(world, "jugador.conocer('primo')"));
        }

        [Fact]
        public void Evento_ConUnLazoQueNoExiste_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "events.json", @"[{ ""id"": ""evento.a"", ""efectos"": [""jugador.conocer('primo')""] }]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("primo"));
        }

        [Fact]
        public void Amigo_EsElQueMasCarinoLeTiene()
        {
            World world = NewWorld();
            Run(world, "jugador.conocer('amistad')");
            Run(world, "jugador.conocer('amistad')");
            Character second = RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend).Last();

            RelationshipRules.Edit(world, second, world.Player).Affection += 20;

            Assert.Equal(2, Number(world, "jugador.amigos"));
            Assert.Equal(RelationshipRules.Initial(RelationshipBond.Friend).Affection + 20, Number(world, "jugador.amigo.carino"));
        }

        [Fact]
        public void Alejarse_QuitaElLazoPeroNoLoQueSiente()
        {
            World world = NewWorld();
            Run(world, "jugador.conocer('amistad')");
            Character friend = OnlyWithBond(world, RelationshipBond.Friend);
            Run(world, "jugador.amigo.rencor += 30");

            Run(world, "jugador.amigo.alejarse()");

            Assert.Equal(0, Number(world, "jugador.amigos"));
            Assert.Equal(Value.Null, Eval(world, "jugador.amigo"));
            Assert.Equal(RelationshipBond.None, friend.RelationshipWith(world.Player.Id)!.Bond);
            Assert.Equal(30, friend.RelationshipWith(world.Player.Id)!.Resentment);
        }

        [Fact]
        public void Vincular_UnColegaComoAmigo_ConservaLoQueSentia()
        {
            World world = NewWorld();
            Run(world, "jugador.conocer('colega')");
            Run(world, "jugador.colega.carino += 10");

            Run(world, "jugador.colega.vincular('amistad')");

            Assert.Equal(Value.Null, Eval(world, "jugador.colega"));
            Assert.Equal(RelationshipRules.Initial(RelationshipBond.Colleague).Affection + 10, Number(world, "jugador.amigo.carino"));
        }

        [Fact]
        public void Rival_EmpiezaConRencor()
        {
            World world = NewWorld();

            Run(world, "jugador.conocer('rival')");

            Assert.Equal(RelationshipRules.Initial(RelationshipBond.Rival).Resentment, Number(world, "jugador.rival.rencor"));
        }

        [Fact]
        public void Rencor_SeOlvidaConLosMeses_MasRapidoSiEsAmable()
        {
            World world = NewWorld();
            Run(world, "jugador.conocer('amistad')");
            Character friend = OnlyWithBond(world, RelationshipBond.Friend);
            RelationshipRules.Edit(world, friend, world.Player).Resentment = 50;

            AdvanceRelationships(world, GameDate.WeeksPerYear);

            int perMonth = friend.Personality.Agreeableness >= RelationshipSystem.ForgivingAgreeableness ? 2 : 1;
            Assert.Equal(50 - (12 * perMonth), friend.RelationshipWith(world.Player.Id)!.Resentment);
        }

        [Fact]
        public void Amistad_SinNadaQueLaAlimente_SeEnfriaDeAPoco()
        {
            World world = NewWorld();
            Run(world, "jugador.conocer('amistad')");
            Character friend = OnlyWithBond(world, RelationshipBond.Friend);
            int start = friend.RelationshipWith(world.Player.Id)!.Affection;

            AdvanceRelationships(world, GameDate.WeeksPerYear);

            // Un punto cada 4 meses: 3 en un año.
            Assert.Equal(start - (12 / RelationshipSystem.FriendshipFadeMonths), friend.RelationshipWith(world.Player.Id)!.Affection);
        }

        [Fact]
        public void TerminarPareja_QuedanComoExConLoQueSentian()
        {
            World world = NewWorld();
            new WeekCycle().AdvanceWeeks(world, 20 * GameDate.WeeksPerYear);
            Run(world, "jugador.formar_pareja()");
            Run(world, "jugador.pareja.rencor += 30");

            Run(world, "jugador.terminar_pareja()");

            Assert.Equal(Value.Null, Eval(world, "jugador.pareja"));
            Assert.Equal(30, Number(world, "jugador.ex.rencor"));
            Assert.Equal(RelationshipRules.Partners.Affection, Number(world, "jugador.ex.carino"));
            Assert.Equal("ex", Eval(world, "jugador.ex.vinculo").AsText("prueba"));
        }

        [Fact]
        public void PerderLaParejaPorMuerte_NoLaDejaComoEx()
        {
            World world = NewWorld();
            new WeekCycle().AdvanceWeeks(world, 20 * GameDate.WeeksPerYear);
            Run(world, "jugador.formar_pareja()");
            Character partner = world.GetCharacter(world.Player.PartnerId!.Value);

            partner.Die(world.Date);
            FamilyFactory.EndPartnership(world, partner, breakUp: false);

            Assert.Null(world.Player.PartnerId);
            Assert.Empty(world.Player.Relationships);
        }

        [Fact]
        public void Plantar_ConUnTestigo_LaSemillaRecuerdaAEsaPersona()
        {
            World world = NewWorld();
            new WeekCycle().AdvanceWeeks(world, 20 * GameDate.WeeksPerYear);
            Run(world, "jugador.conocer('amistad')");
            Run(world, "jugador.amigo.carino += 7");

            Run(world, "plantar('semilla.presto_plata_amigo', jugador.amigo)");
            Run(world, "jugador.conocer('amistad')");

            Assert.Equal(1, Number(world, "semilla('semilla.presto_plata_amigo').testigos"));
            Assert.Equal(RelationshipRules.Initial(RelationshipBond.Friend).Affection + 7, Number(world, "semilla('semilla.presto_plata_amigo').testigo.carino"));
        }

        [Fact]
        public void Plantar_ConUnTestigoQueEsNada_EsUnError()
        {
            World world = NewWorld();

            Assert.Throws<ScriptException>(() => Run(world, "plantar('semilla.presto_plata_amigo', jugador.amigo)"));
        }

        [Fact]
        public void Partida_ConAmigosYRencores_SeRepiteExacta()
        {
            static string Summary(ulong seed)
            {
                var session = new LifeSession(Content, seed);
                var decider = new Decisions.AutoDecider(session.Content);
                while (session.World.Player.IsAlive && session.PlayerAgeInYears < 40)
                {
                    if (session.Pending != null)
                    {
                        var chooser = new Randomness.GameRandom(seed).Fork("prueba." + session.World.Date.TotalWeeks + "." + session.World.EventHistory.Count);
                        session.Choose(decider.Choose(session.World, session.Pending, chooser));
                        continue;
                    }

                    session.Advance(GameDate.WeeksPerYear);
                }

                return string.Join(";", session.World.Player.Relationships.Select(r => r.ToString() + r.Bond));
            }

            Assert.Equal(Summary(11), Summary(11));
            Assert.NotEmpty(Summary(11));
        }
    }
}
