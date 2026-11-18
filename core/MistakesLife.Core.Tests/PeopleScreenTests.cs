using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Tu gente: la lista de cercanos y conocidos y la ficha de cada persona (D4a en docs/roadmap.md).</summary>
    public class PeopleScreenTests
    {
        /// <summary>Una vida en Chile jugada sola (decide el simulador) hasta esa edad.</summary>
        private static LifeSession Life(ulong seed, int age)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            var decider = new AutoDecider(RepoPacks.Content);
            var random = new GameRandom(seed).Fork("prueba.gente");
            while (session.World.Player.IsAlive && session.PlayerAgeInYears < age)
            {
                if (session.Pending != null)
                {
                    session.Choose(decider.Choose(session.World, session.Pending, random));
                }
                else
                {
                    session.Advance(4);
                }
            }

            // Termina sin un momento esperando: con uno encima no se puede hacer nada más.
            while (session.Pending != null)
            {
                session.Choose(decider.Choose(session.World, session.Pending, random));
            }

            return session;
        }

        [Fact]
        public void AlNacer_LaMamaYElPapa_SonCercanos_YNoHayConocidos()
        {
            var session = new LifeSession(RepoPacks.Content, 3, "pais.chile");
            PeopleView people = new LifePresenter(session).People();

            Assert.Contains(people.Close, p => p.Id == session.World.Player.MotherId && p.Relation == "Mamá");
            Assert.Contains(people.Close, p => p.Id == session.World.Player.FatherId && p.Relation == "Papá");
            Assert.All(people.Close, p => Assert.Equal(2, p.Initials.Length));
            Assert.Empty(people.Acquaintances);
            Assert.Equal("Conocidos · 0", people.AcquaintancesLabel);
        }

        [Fact]
        public void EnElColegio_LosCompanerosSonConocidos_YSuVidaDiceTuCurso()
        {
            LifeSession session = Life(1, 10);
            var presenter = new LifePresenter(session);
            SchoolClassView cls = presenter.SchoolClass()!;
            PeopleView people = presenter.People();

            PersonRowView mate = people.Acquaintances.First(p => cls.Classmates.Any(c => c.Id == p.Id));
            Assert.Contains(mate.Relation, new[] { "Compañera", "Compañero" });
            Assert.Equal("Tu curso, " + cls.Name, mate.Life);
            // Nadie aparece dos veces, y nadie de los cercanos está también en los conocidos.
            var ids = people.Close.Concat(people.Acquaintances).Select(p => p.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.Equal($"Conocidos · {people.Acquaintances.Count}", people.AcquaintancesLabel);
        }

        [Fact]
        public void UnaAmistad_VaEnCercanos_ConSuBarraYSuPalabra()
        {
            LifeSession session = Life(2, 12);
            Effect.Parse("jugador.conocer('amistad')").Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));
            Character friend = RelationshipRules.WithBond(session.World, session.World.Player, RelationshipBond.Friend).OrderBy(c => c.Id).Last();

            PersonRowView row = new LifePresenter(session).People().Close.Single(p => p.Id == friend.Id);

            Assert.Equal("Amistad", row.Relation);
            Assert.Equal(RelationshipRules.Feelings(session.World, friend, session.World.Player).Affection, row.Closeness);
            Assert.NotEqual(string.Empty, row.Feeling);
        }

        [Fact]
        public void QuienMurio_VaAlFinalDeLosCercanos_SinBarraNiPalabra()
        {
            var session = new LifeSession(RepoPacks.Content, 3, "pais.chile");
            Character mother = session.World.GetCharacter(session.World.Player.MotherId!.Value);
            mother.Die(session.World.Date);

            PeopleView people = new LifePresenter(session).People();
            PersonRowView last = people.Close.Last();

            Assert.Equal(mother.Id, last.Id);
            Assert.False(last.IsAlive);
            Assert.Equal(0, last.Closeness);
            Assert.Equal(string.Empty, last.Feeling);
            Assert.StartsWith("Falleció a los", last.Life);
        }

        [Fact]
        public void LaFicha_MuestraLoVividoJuntos_LoMasNuevoPrimero()
        {
            LifeSession session = Life(1, 16);
            var presenter = new LifePresenter(session);
            int person = session.World.EventHistory
                .Where(r => r.CharacterId == session.World.Player.Id && r.PersonId.HasValue && r.ChosenOption.HasValue)
                .GroupBy(r => r.PersonId!.Value)
                .OrderByDescending(g => g.Count())
                .First().Key;

            PersonCardView card = presenter.PersonCard(person)!;

            Assert.NotEmpty(card.Lived);
            Assert.True(card.Lived.Count <= LifePresenter.MaxLived);
            Assert.All(card.Lived, l => Assert.StartsWith("A los ", l.When));
            Assert.All(card.Lived, l => Assert.NotEqual(string.Empty, l.Text));
            Assert.Contains("años", card.Subtitle);
            Assert.Equal(card.Person.Life, card.Life);
        }

        [Fact]
        public void LaFicha_TraeLoQuePuedenHacerJuntos_ConLasTardesLibres()
        {
            LifeSession session = Life(2, 12);
            Effect.Parse("jugador.conocer('amistad')").Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));
            Character friend = RelationshipRules.WithBond(session.World, session.World.Player, RelationshipBond.Friend).OrderBy(c => c.Id).Last();
            var presenter = new LifePresenter(session);

            PersonCardView card = presenter.PersonCard(friend.Id)!;

            Assert.Contains(card.Actions, a => a.Id == "accion.juntarse_amigo" && a.PersonId == friend.Id);
            Assert.All(card.Actions, a => Assert.Equal(friend.Id, a.PersonId));
        }

        [Fact]
        public void LaFicha_DeAlguienQueNoExiste_EsNula()
        {
            var session = new LifeSession(RepoPacks.Content, 3, "pais.chile");
            var presenter = new LifePresenter(session);

            Assert.Null(presenter.PersonCard(999999));
            Assert.Null(presenter.PersonCard(session.World.Player.Id));
        }
    }
}
