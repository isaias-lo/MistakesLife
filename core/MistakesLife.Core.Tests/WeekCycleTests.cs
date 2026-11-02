using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class WeekCycleTests
    {
        private sealed class RecordingSystem : IWeeklySystem
        {
            private readonly List<string> _log;

            public RecordingSystem(string id, WeekPhase phase, List<string> log)
            {
                Id = id;
                Phase = phase;
                _log = log;
            }

            public string Id { get; }

            public WeekPhase Phase { get; }

            public void Tick(World world, WeekContext context) => _log.Add(Id);
        }

        private sealed class StopAtWeekSystem : IWeeklySystem
        {
            private readonly int _week;

            public StopAtWeekSystem(int week) => _week = week;

            public string Id => "prueba.detener";

            public WeekPhase Phase => WeekPhase.Events;

            public void Tick(World world, WeekContext context)
            {
                if (context.Date.TotalWeeks == _week)
                {
                    context.RequestStop("evento.dia_clave");
                }
            }
        }

        /// <summary>Cambia la felicidad del jugador al azar cada semana.</summary>
        private sealed class RandomMoodSystem : IWeeklySystem
        {
            public string Id => "prueba.animo";

            public WeekPhase Phase => WeekPhase.Systems;

            public void Tick(World world, WeekContext context)
            {
                world.Player.Stats.Happiness += context.RandomFor(Id).NextInt(-3, 4);
            }
        }

        [Fact]
        public void Sistemas_CorrenEnElOrdenDeLasFases_SinImportarElOrdenDeRegistro()
        {
            var log = new List<string>();
            var cycle = new WeekCycle()
                .Register(new RecordingSystem("consecuencias", WeekPhase.Consequences, log))
                .Register(new RecordingSystem("eventos", WeekPhase.Events, log))
                .Register(new RecordingSystem("decisiones", WeekPhase.Decisions, log))
                .Register(new RecordingSystem("semillas", WeekPhase.Seeds, log))
                .Register(new RecordingSystem("personajes", WeekPhase.Characters, log))
                .Register(new RecordingSystem("sistemas", WeekPhase.Systems, log));

            cycle.AdvanceWeek(WorldFactory.CreateNewLife(1));

            Assert.Equal(new[] { "decisiones", "sistemas", "personajes", "semillas", "eventos", "consecuencias" }, log);
        }

        [Fact]
        public void DentroDeUnaFase_SeRespetaElOrdenDeRegistro()
        {
            var log = new List<string>();
            var cycle = new WeekCycle()
                .Register(new RecordingSystem("salud", WeekPhase.Systems, log))
                .Register(new RecordingSystem("dinero", WeekPhase.Systems, log));

            cycle.AdvanceWeek(WorldFactory.CreateNewLife(1));

            Assert.Equal(new[] { "salud", "dinero" }, log);
        }

        [Fact]
        public void RegistrarDosVecesElMismoSistema_Falla()
        {
            var cycle = new WeekCycle().Register(new BirthdaySystem());
            Assert.Throws<InvalidOperationException>(() => cycle.Register(new BirthdaySystem()));
        }

        [Fact]
        public void AvanzarUnaSemana_MueveLaFecha()
        {
            var world = WorldFactory.CreateNewLife(1);
            new WeekCycle().AdvanceWeek(world);
            Assert.Equal(new GameDate(1), world.Date);
        }

        [Fact]
        public void DiaClave_DetieneElTiempo()
        {
            var world = WorldFactory.CreateNewLife(1);
            int simulated = new WeekCycle().Register(new StopAtWeekSystem(9)).AdvanceWeeks(world, 100);
            Assert.Equal(10, simulated);
            Assert.Equal(new GameDate(10), world.Date);
        }

        [Fact]
        public void Cumpleanos_SeAnotanEnElDiario()
        {
            var world = WorldFactory.CreateNewLife(1);
            new WeekCycle().Register(new BirthdaySystem()).AdvanceWeeks(world, GameDate.WeeksPerYear * 3);
            Assert.Equal(3, world.Journal.Count(e => e.TextKey == "diario.cumpleanos"));
        }

        [Fact]
        public void MismaSemilla_ProduceLaMismaVida()
        {
            int[] LiveLife(ulong seed)
            {
                var world = WorldFactory.CreateNewLife(seed);
                var cycle = new WeekCycle().Register(new RandomMoodSystem());
                var history = new List<int>();
                for (int i = 0; i < 200; i++)
                {
                    cycle.AdvanceWeek(world);
                    history.Add(world.Player.Stats.Happiness);
                }

                return history.ToArray();
            }

            Assert.Equal(LiveLife(2026), LiveLife(2026));
            Assert.NotEqual(LiveLife(2026), LiveLife(2027));
        }

        [Fact]
        public void AgregarUnSistemaNuevo_NoCambiaElAzarDeLosDemas()
        {
            int[] LiveLife(bool withExtraSystem)
            {
                var world = WorldFactory.CreateNewLife(99);
                var cycle = new WeekCycle();
                if (withExtraSystem)
                {
                    cycle.Register(new ExtraRandomSystem());
                }

                cycle.Register(new RandomMoodSystem());
                var history = new List<int>();
                for (int i = 0; i < 100; i++)
                {
                    cycle.AdvanceWeek(world);
                    history.Add(world.Player.Stats.Happiness);
                }

                return history.ToArray();
            }

            Assert.Equal(LiveLife(false), LiveLife(true));
        }

        private sealed class ExtraRandomSystem : IWeeklySystem
        {
            public string Id => "prueba.extra";

            public WeekPhase Phase => WeekPhase.Decisions;

            public void Tick(World world, WeekContext context)
            {
                var random = context.RandomFor(Id);
                for (int i = 0; i < 10; i++)
                {
                    random.NextULong();
                }
            }
        }
    }
}
