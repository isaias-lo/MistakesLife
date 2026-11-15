using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Fase de sistemas, después de los sueldos: al empezar cada mes, el negocio del jugador
    /// reparte lo que ganó el mes que terminó (o cierra). Ver <see cref="VentureRules"/>.
    /// </summary>
    public sealed class VentureSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.negocios";

        private readonly ContentDatabase _content;

        public VentureSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            Venture? venture = player.Venture;
            if (!context.Date.IsFirstWeekOfMonth || venture == null || !player.IsAlive || player.Residence == null)
            {
                return;
            }

            Character? partner = VentureRules.Partner(world, player);
            EconomyDefinition? economy = _content.GetCountry(player.Residence.CountryId).Economy;
            CountryEconomy? prices = world.EconomyOf(player.Residence.CountryId);
            VenturesDefinition? rules = economy?.Ventures;
            if (partner == null || rules == null || economy?.Salaries == null || prices == null)
            {
                // Sin socio (murió) o sin reglas, el negocio no sigue.
                VentureRules.Close(world, player, failed: false);
                return;
            }

            var random = context.RandomFor(Id);
            double quality = VentureRules.Quality(player.Personality.Conscientiousness, partner.Personality.Conscientiousness);
            venture.MonthsOpen++;
            if (random.Chance(VentureRules.MonthlyClosure(rules, venture.MonthsOpen - 1, quality)))
            {
                VentureRules.Close(world, player, failed: true);
                return;
            }

            decimal minimum = PayrollCalculator.ToToday(economy.Salaries.Minimum, prices.PriceIndex);
            decimal profit = VentureRules.MonthProfit(rules, venture, minimum, quality, OccupationRules.EconomyFactor(world, _content, player), random);
            decimal share = profit;
            venture.LastShare = share;
            venture.TotalShare += share;
            player.Money += share;
        }
    }
}
