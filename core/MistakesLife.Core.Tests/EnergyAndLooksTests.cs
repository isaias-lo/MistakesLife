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
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Energía y Apariencia (D1b en docs/roadmap.md), con las reglas que aprobó Isaias.</summary>
    public class EnergyAndLooksTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(World world, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static double Number(World world, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, Content, world.Player, null, readOnly: true)).AsNumber(expression);

        /// <summary>Una vida de esa edad, sin horario (para medir solo lo demás).</summary>
        private static World Adult(int age)
        {
            var session = new LifeSession(Content, 21);
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, age * GameDate.WeeksPerYear);
            world.Player.Routine = null;
            world.Player.Stats.Health = 80;
            return world;
        }

        [Fact]
        public void Energia_LaPegaYLosHijosChicosCansan()
        {
            World world = Adult(30);
            int rested = EnergyRules.Target(world, Content, world.Player);

            Run(world, "jugador.trabajar(sueldo_minimo(), 'informal')");
            int working = EnergyRules.Target(world, Content, world.Player);
            Assert.Equal(rested - EnergyRules.Work, working);

            Run(world, "jugador.formar_pareja()");
            Run(world, "jugador.tener_hijo()");
            Assert.True(EnergyRules.Target(world, Content, world.Player) <= working - EnergyRules.FirstSmallChild);
        }

        [Fact]
        public void Energia_EstarEnfermoCansa()
        {
            World world = Adult(30);
            int healthy = EnergyRules.Target(world, Content, world.Player);
            world.Player.Stats.Health = 20;
            Assert.Equal(healthy - ((EnergyRules.SickBelow - 20) / 2), EnergyRules.Target(world, Content, world.Player));
        }

        [Fact]
        public void Energia_DormirTempranoDescansaYElCarreteCansa()
        {
            World world = Adult(25);
            Run(world, "jugador.conocer('amistad')"); // para carretear hace falta un amigo
            var calm = new Routine("libre");
            var party = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                calm.Set(day, RoutineBlock.Night, "actividad.dormir_temprano");
                party.Set(day, RoutineBlock.Night, "actividad.carrete");
            }

            world.Player.Routine = calm;
            int rested = EnergyRules.Target(world, Content, world.Player);
            world.Player.Routine = party;
            int tired = EnergyRules.Target(world, Content, world.Player);
            Assert.True(rested > tired + 20, $"Dormir temprano: {rested}; carretear todas las noches: {tired}.");
        }

        [Fact]
        public void Energia_SeCansaRapidoYCuestaMasRecuperarlaConLosAnos()
        {
            Assert.Equal(2, EnergyRules.RecoveryPerWeek(20, 10));
            Assert.Equal(1, EnergyRules.RecoveryPerWeek(45, 10));
            Assert.Equal(1, EnergyRules.RecoveryPerWeek(70, 10));
            Assert.Equal(0, EnergyRules.RecoveryPerWeek(70, 11));

            World world = Adult(30);
            Run(world, "jugador.trabajar(sueldo_minimo(), 'informal')");
            world.Player.Stats.Energy = 95;
            new WeekCycle().Register(new EnergySystem(Content)).AdvanceWeeks(world, 1);
            Assert.Equal(95 - EnergyRules.DrainPerWeek, world.Player.Stats.Energy);
        }

        [Fact]
        public void Energia_CansadoRindesMenos()
        {
            World world = Adult(30);
            world.Player.Stats.Energy = 80;
            Assert.Equal(0, EnergyRules.TiredPenalty(world.Player));
            world.Player.Stats.Energy = 10;
            Assert.Equal((EnergyRules.TiredBelow - 10) / 2, EnergyRules.TiredPenalty(world.Player));
        }

        [Fact]
        public void Energia_SeLeeYSeCambiaDesdeLosPaquetes()
        {
            World world = Adult(30);
            world.Player.Stats.Energy = 40;
            Run(world, "jugador.energia += 25");
            Assert.Equal(65, Number(world, "jugador.energia"));
            Run(world, "jugador.apariencia = 70");
            Assert.Equal(70, Number(world, "jugador.apariencia"));
        }

        [Fact]
        public void Apariencia_SeHeredaDeLaMamaYElPapa()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var session = new LifeSession(Content, seed);
                session.Advance(2);
                World world = session.World;
                Character player = world.Player;
                Assert.True(player.InheritedLooks.HasValue);
                Character mother = world.GetCharacter(player.MotherId!.Value);
                Character father = world.GetCharacter(player.FatherId!.Value);
                int average = (mother.InheritedLooks!.Value + father.InheritedLooks!.Value) / 2;
                Assert.InRange(player.InheritedLooks!.Value, System.Math.Max(LooksRules.MinInherited, average - LooksRules.InheritSpread), System.Math.Min(LooksRules.MaxInherited, average + LooksRules.InheritSpread));
                Assert.Equal(player.InheritedLooks.Value, player.Stats.Looks);
            }
        }

        [Fact]
        public void Apariencia_LaMismaPartidaHeredaLoMismo()
        {
            var a = new LifeSession(Content, 9);
            var b = new LifeSession(Content, 9);
            a.Advance(3);
            b.Advance(3);
            Assert.Equal(a.World.Player.InheritedLooks, b.World.Player.InheritedLooks);
        }

        [Fact]
        public void Apariencia_SubeEnLaJuventudYBajaDesdeLos45()
        {
            Assert.Equal(0, LooksRules.AgeChange(8));
            Assert.Equal(4, LooksRules.AgeChange(25));
            Assert.True(LooksRules.AgeChange(60) < 0);
            Assert.True(LooksRules.AgeChange(75) < LooksRules.AgeChange(60));
        }

        [Fact]
        public void Apariencia_ElDeporteSeNotaYElCarreteTambien()
        {
            World world = Adult(25);
            world.Player.InheritedLooks = 50;
            Run(world, "jugador.conocer('amistad')"); // para carretear hace falta un amigo
            var sport = new Routine("libre");
            var party = new Routine("libre");
            for (int day = 0; day < Routine.Days; day++)
            {
                sport.Set(day, RoutineBlock.Afternoon, "actividad.deporte");
                party.Set(day, RoutineBlock.Night, "actividad.carrete");
            }

            world.Player.Routine = sport;
            int fit = LooksRules.Target(world, Content, world.Player);
            world.Player.Routine = party;
            int partying = LooksRules.Target(world, Content, world.Player);
            Assert.True(fit - partying >= 2 * LooksRules.MaxHabits - 2, $"Con deporte: {fit}; con carrete: {partying}.");
        }

        [Fact]
        public void PantallaPrincipal_MuestraLasCincoBarras()
        {
            var presenter = new LifePresenter(new LifeSession(Content, 5));
            presenter.AdvanceMonth();
            var bars = presenter.LifeBars();
            Assert.Equal(new[] { "felicidad", "salud", "inteligencia", "apariencia", "energia" }, bars.Select(b => b.Id).ToArray());
            Assert.Equal(presenter.Session.World.Player.Stats.Looks, bars[3].Value);
            Assert.Equal(presenter.Session.World.Player.Stats.Energy, bars[4].Value);
        }

        [Fact]
        public void Guardado_RecuerdaLaEnergiaYLaApariencia()
        {
            var session = new LifeSession(Content, 5);
            session.Advance(10);
            Character player = session.World.Player;
            player.Stats.Energy = 37;
            player.Stats.Looks = 61;
            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());
            Assert.Equal(37, loaded.World.Player.Stats.Energy);
            Assert.Equal(61, loaded.World.Player.Stats.Looks);
            Assert.Equal(player.InheritedLooks, loaded.World.Player.InheritedLooks);
        }
    }
}
