using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Lo que puedes hacer con cada persona (D4c): conversar, invitar a salir, pedir ayuda con lo
    /// que de verdad tiene (plata, una recomendación en su pega) y hacer las paces. Cada cosa gasta
    /// una tarde y la persona puede decir que no.
    /// </summary>
    public class PeopleActionsTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        /// <summary>Una vida de esa edad con un horario lleno de deporte salvo cuatro noches de tele (cuatro tardes libres).</summary>
        private static LifeSession Life(int age)
        {
            var session = new LifeSession(Content, 5, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, age * GameDate.WeeksPerYear);
            var routine = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                routine.Set(day, RoutineBlock.Afternoon, "actividad.deporte");
                routine.Set(day, RoutineBlock.Night, day < 4 ? "actividad.tele" : "actividad.dormir_temprano");
            }

            session.World.Player.Routine = routine;
            return session;
        }

        private static Character Player(LifeSession session) => session.World.Player;

        /// <summary>Alguien nuevo de tu pega: un conocido, no una amistad.</summary>
        private static Character NewColleague(LifeSession session)
        {
            Run(session, "jugador.conocer('colega')");
            return RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Colleague).OrderBy(c => c.Id).Last();
        }

        /// <summary>Lo que esa persona siente por ti.</summary>
        private static Relationship Theirs(LifeSession session, Character person) =>
            RelationshipRules.Edit(session.World, person, Player(session));

        [Fact]
        public void ConversarConUnConocido_VaSoloEnSuFicha_YElCarinoSube()
        {
            LifeSession session = Life(25);
            Character colleague = NewColleague(session);
            int before = Theirs(session, colleague).Affection;
            var presenter = new LifePresenter(session);

            Assert.DoesNotContain(presenter.Actions("gente"), a => a.Id == "accion.conversar");
            Assert.Contains(presenter.PersonCard(colleague.Id)!.Actions, a => a.Id == "accion.conversar");

            ActionResultView result = presenter.DoAction("accion.conversar", colleague.Id);

            Assert.Equal(before + 2, Theirs(session, colleague).Affection);
            Assert.Contains(colleague.FirstName, result.Text);
        }

        [Fact]
        public void LaFamilia_NoEsUnConocido()
        {
            LifeSession session = Life(25);
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);

            Assert.DoesNotContain(new LifePresenter(session).PersonCard(mother.Id)!.Actions, a => a.Id == "accion.conversar" || a.Id == "accion.invitar");
        }

        [Fact]
        public void InvitarAUnConocidoQueTeQuiere_NaceLaAmistad()
        {
            LifeSession session = Life(25);
            Character colleague = NewColleague(session);
            Theirs(session, colleague).Affection = 55;

            ActionResultView result = new LifePresenter(session).DoAction("accion.invitar", colleague.Id);

            Assert.Equal(RelationshipBond.Friend, Player(session).RelationshipWith(colleague.Id)!.Bond);
            Assert.Equal(new LifePresenter(session).Text("accion.invitar.resultado").Replace("{persona}", colleague.FirstName), result.Text);
        }

        [Fact]
        public void InvitarAUnConocidoQueNoTeConoceTanto_DiceQueOtroDia_YLaTardeSeGasta()
        {
            LifeSession session = Life(25);
            Character colleague = NewColleague(session);
            Theirs(session, colleague).Affection = 30;
            int left = FreeTimeRules.Left(session.World, Content, Player(session));

            ActionResultView result = new LifePresenter(session).DoAction("accion.invitar", colleague.Id);

            Assert.Equal(RelationshipBond.Colleague, Player(session).RelationshipWith(colleague.Id)!.Bond);
            Assert.Contains("otro día", result.Text);
            Assert.Equal(left - 1, FreeTimeRules.Left(session.World, Content, Player(session)));
            Assert.All(result.Changes, c => Assert.False(c.IsGood));
        }

        [Fact]
        public void PedirPlataALaMama_TePresta_YQuedaLaSemillaDeDevolverla()
        {
            LifeSession session = Life(30);
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);
            mother.WorkStatus = WorkStatus.Retired;
            Theirs(session, mother).Affection = 80;
            Theirs(session, mother).Trust = 70;
            decimal money = Player(session).Money;
            var presenter = new LifePresenter(session);
            Assert.Contains(presenter.PersonCard(mother.Id)!.Actions, a => a.Id == "accion.pedir_plata");

            presenter.DoAction("accion.pedir_plata", mother.Id);

            Assert.True(Player(session).Money > money);
            Seed seed = Assert.Single(session.World.Seeds.OpenFor(Player(session).Id), s => s.TypeId == "semilla.te_prestaron_plata");
            Assert.Contains(mother.Id, seed.Witnesses);
            // Mientras no la devuelva, no se pide otra vez.
            Assert.DoesNotContain(new LifePresenter(session).PersonCard(mother.Id)!.Actions, a => a.Id == "accion.pedir_plata");
        }

        [Fact]
        public void PedirPlata_SoloAQuienTieneDeDonde()
        {
            LifeSession session = Life(30);
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);
            mother.WorkStatus = WorkStatus.Unemployed;

            Assert.DoesNotContain(new LifePresenter(session).PersonCard(mother.Id)!.Actions, a => a.Id == "accion.pedir_plata");
        }

        [Fact]
        public void PedirQueTeRecomiende_EnSuPega_LaBusquedaEsConEsaPersona()
        {
            LifeSession session = Life(30);
            Player(session).Job = null;
            Character mother = session.World.GetCharacter(Player(session).MotherId!.Value);
            for (ulong seed = 1; mother.Job == null; seed++)
            {
                PeopleWorkSystem.Look(session.World, Content, Content.GetCountry("pais.chile").PeopleWork!, mother, 50, new Randomness.GameRandom(seed));
            }

            Theirs(session, mother).Affection = 80;
            Theirs(session, mother).Trust = 70;

            new LifePresenter(session).DoAction("accion.pedir_recomendacion", mother.Id);

            Assert.Equal("busqueda.forma.contactos", Player(session).JobSearch!.MethodId);
            Assert.Equal(mother.Id, Player(session).JobSearch!.ContactId);
        }

        [Fact]
        public void HacerLasPacesConUnRival_SinTantoRencor_DejaDeSerRival()
        {
            LifeSession session = Life(25);
            Run(session, "jugador.conocer('rival')");
            Character rival = RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Rival).OrderBy(c => c.Id).Last();
            Theirs(session, rival).Resentment = 40;

            new LifePresenter(session).DoAction("accion.paces_rival", rival.Id);

            Assert.Equal(RelationshipBond.None, Player(session).RelationshipWith(rival.Id)!.Bond);
            Assert.Equal(20, Theirs(session, rival).Resentment);
        }

        [Fact]
        public void HacerLasPacesConUnRival_ConMuchoRencor_NoQuiereHablar()
        {
            LifeSession session = Life(25);
            Run(session, "jugador.conocer('rival')");
            Character rival = RelationshipRules.WithBond(session.World, Player(session), RelationshipBond.Rival).OrderBy(c => c.Id).Last();
            Theirs(session, rival).Resentment = 80;

            ActionResultView result = new LifePresenter(session).DoAction("accion.paces_rival", rival.Id);

            Assert.Equal(RelationshipBond.Rival, Player(session).RelationshipWith(rival.Id)!.Bond);
            Assert.Equal(80, Theirs(session, rival).Resentment);
            Assert.Contains("no quiso", result.Text);
        }

        [Fact]
        public void AccionConConocidos_FueraDeLaFicha_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddActionsJson("pack.prueba", "acciones.json", @"[
              { ""id"": ""accion.a"", ""grupo"": ""gente"", ""con"": ""conocido"", ""cambia"": { ""carino"": 1 } },
              { ""id"": ""accion.b"", ""grupo"": ""salud"", ""acepta"": ""persona.carino > 50"", ""cambia"": { ""salud"": 1 } },
              { ""id"": ""accion.c"", ""grupo"": ""gente"", ""con"": ""amigo"", ""si_no_acepta"": { ""felicidad"": -1 }, ""cambia"": { ""carino"": 1 } }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.a") && e.Message.Contains("solo_en_ficha"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.b") && e.Message.Contains("con quién"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("accion.c") && e.Message.Contains("acepta"));
        }
    }
}
