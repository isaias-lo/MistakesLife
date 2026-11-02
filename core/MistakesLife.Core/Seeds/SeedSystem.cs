using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// Fase de semillas del ciclo semanal: olvida las vencidas, debilita las que
    /// perdieron testigos y decide cuáles despiertan, respetando el límite por mes.
    /// </summary>
    public sealed class SeedSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.semillas";

        public SeedSystem()
            : this(new SeedRules())
        {
        }

        public SeedSystem(SeedRules rules)
        {
            Rules = rules;
        }

        public SeedRules Rules { get; }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Seeds;

        public void Tick(World world, WeekContext context)
        {
            SeedBook book = world.Seeds;
            GameRandom random = context.RandomFor(Id);
            var candidates = new List<Seed>();

            foreach (Seed seed in book.Dormant.ToList())
            {
                ForgetDeadWitnesses(world, seed);

                if (context.Date >= seed.ExpiryDate || seed.Strength <= Seed.MinStrength)
                {
                    book.Expire(seed, context.Date);
                    continue;
                }

                if (context.Date.WeeksSince(seed.PlantedDate) < seed.MinWeeksDormant)
                {
                    continue;
                }

                if (seed.Condition != null && !seed.Condition.IsMet(world, seed))
                {
                    continue;
                }

                candidates.Add(seed);
            }

            // Ventana del "mes": esta semana y las anteriores, hasta completar WeeksPerMonth.
            var monthStart = new GameDate(System.Math.Max(0, context.Date.TotalWeeks - (Rules.WeeksPerMonth - 1)));
            int awakenedThisMonth = book.AwakeningsBetween(monthStart, context.Date);
            int slots = Rules.MaxAwakeningsPerMonth - awakenedThisMonth;

            // Las más fuertes tienen prioridad cuando hay pocos cupos.
            foreach (Seed seed in candidates.OrderByDescending(s => s.Strength).ThenBy(s => s.Id))
            {
                if (slots <= 0)
                {
                    break;
                }

                double chance = Rules.WeeklyWakeChanceAtFullStrength * seed.Strength / Seed.MaxStrength;
                if (random.Chance(chance))
                {
                    book.Awaken(seed, context.Date);
                    slots--;
                }
            }
        }

        private void ForgetDeadWitnesses(World world, Seed seed)
        {
            foreach (int witnessId in seed.Witnesses.ToList())
            {
                if (world.TryGetCharacter(witnessId, out var witness) && witness.IsAlive)
                {
                    continue;
                }

                seed.RemoveWitness(witnessId);
                seed.Weaken(Rules.StrengthLostPerDeadWitness);
            }
        }
    }
}
