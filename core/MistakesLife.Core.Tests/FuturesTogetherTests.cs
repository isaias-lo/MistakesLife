using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R6d: futuros juntos con una persona (recomendarla en tu pega, vivir juntos).</summary>
    public class FuturesTogetherTests
    {
        private const string Vestuario = "oficio.vendedor.area.vestuario";

        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Una vida a los 24 que trabaja con equipo y tiene una amistad nueva (devuelve su id).</summary>
        private static (LifeSession Session, int FriendId) WorkingWithFriend(string country = "pais.chile", ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, country);
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (24 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, "jugador.desmarcar('buscando_trabajo')");
            Run(world, "jugador.marcar('trabajando')");
            Run(world, $"jugador.trabajar_en('{Vestuario}')");
            Weeks(session, 3);
            var before = RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend).Select(c => c.Id).ToList();
            Run(world, "jugador.conocer('amistad')");
            int friend = RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend).Select(c => c.Id).Except(before).Single();
            return (session, friend);
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static void Weeks(LifeSession session, int weeks)
        {
            for (int i = 0; i < weeks; i++)
            {
                ClearPending(session);
                session.Advance(1);
            }

            ClearPending(session);
        }

        private static void ClearPending(LifeSession session)
        {
            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }
        }

        private static void Choose(LifeSession session, string optionKey)
        {
            PendingEvent pending = session.Pending!;
            EventDefinition ev = Content.Events.Single(e => e.Id == pending.EventId);
            session.Choose(pending.AvailableOptions.Single(i => ev.Options[i].TextKey == optionKey));
        }

        [Fact]
        public void Recomendar_ProbabilidadDeContratar_EsLaDeUnaEntrevistaPorContacto()
        {
            // Chile: exito_entrevista 0,35 + exito_extra de "contactos" 0,2.
            Assert.Equal(0.55, ReferralRules.HireChance(Content.GetCountry("pais.chile").Economy!.JobSearch), 6);
            Assert.Equal(0, ReferralRules.HireChance(null));
        }

        [Fact]
        public void Recomendar_AparececomoBotonDeLaAmistadYSiLaContratanQuedaEnTuEquipo()
        {
            int hired = 0, notHired = 0;
            for (ulong seed = 1; seed <= 12; seed++)
            {
                (LifeSession session, int friend) = WorkingWithFriend(seed: seed);
                Assert.Contains(session.RequestableWith(friend), e => e.Id == "evento.futuro.recomendar_amigo");
                Assert.True(session.Request("evento.futuro.recomendar_amigo", friend));
                Assert.Equal(friend, session.Pending!.PersonId);
                Choose(session, "opcion.futuro.recomendar.si");

                World world = session.World;
                bool inTeam = WorkRules.Colleagues(world, world.Player).Any(c => c.Id == friend);
                string journal = world.Journal.Last(j => ReferralRules.Journals.Contains(j.TextKey)).TextKey;
                Assert.Equal(inTeam, journal == ReferralRules.HiredJournal);
                if (inTeam)
                {
                    hired++;
                    Assert.Contains(friend, world.Player.Team!.RecommendedIds);
                    Assert.DoesNotContain(session.RequestableWith(friend), e => e.Id == "evento.futuro.recomendar_amigo");
                }
                else
                {
                    notHired++;
                }
            }

            Assert.True(hired > 0 && notHired > 0, $"contratados {hired}, no {notHired}");
        }

        [Fact]
        public void VivirJuntos_ConArriendo_ElArriendoSeDivideYSeGuarda()
        {
            (LifeSession session, int friend) = WorkingWithFriend();
            World world = session.World;
            Run(world, "jugador.marcar('vive_independiente')");
            Run(world, "jugador.arrendar('vivienda.departamento')");
            Assert.Contains(session.RequestableWith(friend), e => e.Id == "evento.futuro.vivir_juntos");

            Assert.True(session.Request("evento.futuro.vivir_juntos", friend));
            Choose(session, "opcion.futuro.vivir_juntos.con_permiso");
            Assert.Equal(friend, HousingRules.Roommate(world, world.Player)?.Id);

            // El arriendo del mes se paga a medias.
            do
            {
                Weeks(session, 1);
            }
            while (!world.Date.AddWeeks(-1).IsFirstWeekOfMonth);

            if (HousingRules.Roommate(world, world.Player) != null && world.Player.PartnerId == null)
            {
                decimal paid = world.Player.LastExpenses.Single(l => l.Id == "vivienda.departamento").Amount;
                Assert.Equal(System.Math.Round(world.Player.Lease!.MonthlyRent / 2, System.MidpointRounding.AwayFromZero), paid);
            }

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());
            Assert.Equal(world.Player.Lease!.RoommateId, loaded.World.Player.Lease!.RoommateId);
        }

        [Fact]
        public void VivirJuntos_SinPermisoDelDueno_PlantaLaSemilla()
        {
            (LifeSession session, int friend) = WorkingWithFriend();
            World world = session.World;
            Run(world, "jugador.marcar('vive_independiente')");
            Run(world, "jugador.arrendar('vivienda.departamento')");
            Assert.True(session.Request("evento.futuro.vivir_juntos", friend));
            Choose(session, "opcion.futuro.vivir_juntos.sin_permiso");
            Assert.Contains(world.Seeds.All, s => s.TypeId == "semilla.subarriendo_sin_permiso" && s.OwnerId == world.PlayerId);
        }

        [Fact]
        public void VivirJuntos_EnUnaPieza_NoSePuede()
        {
            (LifeSession session, int friend) = WorkingWithFriend();
            World world = session.World;
            Run(world, "jugador.marcar('vive_independiente')");
            Run(world, "jugador.arrendar('vivienda.pieza')");
            Assert.DoesNotContain(session.RequestableWith(friend), e => e.Id == "evento.futuro.vivir_juntos");
        }

        [Fact]
        public void DejarDeVivirJuntos_ConElCompaneroDeCasa_TerminaDeCompartir()
        {
            (LifeSession session, int friend) = WorkingWithFriend();
            World world = session.World;
            Run(world, "jugador.marcar('vive_independiente')");
            Run(world, "jugador.arrendar('vivienda.departamento')");
            Assert.True(session.Request("evento.futuro.vivir_juntos", friend));
            Choose(session, "opcion.futuro.vivir_juntos.con_permiso");

            Assert.Contains(session.RequestableWith(friend), e => e.Id == "evento.futuro.dejar_de_vivir_juntos");
            Assert.True(session.Request("evento.futuro.dejar_de_vivir_juntos", friend));
            Choose(session, "opcion.futuro.dejar_de_vivir_juntos.calma");
            Assert.Null(HousingRules.Roommate(world, world.Player));
        }
    }
}
