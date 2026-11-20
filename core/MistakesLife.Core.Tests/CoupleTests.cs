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
    /// La pareja por etapas (D4d1): siempre con alguien real de tu vida, que avanza por lo que
    /// hacen los dos (salir, pololear, vivir juntos, casarse, terminar) y la viudez.
    /// </summary>
    public class CoupleTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(LifeSession session, string effect, Character? person = null) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false, person));

        private static Value Eval(LifeSession session, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true));

        /// <summary>Una vida de esa edad, heterosexual, con tardes libres para hacer cosas con la gente.</summary>
        private static LifeSession Life(int age, ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            Character player = session.World.Player;
            player.Orientation = Orientation.Heterosexual;
            player.GenderIdentity = player.Gender;
            var routine = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                routine.Set(day, RoutineBlock.Afternoon, "actividad.tele");
                routine.Set(day, RoutineBlock.Night, "actividad.dormir_temprano");
            }

            player.Routine = routine;
            return session;
        }

        private static Character Player(LifeSession session) => session.World.Player;

        /// <summary>Alguien nuevo que te podría gustar y que te quiere así de tanto.</summary>
        private static Character Someone(LifeSession session, int affection)
        {
            Character other = FamilyFactory.CreateStranger(session.World, Content, Player(session));
            RelationshipRules.Edit(session.World, other, Player(session)).Affection = affection;
            return other;
        }

        private static void Weeks(LifeSession session, int weeks) =>
            new WeekCycle().AdvanceWeeks(session.World, weeks);

        [Fact]
        public void AlguienNuevo_TePuedeGustar_YNoEsTuFamilia()
        {
            LifeSession session = Life(25);
            Character other = Someone(session, 30);
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);

            Assert.True(CoupleRules.CanDate(session.World, Player(session), other));
            Assert.NotEqual(Player(session).GenderIdentity, other.GenderIdentity);
            Assert.True(other.AgeInYears(session.World.Date) >= 18);
            Assert.False(CoupleRules.CouldAsk(session.World, Player(session), mother));
        }

        [Fact]
        public void UnMenor_SoloSaleConAlguienDeEdadParecida()
        {
            LifeSession session = Life(15);
            Character teenMate = Someone(session, 80);
            var adult = new Character(session.World.NextCharacterId(), "Adulta", "Prueba", teenMate.Sex,
                session.World.Date.AddWeeks(-25 * GameDate.WeeksPerYear), new Personality(50, 50, 50, 50, 50), new Stats(80, 80, 50));
            adult.Gender = teenMate.Gender;
            adult.GenderIdentity = teenMate.GenderIdentity;
            adult.Orientation = Orientation.Bisexual;
            session.World.AddCharacter(adult);
            Character teen = Someone(session, 80);

            Assert.False(CoupleRules.CanDate(session.World, Player(session), adult));
            Assert.InRange(teen.AgeInYears(session.World.Date), 14, 17);
            Assert.True(CoupleRules.CanDate(session.World, Player(session), teen));
        }

        [Fact]
        public void InvitarAUnaCita_AQuienTeQuiere_EmpiezanASalir_YLaFichaLoDice()
        {
            LifeSession session = Life(25);
            Character other = Someone(session, 60);
            var presenter = new LifePresenter(session);
            Assert.Contains(presenter.PersonCard(other.Id)!.Actions, a => a.Id == "accion.pareja.invitar_cita");

            presenter.DoAction("accion.pareja.invitar_cita", other.Id);

            Assert.Equal(other.Id, Player(session).PartnerId);
            Assert.Equal(Player(session).Id, other.PartnerId);
            Assert.Equal(CoupleStage.Dating, Player(session).CoupleStage);
            Assert.Equal("saliendo", Eval(session, "jugador.etapa_pareja").AsText("etapa"));
            Assert.Contains("Saliendo desde esta semana", new LifePresenter(session).PersonCard(other.Id)!.Subtitle);
            // Con pareja ya no se invita a otra persona a una cita.
            Character third = Someone(session, 80);
            Assert.DoesNotContain(new LifePresenter(session).PersonCard(third.Id)!.Actions, a => a.Id == "accion.pareja.invitar_cita");
        }

        [Fact]
        public void InvitarAUnaCita_AQuienNoLeGustas_DiceQueNo()
        {
            LifeSession session = Life(25);
            Character other = Someone(session, 80);
            // Le gustan las personas de su mismo género: tú no.
            other.Orientation = Orientation.Homosexual;
            var presenter = new LifePresenter(session);
            Assert.Contains(presenter.PersonCard(other.Id)!.Actions, a => a.Id == "accion.pareja.invitar_cita");

            ActionResultView result = presenter.DoAction("accion.pareja.invitar_cita", other.Id);

            Assert.Null(Player(session).PartnerId);
            Assert.Contains("no te ve de esa forma", result.Text);
        }

        [Fact]
        public void PedirPololeo_DespuesDeUnasSemanas_YLuegoVivirJuntos()
        {
            LifeSession session = Life(27);
            Character other = Someone(session, 60);
            session.DoAction("accion.pareja.invitar_cita", other.Id);
            Assert.DoesNotContain(new LifePresenter(session).PersonCard(other.Id)!.Actions, a => a.Id == "accion.pareja.pedir_pololeo");

            Weeks(session, 5);
            RelationshipRules.Edit(session.World, other, Player(session)).Affection = 70;
            new LifePresenter(session).DoAction("accion.pareja.pedir_pololeo", other.Id);
            Assert.Equal(CoupleStage.Steady, Player(session).CoupleStage);
            Assert.Equal(CoupleStage.Steady, other.CoupleStage);
            Assert.False(CoupleRules.LivesWithPartner(session.World, Player(session)));

            Run(session, "jugador.arrendar('vivienda.departamento')");
            Weeks(session, 30);
            RelationshipRules.Edit(session.World, other, Player(session)).Affection = 80;
            RelationshipRules.Edit(session.World, other, Player(session)).Trust = 70;
            new LifePresenter(session).DoAction("accion.pareja.vivir_juntos", other.Id);

            Assert.Equal(CoupleStage.LivingTogether, Player(session).CoupleStage);
            Assert.True(CoupleRules.LivesWithPartner(session.World, Player(session)));
            Assert.Equal(Player(session).Residence!.CommuneId, other.Residence!.CommuneId);
            Assert.True(Eval(session, "jugador.vive_con_pareja").AsBool("vive"));
        }

        [Fact]
        public void Terminar_QuedanComoEx()
        {
            LifeSession session = Life(25);
            Character other = Someone(session, 60);
            session.DoAction("accion.pareja.invitar_cita", other.Id);

            new LifePresenter(session).DoAction("accion.pareja.terminar", other.Id);

            Assert.Null(Player(session).PartnerId);
            Assert.Null(Player(session).CoupleStage);
            Assert.Equal(RelationshipBond.Ex, Player(session).RelationshipWith(other.Id)!.Bond);
        }

        [Fact]
        public void AlCasarse_SeVanAVivirJuntos()
        {
            LifeSession session = Life(30);
            Character other = Someone(session, 60);
            session.DoAction("accion.pareja.invitar_cita", other.Id);
            Run(session, "jugador.pololear()");

            Run(session, "jugador.unirse('union.chile.matrimonio')");

            Assert.Equal(CoupleStage.LivingTogether, Player(session).CoupleStage);
            Assert.Contains("Casados", new LifePresenter(session).PersonCard(other.Id)!.Subtitle);
        }

        [Fact]
        public void EnElCarrete_ConocesAAlguienConNombre_QueQuedaComoConocido()
        {
            LifeSession session = Life(22);
            for (int day = 0; day < Routine.Days; day++)
            {
                Player(session).Routine!.Set(day, RoutineBlock.Night, "actividad.carrete");
            }

            int before = Player(session).Relationships.Count;
            for (int week = 0; week < 3 * GameDate.WeeksPerYear && session.Pending?.EventId != "evento.pareja.conocer.carrete"; week++)
            {
                while (session.Pending != null && session.Pending.EventId != "evento.pareja.conocer.carrete")
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                if (session.Pending == null)
                {
                    session.Advance(1);
                }
            }

            PendingEvent pending = Assert.IsType<PendingEvent>(session.Pending);
            Assert.Equal("evento.pareja.conocer.carrete", pending.EventId);
            Character met = session.World.GetCharacter(pending.PersonId!.Value);
            Assert.Equal(RelationshipBond.None, Player(session).RelationshipWith(met.Id)!.Bond);
            Assert.True(CoupleRules.CanDate(session.World, Player(session), met));
            Assert.True(Player(session).Relationships.Count > before);
            Assert.Contains(new LifePresenter(session).People().Acquaintances, r => r.Id == met.Id);
        }

        [Fact]
        public void QuienTeQuiere_TeInvitaASalir_YSiDicesQueSi_EstanSaliendo()
        {
            LifeSession session = Life(24);
            Character other = Someone(session, 70);
            var seen = new List<string>();
            for (int week = 0; week < 3 * GameDate.WeeksPerYear && Player(session).PartnerId == null; week++)
            {
                while (session.Pending != null)
                {
                    seen.Add(session.Pending.EventId);
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                RelationshipRules.Edit(session.World, other, Player(session)).Affection = 70;
                session.Advance(1);
            }

            Assert.Contains("evento.pareja.te_invita", seen);
            Assert.NotNull(Player(session).PartnerId);
            Assert.True(CoupleRules.Stage(Player(session)).HasValue);
        }

        [Fact]
        public void SiMuereTuPareja_QuedasEnViudez_YSigueEnTuGente()
        {
            LifeSession session = Life(60);
            Character other = Someone(session, 60);
            session.DoAction("accion.pareja.invitar_cita", other.Id);
            Run(session, "jugador.convivir()");

            other.Die(session.World.Date, Character.CauseNatural);
            FamilyFactory.EndPartnership(session.World, other, breakUp: false);
            Weeks(session, 3);

            Assert.Equal(other.Id, Player(session).LatePartnerId);
            Assert.True(Eval(session, "jugador.en_viudez").AsBool("viudez"));
            Assert.Equal(3, Eval(session, "jugador.semanas_viudez").AsNumber("semanas"));
            PersonRowView row = Assert.Single(new LifePresenter(session).People().Close, r => r.Id == other.Id);
            Assert.Equal("Fue tu pareja", row.Relation);
            // Recién enviudado, no invitas a nadie a una cita.
            Character someone = Someone(session, 80);
            Assert.DoesNotContain(new LifePresenter(session).PersonCard(someone.Id)!.Actions, a => a.Id == "accion.pareja.invitar_cita");
        }

        [Fact]
        public void AlGuardarYCargar_SeAcuerdaDeLaEtapaYDeLaViudez()
        {
            LifeSession session = Life(30);
            Character other = Someone(session, 60);
            session.DoAction("accion.pareja.invitar_cita", other.Id);
            Run(session, "jugador.pololear()");
            Player(session).LatePartnerId = 3;

            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());

            Assert.Equal(CoupleStage.Steady, loaded.World.Player.CoupleStage);
            Assert.Equal(Player(session).CoupleSince, loaded.World.Player.CoupleSince);
            Assert.Equal(CoupleStage.Steady, loaded.World.GetCharacter(other.Id).CoupleStage);
            Assert.Equal(3, loaded.World.Player.LatePartnerId);
        }

        [Fact]
        public void NingunEvento_InventaUnaPareja()
        {
            Assert.DoesNotContain(Content.Events, e => e.Id == "evento.conocer_pareja" || e.Id == "evento.pololeo");
            Assert.DoesNotContain(Content.Events, e => e.Effects.Concat(e.Options.SelectMany(o => o.Effects)).Any(x => x.Contains("formar_pareja") || x.Contains("pololeando")));
        }

        [Fact]
        public void ConAlguienNuevo_LasCondicionesNoPuedenUsarPersona()
        {
            var content = new ContentDatabase();
            content.AddEventsJson("pack.prueba", "eventos.json", @"[
              { ""id"": ""evento.prueba.conocer"", ""con"": ""alguien_nuevo"", ""condiciones"": ""persona.carino > 10"", ""probabilidad_anual"": 0.1 }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.prueba.conocer") && e.Message.Contains("persona"));
        }
    }
}
