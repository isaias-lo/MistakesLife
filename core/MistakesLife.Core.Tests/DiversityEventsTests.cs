using System;
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
    public class DiversityEventsTests
    {
        /// <summary>Juega con los paquetes reales hasta la edad pedida, eligiendo siempre la primera opción.</summary>
        private static LifeSession Play(Action<Character> setUp, int years, ulong seed = 11)
        {
            var session = new LifeSession(RepoPacks.Content, seed);
            setUp(session.World.Player);
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

            return session;
        }

        private static bool Saw(LifeSession session, string eventId) =>
            session.World.EventHistory.Any(h => h.EventId == eventId);

        private static Gender Other(Gender gender) => gender == Gender.Female ? Gender.Male : Gender.Female;

        [Fact]
        public void PersonaTransEnChile_SeDescubre_ViveSuIdentidad_YCambiaSuRegistroConLaLey()
        {
            Gender felt = Gender.Female;
            string name = string.Empty;
            LifeSession session = Play(p => { felt = Other(p.Gender); p.GenderIdentity = felt; name = p.FirstName; }, 40);
            Character player = session.World.Player;

            Assert.True(player.HasFlag("siente_identidad"));
            Assert.True(player.HasFlag("vive_identidad"));
            Assert.Equal(felt, player.Gender);
            Assert.NotEqual(name, player.FirstName);
            Assert.Contains(session.World.Journal, e => e.CharacterId == player.Id && e.TextKey == "diario.cambio_registral");
            Assert.True(player.HasFlag("fuera_del_closet"));
        }

        [Fact]
        public void PersonaTransEnChile_NoCambiaSuRegistroAntesDeLaEdadDeLaLey()
        {
            LifeSession session = Play(p => p.GenderIdentity = Other(p.Gender), 13);

            Assert.False(Saw(session, "evento.cambio_registral"));
        }

        [Fact]
        public void PersonaNoBinariaEnChile_NoPuedeCambiarSuRegistro_PorqueLaLeyNoLoPermite()
        {
            Gender registered = Gender.Female;
            LifeSession session = Play(p => { registered = p.Gender; p.GenderIdentity = Gender.NonBinary; }, 30);

            Assert.True(session.World.Player.HasFlag("vive_identidad"));
            Assert.False(Saw(session, "evento.cambio_registral"));
            Assert.Equal(registered, session.World.Player.Gender);
        }

        [Fact]
        public void PersonaGayOLesbiana_DescubreSuAtraccion_SaleDelCloset_YSuFamiliaReacciona()
        {
            LifeSession session = Play(p => p.Orientation = Orientation.Homosexual, 45);
            Character player = session.World.Player;

            Assert.True(Saw(session, "evento.descubrir_atraccion"));
            Assert.True(player.HasFlag("fuera_del_closet"));
            Assert.True(Saw(session, "evento.familia_apoya") || Saw(session, "evento.familia_le_cuesta"));
        }

        [Fact]
        public void PersonaHeterosexualYCis_NoVeEventosDeDescubrirse()
        {
            LifeSession session = Play(p => { p.Orientation = Orientation.Heterosexual; p.GenderIdentity = p.Gender; }, 25);

            Assert.False(Saw(session, "evento.descubrir_atraccion"));
            Assert.False(Saw(session, "evento.identidad_ninez"));
            Assert.False(Saw(session, "evento.salir_del_closet"));
        }

        [Fact]
        public void Lenguaje_CambiarGeneroYNombre_SoloEnEfectos()
        {
            var session = new LifeSession(RepoPacks.Content, 5);
            session.Advance(20 * GameDate.WeeksPerYear);
            Character player = session.World.Player;
            string name = player.FirstName;
            string other = player.Gender == Gender.Female ? "masculino" : "femenino";
            var scope = new EventScope(session.World, session.Content, player, null, readOnly: false);

            Effect.Parse($"jugador.cambiar_genero('{other}')").Apply(scope);
            Effect.Parse("jugador.cambiar_nombre()").Apply(scope);

            Assert.NotEqual(name, player.FirstName);
            Assert.True(Expression.Parse($"jugador.genero == '{other}' && jugador.sexo != jugador.genero").IsTrue(new EventScope(session.World, session.Content, player, null, readOnly: true)));
            Assert.Throws<ScriptException>(() => Effect.Parse("jugador.cambiar_genero('otro')").Apply(scope));
            Assert.Throws<ScriptException>(() => Expression.Parse("jugador.cambiar_nombre() == nada").IsTrue(new EventScope(session.World, session.Content, player, null, readOnly: true)));
        }
    }
}
