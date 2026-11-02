using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class SeedTests
    {
        /// <summary>Condición de prueba que se prende y apaga a mano.</summary>
        private sealed class SwitchCondition : ISeedCondition
        {
            public bool On { get; set; }

            public bool IsMet(World world, Seed seed) => On;
        }

        private static Character AddPerson(World world)
        {
            var person = new Character(world.NextCharacterId(), "nombre", "apellido", Sex.Male, world.Date, new Personality(50, 50, 50, 50, 50), new Stats(80, 80, 50));
            world.AddCharacter(person);
            return person;
        }

        private static SeedDefinition Definition(int strength = 100, int expiresAfterWeeks = 1000) =>
            new SeedDefinition("semilla.prueba", SeedPolarity.Negative, strength, expiresAfterWeeks);

        /// <summary>Reglas sin azar: una semilla posible siempre despierta.</summary>
        private static SeedRules CertainRules(int maxPerMonth = 1) =>
            new SeedRules { WeeklyWakeChanceAtFullStrength = 1.0, MaxAwakeningsPerMonth = maxPerMonth };

        private static WeekCycle Cycle(SeedRules rules) => new WeekCycle().Register(new SeedSystem(rules));

        [Fact]
        public void Plantar_GuardaOrigenTestigosYQuedaDormida()
        {
            var world = WorldFactory.CreateNewLife(1);
            Character witness = AddPerson(world);

            Seed seed = world.Seeds.Plant(Definition(40), world.PlayerId, new GameDate(17 * 52), ownerAge: 17, witnesses: new[] { witness.Id });

            Assert.Equal(SeedState.Dormant, seed.State);
            Assert.Equal(17, seed.OwnerAgeAtPlanting);
            Assert.Equal(40, seed.Strength);
            Assert.True(seed.HasWitness(witness.Id));
        }

        [Fact]
        public void Fuerza_SeLimitaEntreCeroYCien()
        {
            var world = WorldFactory.CreateNewLife(1);
            Seed seed = world.Seeds.Plant(Definition(90), world.PlayerId, world.Date, 0);
            seed.Strengthen(50);
            Assert.Equal(100, seed.Strength);
            seed.Weaken(500);
            Assert.Equal(0, seed.Strength);
            Assert.Throws<ArgumentOutOfRangeException>(() => seed.Weaken(-1));
        }

        [Fact]
        public void Semilla_QueNuncaCumpleSuCondicion_SeOlvidaAlVencer()
        {
            var world = WorldFactory.CreateNewLife(1);
            var definition = Definition(expiresAfterWeeks: 10);
            definition.Condition = new SwitchCondition { On = false };
            Seed seed = world.Seeds.Plant(definition, world.PlayerId, world.Date, 0);

            Cycle(CertainRules()).AdvanceWeeks(world, 20);

            Assert.Equal(SeedState.Expired, seed.State);
            Assert.Equal(new GameDate(10), seed.ClosedDate);
        }

        [Fact]
        public void Semilla_SinFuerza_SeOlvida()
        {
            var world = WorldFactory.CreateNewLife(1);
            Seed seed = world.Seeds.Plant(Definition(0), world.PlayerId, world.Date, 0);

            Cycle(CertainRules()).AdvanceWeek(world);

            Assert.Equal(SeedState.Expired, seed.State);
        }

        [Fact]
        public void Semilla_RespetaElTiempoMinimoDormida()
        {
            var world = WorldFactory.CreateNewLife(1);
            var definition = Definition();
            definition.MinWeeksDormant = 8;
            Seed seed = world.Seeds.Plant(definition, world.PlayerId, world.Date, 0);

            Cycle(CertainRules()).AdvanceWeeks(world, 20);

            Assert.Equal(new GameDate(8), seed.AwakenedDate);
        }

        [Fact]
        public void Semilla_DespiertaSoloCuandoSeCumpleSuCondicion()
        {
            var world = WorldFactory.CreateNewLife(1);
            var condition = new SwitchCondition();
            var definition = Definition();
            definition.Condition = condition;
            Seed seed = world.Seeds.Plant(definition, world.PlayerId, world.Date, 0);
            WeekCycle cycle = Cycle(CertainRules());

            cycle.AdvanceWeeks(world, 30);
            Assert.Equal(SeedState.Dormant, seed.State);

            condition.On = true;
            cycle.AdvanceWeek(world);
            Assert.Equal(SeedState.Awake, seed.State);
            Assert.Contains(seed, world.Seeds.Awake);
        }

        [Fact]
        public void LimitePorMes_EvitaQueDespiertenTodasJuntas()
        {
            var world = WorldFactory.CreateNewLife(1);
            for (int i = 0; i < 10; i++)
            {
                world.Seeds.Plant(Definition(), world.PlayerId, world.Date, 0);
            }

            Cycle(CertainRules(maxPerMonth: 1)).AdvanceWeeks(world, 8);

            // 8 semanas = 2 meses de 4 semanas: solo 2 despiertan.
            Assert.Equal(2, world.Seeds.Awake.Count());
            Assert.Equal(new[] { 0, 4 }, world.Seeds.Awakenings.Select(d => d.TotalWeeks).ToArray());
        }

        [Fact]
        public void ConPocosCupos_DespiertaPrimeroLaMasFuerte()
        {
            var world = WorldFactory.CreateNewLife(1);
            var rules = CertainRules();
            rules.WeeklyWakeChanceAtFullStrength = 2.0; // así hasta la débil despierta seguro
            Seed weak = world.Seeds.Plant(Definition(60), world.PlayerId, world.Date, 0);
            Seed strong = world.Seeds.Plant(Definition(90), world.PlayerId, world.Date, 0);

            Cycle(rules).AdvanceWeek(world);

            Assert.Equal(SeedState.Awake, strong.State);
            Assert.Equal(SeedState.Dormant, weak.State);
        }

        [Fact]
        public void TestigoQueMuere_DebilitaLaSemilla()
        {
            var world = WorldFactory.CreateNewLife(1);
            Character witness = AddPerson(world);
            var definition = Definition(50);
            definition.Condition = new SwitchCondition { On = false };
            Seed seed = world.Seeds.Plant(definition, world.PlayerId, world.Date, 0, new[] { witness.Id });

            witness.Die(world.Date);
            Cycle(new SeedRules { StrengthLostPerDeadWitness = 15 }).AdvanceWeek(world);

            Assert.False(seed.HasWitness(witness.Id));
            Assert.Equal(35, seed.Strength);
        }

        [Fact]
        public void ContarloAOtro_ExtiendeLaSemilla()
        {
            var world = WorldFactory.CreateNewLife(1);
            Character a = AddPerson(world);
            Character b = AddPerson(world);
            Seed seed = world.Seeds.Plant(Definition(), world.PlayerId, world.Date, 0, new[] { a.Id });

            seed.AddWitness(b.Id);

            Assert.Equal(new[] { a.Id, b.Id }, seed.Witnesses.ToArray());
        }

        [Fact]
        public void Resolver_ConSemillaNueva_LaEnlazaConSuOrigen()
        {
            var world = WorldFactory.CreateNewLife(1);
            Seed seed = world.Seeds.Plant(Definition(), world.PlayerId, world.Date, 0);

            Seed? followUp = world.Seeds.Resolve(seed, new GameDate(5), new SeedDefinition("semilla.lealtad", SeedPolarity.Positive, 50, 520));

            Assert.Equal(SeedState.Resolved, seed.State);
            Assert.NotNull(followUp);
            Assert.Equal(seed.Id, followUp!.ParentSeedId);
            Assert.Equal(SeedState.Dormant, followUp.State);
            Assert.Throws<InvalidOperationException>(() => world.Seeds.Resolve(seed, new GameDate(6)));
        }

        [Fact]
        public void Legado_SoloLasHeredablesPasanAlHijo()
        {
            var world = WorldFactory.CreateNewLife(1);
            Character child = AddPerson(world);
            var heritable = Definition();
            heritable.Heritable = true;
            Seed debt = world.Seeds.Plant(heritable, world.PlayerId, world.Date, 40);
            Seed secret = world.Seeds.Plant(Definition(), world.PlayerId, world.Date, 40);

            int passed = world.Seeds.PassToHeir(world.PlayerId, child.Id);

            Assert.Equal(1, passed);
            Assert.Equal(child.Id, debt.OwnerId);
            Assert.Equal(world.PlayerId, secret.OwnerId);
            Assert.True(secret.IsOpen); // sigue viva en el mundo a través de sus testigos
        }

        [Fact]
        public void MismaPartida_DespiertaLasSemillasEnLaMismaSemana()
        {
            int[] Run(ulong worldSeed)
            {
                var world = WorldFactory.CreateNewLife(worldSeed);
                for (int i = 0; i < 5; i++)
                {
                    world.Seeds.Plant(Definition(30 + (i * 10)), world.PlayerId, world.Date, 0);
                }

                new WeekCycle().Register(new SeedSystem()).AdvanceWeeks(world, 100);
                return world.Seeds.All.Select(s => s.AwakenedDate?.TotalWeeks ?? -1).ToArray();
            }

            Assert.Equal(Run(10), Run(10));
        }

        [Fact]
        public void EjemploDelDiseno_DefenderAMatias_VuelveAniosDespues()
        {
            // 9 años: defiendes a Matías. Se planta una semilla positiva con él como testigo.
            var world = WorldFactory.CreateNewLife(2026);
            Character matias = AddPerson(world);
            var postulasASuEmpresa = new SwitchCondition();
            var defendiste = new SeedDefinition("semilla.defendiste_a_companero", SeedPolarity.Positive, 70, expiresAfterWeeks: 40 * 52)
            {
                Condition = postulasASuEmpresa,
            };
            defendiste.PossibleEvents.Add("evento.te_reconoce_en_entrevista");
            WeekCycle cycle = Cycle(CertainRules());

            cycle.AdvanceWeeks(world, 9 * 52);
            Seed seed = world.Seeds.Plant(defendiste, world.PlayerId, world.Date, world.Player.AgeInYears(world.Date), new[] { matias.Id });

            // 16 años: te alejas de Matías. La semilla baja, sin desaparecer.
            cycle.AdvanceWeeks(world, 7 * 52);
            seed.Weaken(20);
            Assert.Equal(50, seed.Strength);

            // Hasta los 31 no pasa nada: la condición no se cumple.
            cycle.AdvanceWeeks(world, 15 * 52);
            Assert.Equal(SeedState.Dormant, seed.State);

            // 31 años: postulas a su empresa. La semilla despierta.
            postulasASuEmpresa.On = true;
            cycle.AdvanceWeeks(world, 4);
            Assert.Equal(SeedState.Awake, seed.State);
            Assert.Contains("evento.te_reconoce_en_entrevista", seed.PossibleEvents);

            // Matías te contrata: se cierra y nace la lealtad hacia él.
            Seed? lealtad = world.Seeds.Resolve(
                seed,
                world.Date,
                new SeedDefinition("semilla.lealtad_jefe", SeedPolarity.Positive, 60, 20 * 52),
                world.Player.AgeInYears(world.Date),
                new[] { matias.Id });

            Assert.Equal(9, seed.OwnerAgeAtPlanting);
            Assert.Equal(31, world.Player.AgeInYears(world.Date));
            Assert.Equal(seed.Id, lealtad!.ParentSeedId);
            Assert.True(lealtad.HasWitness(matias.Id));
        }
    }
}
