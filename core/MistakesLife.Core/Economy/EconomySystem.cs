using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Fase de sistemas: la economía de cada país. Al empezar cada mes:
    /// 1. los precios suben según la inflación del mes;
    /// 2. el país puede cambiar de régimen de inflación (con las probabilidades
    ///    de su paquete), y entonces el jugador que vive ahí lo nota en su diario;
    /// 3. si no cambia, la inflación anual se mueve un poco dentro de su rango.
    /// Antes, si el país tiene ciclo económico, puede pasar a otra fase (recesión,
    /// crisis…), que trae su propio régimen de inflación y golpea los fondos de pensión.
    /// Todo sale de los datos del país (campo "economia" de country.json).
    /// </summary>
    public sealed class EconomySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.economia";

        private readonly ContentDatabase _content;

        public EconomySystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        /// <summary>
        /// Crea la economía de cada país que tenga datos, si todavía no existe,
        /// en su régimen inicial. Se llama al crear la partida y en cada semana.
        /// </summary>
        public static void Initialize(World world, ContentDatabase content)
        {
            foreach (CountryDefinition country in content.Countries.Where(c => c.Economy != null).OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                if (world.EconomyOf(country.Id) != null)
                {
                    continue;
                }

                InflationDefinition inflation = country.Economy!.Inflation;
                InflationRegimeDefinition regime = inflation.Regime(inflation.InitialRegime)!;
                double rate = DrawRate(regime, world.Random.Fork("economia.inicio." + country.Id));
                world.AddEconomy(new CountryEconomy(country.Id, regime.Id, rate) { PhaseId = country.Economy.Cycle?.InitialPhase });
            }
        }

        public void Tick(World world, WeekContext context)
        {
            Initialize(world, _content);
            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            foreach (CountryDefinition country in _content.Countries.Where(c => c.Economy != null).OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                CountryEconomy economy = world.EconomyOf(country.Id)!;
                GameRandom random = context.RandomFor(SystemId + "." + country.Id);
                InflationDefinition inflation = country.Economy!.Inflation;
                InflationRegimeDefinition regime = inflation.Regime(economy.RegimeId)!;

                economy.AdvanceMonth();

                EconomicCycleDefinition? cycle = country.Economy.Cycle;
                EconomicPhaseDefinition? phase = cycle == null || economy.PhaseId == null ? null : cycle.Phase(economy.PhaseId);
                string? nextPhaseId = phase == null ? null : NextChange(phase.AnnualSwitch, random);
                if (nextPhaseId != null)
                {
                    // La nueva fase puede traer su propio régimen de inflación.
                    EnterPhase(world, country, economy, cycle!.Phase(nextPhaseId)!, random);
                    continue;
                }

                string? nextRegimeId = NextChange(regime.AnnualSwitch, random);
                InflationRegimeDefinition? next = nextRegimeId == null ? null : inflation.Regime(nextRegimeId);
                if (next != null)
                {
                    ChangeRegime(world, economy, next, random);
                }
                else if (regime.MonthlyVariation > 0)
                {
                    double moved = economy.AnnualInflation + ((random.NextDouble() * 2) - 1) * regime.MonthlyVariation;
                    economy.AnnualInflation = Math.Max(regime.AnnualMin, Math.Min(regime.AnnualMax, moved));
                }
            }
        }

        /// <summary>
        /// Pasa el país a otro régimen con una inflación nueva dentro de su rango,
        /// y lo anota en el diario del jugador si vive ahí (texto "diario." + id del régimen).
        /// </summary>
        public static void ChangeRegime(World world, CountryEconomy economy, InflationRegimeDefinition regime, GameRandom random)
        {
            economy.ChangeRegime(regime.Id, DrawRate(regime, random));
            if (world.TryGetCharacter(world.PlayerId, out Character? player) && player.IsAlive && player.Residence?.CountryId == economy.CountryId)
            {
                world.WriteJournal(player.Id, "diario." + regime.Id);
            }
        }

        /// <summary>Una inflación anual al azar dentro del rango del régimen.</summary>
        public static double DrawRate(InflationRegimeDefinition regime, GameRandom random) =>
            regime.AnnualMin + (random.NextDouble() * (regime.AnnualMax - regime.AnnualMin));

        /// <summary>
        /// Empieza una fase del ciclo económico: el jugador que vive ahí lo lee en su
        /// diario ("diario." + id), la inflación pasa al régimen de la fase (si tiene)
        /// y los fondos de pensión de quienes viven en el país cambian según "golpe_fondos".
        /// </summary>
        public static void EnterPhase(World world, CountryDefinition country, CountryEconomy economy, EconomicPhaseDefinition phase, GameRandom random)
        {
            economy.PhaseId = phase.Id;
            if (world.TryGetCharacter(world.PlayerId, out Character? player) && player.IsAlive && player.Residence?.CountryId == economy.CountryId)
            {
                world.WriteJournal(player.Id, "diario." + phase.Id);
            }

            InflationRegimeDefinition? regime = phase.InflationRegime == null ? null : country.Economy?.Inflation.Regime(phase.InflationRegime);
            if (regime != null && regime.Id != economy.RegimeId)
            {
                ChangeRegime(world, economy, regime, random);
            }

            if (phase.PensionFundShock != 0)
            {
                foreach (Character character in world.Characters.Where(c => c.IsAlive && c.PensionFund > 0 && c.Residence?.CountryId == economy.CountryId))
                {
                    character.PensionFund *= 1 + (decimal)phase.PensionFundShock;
                }
            }
        }

        /// <summary>
        /// Tira el dado de cada cambio posible (convertido a probabilidad mensual),
        /// en orden de id. Devuelve el id del primero que sale, o null si no cambia.
        /// </summary>
        private static string? NextChange(System.Collections.Generic.Dictionary<string, double> annualSwitch, GameRandom random)
        {
            foreach (var change in annualSwitch.OrderBy(c => c.Key, StringComparer.Ordinal))
            {
                double monthly = 1 - Math.Pow(1 - change.Value, 1.0 / 12);
                if (random.Chance(monthly))
                {
                    return change.Key;
                }
            }

            return null;
        }
    }
}
