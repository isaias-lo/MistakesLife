using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Un negocio chico con un socio (rediseño R6d2). Cada mes gana o pierde según lo
    /// responsables que son los dos, la economía y la suerte, y puede cerrar como cierran de
    /// verdad las empresas nuevas (ver <see cref="VenturesDefinition"/>). Las ganancias se
    /// reparten a medias: a cada uno le toca lo de un microemprendedor típico.
    /// </summary>
    public static class VentureRules
    {
        /// <summary>Cuánto cambia un mes de otro en el mismo negocio (desviación del logaritmo).</summary>
        public const double MonthlyNoise = 0.3;

        public const string OpenedJournal = "diario.negocio.abrio";
        public const string ClosedJournal = "diario.negocio.cerro";
        public const string FailedJournal = "diario.negocio.quebro";

        /// <summary>Las líneas del diario que escriben los negocios.</summary>
        public static readonly string[] Journals = { OpenedJournal, ClosedJournal, FailedJournal };

        /// <summary>
        /// Qué tan bien llevan el negocio los dos, de 0,5 a 1,5 (1 = como el promedio): la
        /// responsabilidad del jugador y la de su socio, a partes iguales.
        /// </summary>
        public static double Quality(int playerResponsibility, int partnerResponsibility) =>
            0.5 + ((playerResponsibility + partnerResponsibility) / 200.0);

        /// <summary>Probabilidad de cerrar este mes: la del año repartida en 12, más alta si lo llevan mal.</summary>
        public static double MonthlyClosure(VenturesDefinition rules, int monthsOpen, double quality)
        {
            double yearly = monthsOpen < 12 ? rules.FirstYearClosure : rules.YearlyClosure;
            double monthly = 1 - Math.Pow(1 - yearly, 1.0 / 12);
            return Math.Max(0, Math.Min(1, monthly * (2 - quality)));
        }

        /// <summary>
        /// Lo que le toca a cada socio este mes, en la moneda de hoy: la
        /// típica del país, por cómo lo llevan, por la economía y por la suerte del mes (la
        /// mayoría de los meses queda bajo el promedio y unos pocos muy sobre).
        /// </summary>
        public static decimal MonthProfit(VenturesDefinition rules, Venture venture, decimal minimumToday, double quality, double economy, GameRandom random)
        {
            double typical = venture.Formal ? rules.FormalProfit : rules.InformalProfit;
            double month = LogNormal(random, MonthlyNoise);
            double profit = (double)minimumToday * typical * quality * economy * venture.Luck * month;
            if (venture.Formal)
            {
                profit *= 1 - rules.FormalTax;
            }

            return Math.Round((decimal)profit, MidpointRounding.AwayFromZero);
        }

        /// <summary>Abre un negocio con esa persona: el jugador pone su aporte (en la moneda de hoy).</summary>
        public static Venture Open(World world, VenturesDefinition rules, Character player, Character partner, bool formal, decimal contributionToday, double priceIndex)
        {
            if (player.Venture != null)
            {
                throw new InvalidOperationException("Ya tiene un negocio.");
            }

            player.Money -= contributionToday;
            GameRandom random = world.Random.Fork("negocio." + player.Id + "." + world.Date.TotalWeeks);
            player.Venture = new Venture(partner.Id, formal, Math.Round(contributionToday / (decimal)priceIndex, 2, MidpointRounding.AwayFromZero), LogNormal(random, rules.Spread));
            world.WriteJournal(player.Id, OpenedJournal);
            return player.Venture;
        }

        /// <summary>Cierra el negocio (lo deciden los socios): lo puesto ya se gastó en el negocio.</summary>
        public static void Close(World world, Character player, bool failed)
        {
            if (player.Venture == null)
            {
                return;
            }

            player.Venture = null;
            world.WriteJournal(player.Id, failed ? FailedJournal : ClosedJournal);
        }

        /// <summary>El socio, si sigue vivo; null si no hay negocio o murió.</summary>
        public static Character? Partner(World world, Character player) =>
            player.Venture != null && world.TryGetCharacter(player.Venture.PartnerId, out Character? partner) && partner.IsAlive ? partner : null;

        /// <summary>Un factor al azar con promedio 1: la mayoría de las veces queda bajo 1 y a veces muy sobre.</summary>
        public static double LogNormal(GameRandom random, double spread) =>
            Math.Exp((Normal(random) * spread) - (spread * spread / 2));

        /// <summary>Un número al azar con distribución normal (media 0, desviación 1).</summary>
        private static double Normal(GameRandom random)
        {
            double u1 = Math.Max(1e-12, random.NextDouble());
            double u2 = random.NextDouble();
            return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }
    }
}
