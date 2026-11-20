using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// La vida de pareja (D4d2): la infidelidad siempre con alguien real (insinuada y solo entre
    /// adultos, como eligió Isaias), los celos que nacen de lo que haces y los viajes.
    /// </summary>
    public class CoupleLifeTests
    {
        private static void Run(LifeSession session, string effect, Character? person = null) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false, person));

        private static Value Eval(LifeSession session, string expression, Character? person = null) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true, person));

        /// <summary>Una vida adulta heterosexual que pololea con alguien real.</summary>
        private static (LifeSession Session, Character Partner) Couple(ContentDatabase content, int age = 30)
        {
            var session = new LifeSession(content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            Character player = session.World.Player;
            player.Orientation = Orientation.Heterosexual;
            player.GenderIdentity = player.Gender;
            Character partner = FamilyFactory.CreateStranger(session.World, content, player);
            partner.Orientation = Orientation.Heterosexual;
            CoupleRules.StartDating(session.World, player, partner);
            CoupleRules.SetStage(session.World, player, CoupleStage.Steady);
            return (session, partner);
        }

        /// <summary>Juega semana a semana, eligiendo la primera opción, hasta que aparece ese evento.</summary>
        private static PendingEvent? PlayUntil(LifeSession session, string eventId, int weeks)
        {
            for (int week = 0; week < weeks; week++)
            {
                while (session.Pending != null)
                {
                    if (session.Pending.EventId == eventId)
                    {
                        return session.Pending;
                    }

                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }

            return session.Pending?.EventId == eventId ? session.Pending : null;
        }

        [Fact]
        public void HayQuimica_EntreAdultosQueSePuedenGustar_AunqueTengasPareja()
        {
            (LifeSession session, Character partner) = Couple(RepoPacks.Content);
            Character other = FamilyFactory.CreateStranger(session.World, RepoPacks.Content, session.World.Player);
            other.Orientation = Orientation.Bisexual;

            Assert.True(Eval(session, "persona.hay_quimica", other).AsBool("quimica"));
            Assert.False(Eval(session, "persona.te_puede_gustar", other).AsBool("gustar"));
            Assert.False(Eval(session, "persona.hay_quimica", partner).AsBool("pareja"));
        }

        [Fact]
        public void SiCedes_LaSemillaQuedaConEsaPersona_YDespuesTuParejaSeEntera()
        {
            (LifeSession session, Character partner) = Couple(RepoPacks.Content);
            Character other = FamilyFactory.CreateStranger(session.World, RepoPacks.Content, session.World.Player);
            int rencor = RelationshipRules.Feelings(session.World, partner, session.World.Player).Resentment;

            Run(session, "plantar('semilla.infidelidad', persona)", other);
            PendingEvent? pending = PlayUntil(session, "evento.pareja.infidelidad_se_sabe", 6 * GameDate.WeeksPerYear);

            Assert.NotNull(pending);
            Assert.Equal(other.Id, pending!.PersonId);
            session.Choose(pending.AvailableOptions[0]);
            if (session.World.Player.PartnerId == partner.Id)
            {
                Assert.True(RelationshipRules.Feelings(session.World, partner, session.World.Player).Resentment >= rencor + 20);
            }
        }

        [Fact]
        public void TuParejaTeEngana_ConAlguienConNombreDeSuVida_QueQuedaComoRival()
        {
            // Los paquetes reales, con el engaño seguro cada semana para no esperar años.
            ContentDatabase content = PackLoader.LoadAll(RepoPacks.Directory);
            content.Events.First(e => e.Id == "evento.pareja.te_engana").AnnualProbability = 0.999;
            (LifeSession session, Character partner) = Couple(content);
            RelationshipRules.Edit(session.World, partner, session.World.Player).Resentment = 40;

            PendingEvent? pending = PlayUntil(session, "evento.pareja.te_engana", 3 * GameDate.WeeksPerYear);

            Assert.NotNull(pending);
            Character lover = session.World.GetCharacter(pending!.PersonId!.Value);
            // Alguien a quien tu pareja le podría gustar, no tú.
            Assert.True(CoupleRules.AttractedTo(lover, partner) && CoupleRules.AttractedTo(partner, lover));
            session.Choose(1);
            Assert.Null(session.World.Player.PartnerId);
            Assert.Equal(RelationshipBond.Rival, session.World.Player.RelationshipWith(lover.Id)!.Bond);
        }

        [Fact]
        public void UnViajeDeUnaSemana_SeHaceDesdeLaFicha_YQuedaLaFoto()
        {
            (LifeSession session, Character partner) = Couple(RepoPacks.Content);
            var routine = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                routine.Set(day, RoutineBlock.Afternoon, "actividad.tele");
                routine.Set(day, RoutineBlock.Night, "actividad.tele");
            }

            session.World.Player.Routine = routine;
            session.World.Player.Money = 5_000_000;
            decimal before = session.World.Player.Money;
            var presenter = new LifePresenter(session);
            Assert.Contains(presenter.PersonCard(partner.Id)!.Actions, a => a.Id == "accion.pareja.viaje");

            presenter.DoAction("accion.pareja.viaje", partner.Id);

            Assert.True(session.World.Player.Money < before);
            Assert.True(Eval(session, "jugador.tiene('foto_viaje_pareja')").AsBool("foto"));
        }

        [Fact]
        public void LaInfidelidad_EsSoloEntreAdultos()
        {
            foreach (EventDefinition ev in RepoPacks.Content.Events.Where(e => e.Id == "evento.pareja.tentacion" || e.Id == "evento.pareja.te_engana"))
            {
                Assert.Contains("jugador.edad >= 18", ev.Conditions);
            }

            (LifeSession session, _) = Couple(RepoPacks.Content, 17);
            Character teen = FamilyFactory.CreateStranger(session.World, RepoPacks.Content, session.World.Player);
            Assert.False(Eval(session, "persona.hay_quimica", teen).AsBool("quimica"));
        }
    }
}
