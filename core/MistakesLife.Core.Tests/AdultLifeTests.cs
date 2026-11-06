using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class AdultLifeTests
    {
        /// <summary>Juega con los paquetes reales hasta la edad pedida, eligiendo siempre la primera opción.</summary>
        private static void PlayUntil(LifeSession session, int years)
        {
            int target = years * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }
        }

        private static bool Check(LifeSession session, string condition) =>
            Expression.Parse(condition).IsTrue(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true));

        [Theory]
        [InlineData(Sex.Female, 60)]
        [InlineData(Sex.Male, 65)]
        public void Jubilacion_LlegaALaEdadLegalDeChileSegunElSexo(Sex sex, int legalAge)
        {
            for (ulong seed = 1; seed < 60; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                if (session.World.Player.Sex != sex)
                {
                    continue;
                }

                PlayUntil(session, legalAge - 1);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                session.World.Player.SetFlag("trabajando");
                session.World.Player.ClearFlag("jubilado");
                int before = session.World.EventHistory.Count;
                PlayUntil(session, legalAge + 1);

                // Si quebró su empresa justo antes, ya no trabaja y no se jubila: se prueba otra vida.
                if (!session.World.Player.IsAlive || session.World.EventHistory.Skip(before).Any(e => e.EventId == "evento.empresa.quiebra"))
                {
                    continue;
                }

                EventRecord retirement = session.World.EventHistory.First(e => e.EventId == "evento.jubilacion");
                Assert.Equal(legalAge, session.World.Player.AgeInYears(retirement.Date));
                Assert.True(session.World.Player.HasFlag("jubilado"));
                Assert.False(session.World.Player.HasFlag("trabajando"));
                return;
            }

            Assert.Fail("Ninguna partida llegó viva a la edad de jubilación.");
        }

        [Fact]
        public void EventosDelTrabajo_NoPasanSinTrabajar_PorEjemploDespuesDeJubilar()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            session.World.Player.SetFlag("trabaja_contabilidad");
            string condition = session.Content.Events.Single(e => e.Id == "evento.contabilidad.cierre_anual").Conditions!;

            Assert.False(Check(session, condition));
            session.World.Player.SetFlag("trabajando");
            Assert.True(Check(session, condition));
        }
    }
}
