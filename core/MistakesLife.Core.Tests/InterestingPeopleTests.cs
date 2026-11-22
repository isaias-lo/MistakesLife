using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// La gente interesante de tu horario (D4b2): gente real con algo que darte, donde de verdad
    /// la verías; lo que aporta se descubre al conversar.
    /// </summary>
    public class InterestingPeopleTests
    {
        private const string Desarrollo = "carrera.software.area.desarrollo";
        private const string Instituto = "carrera.software.nivel.analista_programador";

        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        private static Value Eval(LifeSession session, string expression, Character person) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true, person));

        /// <summary>Juega semanas eligiendo siempre la primera opción.</summary>
        private static void Weeks(LifeSession session, int weeks)
        {
            for (int i = 0; i < weeks && session.World.Player.IsAlive; i++)
            {
                while (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }

            // Lo que quedó en pantalla al final (una tarea de la pega, D5b) se contesta también,
            // para que la prueba pueda hacer lo suyo con el tiempo parado.
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }
        }

        /// <summary>
        /// Una vida de 25 años, titulada de analista programador, que entra a programar en una
        /// empresa de ese tamaño (la primera partida que llega a una).
        /// </summary>
        private static LifeSession Developer(string sizeId = "empresa.tamano.grande", ulong firstSeed = 1)
        {
            for (ulong seed = firstSeed; seed < firstSeed + 200; seed++)
            {
                var session = new LifeSession(Content, seed, "pais.chile");
                session.TimeMachine.Enabled = false;
                new WeekCycle().AdvanceWeeks(session.World, (25 * GameDate.WeeksPerYear) - session.World.Player.AgeInWeeks(session.World.Date), out _);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                if (session.World.Player.Job != null)
                {
                    Run(session, "jugador.dejar_trabajo()");
                }

                if (session.World.Player.Studies != null)
                {
                    Run(session, "jugador.desertar()");
                }

                Run(session, $"estudiar('{Instituto}')");
                Run(session, "jugador.titularse()");
                Run(session, $"jugador.trabajar_en('{Desarrollo}')");
                if (session.World.CompanyById(session.World.Player.Job?.CompanyId)?.SizeId == sizeId)
                {
                    FreeAfternoons(session);
                    return session;
                }
            }

            throw new InvalidOperationException("Ninguna partida llegó a una empresa de ese tamaño.");
        }

        /// <summary>Un horario con tardes libres (la tele), para poder hacer acciones.</summary>
        private static void FreeAfternoons(LifeSession session)
        {
            var routine = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                routine.Set(day, RoutineBlock.Afternoon, "actividad.tele");
                routine.Set(day, RoutineBlock.Night, "actividad.tele");
            }

            session.World.Player.Routine = routine;
        }

        /// <summary>Juega hasta que aparece alguien interesante (como mucho un año).</summary>
        private static Character FirstInteresting(LifeSession session)
        {
            for (int week = 0; week < GameDate.WeeksPerYear; week++)
            {
                Weeks(session, 1);
                Character? person = InterestingPeopleRules.Shown(session.World, session.Content, session.World.Player).FirstOrDefault();
                if (person != null)
                {
                    return person;
                }
            }

            throw new InvalidOperationException("No apareció nadie interesante en un año.");
        }

        [Fact]
        public void EnTuPega_AlRatoAparece_AlguienConUnCargoMasAltoEnTuMismaEmpresa_QueNoEsTuConocido()
        {
            LifeSession session = Developer();
            Character player = session.World.Player;
            Character person = FirstInteresting(session);

            Assert.Equal(player.Job!.CompanyId, person.Job!.CompanyId);
            Assert.True(Content.GetCareerPosition(person.Job.Position!.PositionId).Depth > Content.GetCareerPosition(player.Job.Position!.PositionId).Depth);
            Assert.Equal(InterestingPeopleRules.WorkSource, InterestingPeopleRules.Entry(player, person.Id)!.Source);
            Assert.True(player.Job.MonthsInJob >= InterestingPeopleRules.MonthsToSettle);

            // Todavía no se conocen: está en la franja, no en tus conocidos.
            PeopleView people = new LifePresenter(session).People();
            PersonRowView row = Assert.Single(people.Interesting, r => r.Id == person.Id);
            Assert.Equal(0, row.Closeness);
            Assert.Contains(T(session, person.Job.Position.PositionId), row.Relation);
            Assert.DoesNotContain(people.Acquaintances, r => r.Id == person.Id);
            Assert.Null(player.RelationshipWith(person.Id));
        }

        [Fact]
        public void LaFicha_AntesDeConversar_EscondeLoQueAporta_YSoloDejaAcercarte()
        {
            LifeSession session = Developer();
            Character person = FirstInteresting(session);

            PersonCardView card = new LifePresenter(session).PersonCard(person.Id)!;

            Assert.True(card.OffersHidden);
            Assert.Empty(card.Offers);
            Assert.Equal(new[] { "accion.acercarte" }, card.Actions.Select(a => a.Id).ToArray());
        }

        [Fact]
        public void SiTePesca_SeConocen_PasaATusConocidos_YSeVeLoQueTePuedeAportar()
        {
            LifeSession session = Developer();
            Character player = session.World.Player;
            Character person = FirstInteresting(session);
            RelationshipRules.Edit(session.World, person, player).Affection = 60;

            var presenter = new LifePresenter(session);
            presenter.DoAction("accion.acercarte", person.Id);

            Assert.True(InterestingPeopleRules.Met(player, person.Id));
            PeopleView people = presenter.People();
            Assert.DoesNotContain(people.Interesting, r => r.Id == person.Id);
            PersonRowView row = Assert.Single(people.Acquaintances, r => r.Id == person.Id);
            Assert.Equal(T(session, "ui.relacion.interesante.pega." + (person.Gender == Gender.Female ? "f" : "m")), row.Relation);
            PersonCardView card = presenter.PersonCard(person.Id)!;
            Assert.False(card.OffersHidden);
            // Trabaja en software como tú: te puede dar un consejo de lo tuyo.
            Assert.Contains(T(session, "aporta.consejo_pega"), card.Offers);
            Assert.Equal(1, Eval(session, "jugador.gente_interesante_conocida", person).AsNumber("conocida"));
        }

        [Fact]
        public void SiNoTePescaDosVeces_SeCierraLaPuerta()
        {
            LifeSession session = Developer();
            Character player = session.World.Player;
            Character person = FirstInteresting(session);
            RelationshipRules.Edit(session.World, person, player).Affection = 0;
            Run(session, "jugador.desempeno = 50");
            var presenter = new LifePresenter(session);

            presenter.DoAction("accion.acercarte", person.Id);
            Assert.Contains(InterestingPeopleRules.Shown(session.World, Content, player), p => p.Id == person.Id);
            Weeks(session, 5);
            presenter.DoAction("accion.acercarte", person.Id);

            Assert.True(InterestingPeopleRules.Closed(InterestingPeopleRules.Entry(player, person.Id)!));
            Assert.DoesNotContain(InterestingPeopleRules.Shown(session.World, Content, player), p => p.Id == person.Id);
            Assert.Null(player.RelationshipWith(person.Id));
        }

        [Fact]
        public void SiDejasLaPega_YaNoLaVes()
        {
            LifeSession session = Developer();
            Character person = FirstInteresting(session);

            Run(session, "jugador.dejar_trabajo()");

            Assert.DoesNotContain(InterestingPeopleRules.Shown(session.World, Content, session.World.Player), p => p.Id == person.Id);
        }

        [Fact]
        public void EnUnaEmpresaChica_SoloAparecenCargosQueExistenEnEsaEmpresa()
        {
            LifeSession session = Developer("empresa.tamano.micro");
            Weeks(session, GameDate.WeeksPerYear);
            Character player = session.World.Player;
            int max = CompanyRules.MaxDepth(session.World, Content, player)!.Value;

            foreach (InterestingPerson entry in player.InterestingPeople.Where(e => e.Source == InterestingPeopleRules.WorkSource))
            {
                Character person = session.World.GetCharacter(entry.PersonId);
                if (person.Job?.CompanyId == player.Job?.CompanyId)
                {
                    Assert.True(Content.GetCareerPosition(person.Job!.Position!.PositionId).Depth <= max);
                }
            }
        }

        [Fact]
        public void QuienMandaEnTuArea_TePuedeRecomendar_YLlegaElAscenso()
        {
            // Una partida donde la primera persona que aparece manda en tu área.
            LifeSession? found = null;
            Character? manager = null;
            for (ulong seed = 1; seed < 60 && found == null; seed++)
            {
                LifeSession candidate = Developer(firstSeed: seed);
                Character first = FirstInteresting(candidate);
                if (InterestingPeopleRules.ManagesYourArea(Content, candidate.World.Player, first))
                {
                    found = candidate;
                    manager = first;
                }
            }

            LifeSession session = found!;
            Character player = session.World.Player;
            Character person = manager!;
            RelationshipRules.Edit(session.World, person, player).Affection = 80;
            RelationshipRules.Edit(session.World, person, player).Trust = 80;
            var presenter = new LifePresenter(session);
            presenter.DoAction("accion.acercarte", person.Id);
            for (int i = 0; i < 40; i++)
            {
                CareerLadder.AdvanceMonth(player);
            }

            Run(session, "jugador.desempeno = 90");
            Assert.True(Eval(session, "persona.manda_en_tu_area", person).AsBool("manda"));
            Assert.Contains(T(session, "aporta.recomendacion_ascenso"), presenter.PersonCard(person.Id)!.Offers);
            string before = player.Job!.Position!.PositionId;
            // Cinco semanas para que la acción quede disponible. Si en ese tiempo llega sola una
            // oferta de ascenso, se rechaza: esta prueba mira el ascenso por recomendación.
            for (int week = 0; week < 5; week++)
            {
                session.Advance(1);
                while (session.Pending != null)
                {
                    PendingEvent pending = session.Pending;
                    EventDefinition ev = Content.Events.Single(e => e.Id == pending.EventId);
                    int option = pending.AvailableOptions[0];
                    if (ev.Id == "evento.oferta_ascenso")
                    {
                        option = pending.AvailableOptions.Single(i => ev.Options[i].TextKey == "opcion.oferta_ascenso.rechazar");
                    }

                    session.Choose(option);
                }
            }

            presenter.DoAction("accion.recomendacion_ascenso", person.Id);
            for (int week = 0; week < GameDate.WeeksPerYear && player.Job!.Position!.PositionId == before; week++)
            {
                if (session.Pending?.EventId == "evento.ascenso_recomendado")
                {
                    Assert.Equal(person.Id, session.Pending.PersonId);
                    session.Choose(0);
                    break;
                }

                Weeks(session, 1);
            }

            Assert.NotEqual(before, player.Job!.Position!.PositionId);
        }

        [Fact]
        public void EnLaU_AlguienQueHaceClasesEnTuCarrera_YTrabajaEnElla()
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            new WeekCycle().AdvanceWeeks(session.World, (18 * GameDate.WeeksPerYear) + 10 - session.World.Player.AgeInWeeks(session.World.Date), out _);
            Run(session, "estudiar('carrera.software.nivel.ingenieria_civil_computacion')");
            Weeks(session, 1);
            Character player = session.World.Player;
            Assert.True(Education.UniversityRules.InSection(player));

            Character person = FirstInteresting(session);
            Assert.Equal(InterestingPeopleRules.StudiesSource, InterestingPeopleRules.Entry(player, person.Id)!.Source);
            Assert.Equal("carrera.software", Content.GetCareerArea(person.Job!.Position!.AreaId).CareerId);
            Assert.StartsWith("Hace clases en tu carrera", new LifePresenter(session).People().Interesting.Single(r => r.Id == person.Id).Life);
            return;
        }


        [Fact]
        public void AlGuardarYCargar_SeAcuerdaDeQuienApareceYDeLasVecesQueNoTePesco()
        {
            LifeSession session = Developer();
            Character person = FirstInteresting(session);
            InterestingPeopleRules.Decline(session.World, session.World.Player, person);

            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());

            InterestingPerson entry = Assert.Single(loaded.World.Player.InterestingPeople, e => e.PersonId == person.Id);
            Assert.Equal(1, entry.Declines);
            Assert.Equal(InterestingPeopleRules.WorkSource, entry.Source);
            Assert.Equal(InterestingPeopleRules.Entry(session.World.Player, person.Id)!.Since, entry.Since);
        }

        private static string T(LifeSession session, string key) => session.Content.Text(key);
    }
}
