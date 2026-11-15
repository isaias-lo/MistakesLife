using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Life;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Pedir un aumento (jugador.pedir_aumento): la respuesta sale de lo que pasa en la vida
    /// real (encuesta "Raise Anatomy" de PayScale, 2018, unas 160 mil personas: de quienes
    /// pidieron, el 39 % recibió lo que pidió, el 31 % recibió menos y el 30 % nada) movida
    /// por su desempeño, por cuánto lo respeta su jefe y por cómo está la economía.
    /// </summary>
    public static class RaiseRules
    {
        /// <summary>Probabilidad de recibir todo lo pedido, para un desempeño y respeto normales.</summary>
        public const double FullChance = 0.39;

        /// <summary>Probabilidad de recibir algo, pero menos (la mitad de lo pedido).</summary>
        public const double PartialChance = 0.31;

        /// <summary>El respeto del jefe que deja las probabilidades como en la encuesta (de 0 a 100).</summary>
        public const int NeutralRespect = 50;

        /// <summary>Cuánto se recibe en un aumento a medias: la mitad de lo pedido.</summary>
        public const double PartialShare = 0.5;

        public const string FullJournal = "diario.pega.aumento_completo";
        public const string PartialJournal = "diario.pega.aumento_parcial";
        public const string NoneJournal = "diario.pega.aumento_no";

        /// <summary>Las líneas del diario que escribe un aumento (para revisar que tengan texto).</summary>
        public static readonly string[] Journals = { FullJournal, PartialJournal, NoneJournal };

        /// <summary>El resultado de pedir: 1 = todo, 0.5 = la mitad, 0 = nada.</summary>
        public static double Ask(World world, ContentDatabase content, Character character, Character? boss, double percent)
        {
            Job job = character.Job ?? throw new InvalidOperationException("No trabaja.");

            int respect = boss == null ? NeutralRespect : RelationshipRules.Feelings(world, boss, character).Respect;
            (double full, double some) = Chances(job.Performance, respect, OccupationRules.EconomyFactor(world, content, character), percent);

            GameRandom random = world.Random.Fork("aumento." + character.Id + "." + world.Date.TotalWeeks);
            double roll = random.NextDouble();
            double share = roll < full ? 1 : roll < some ? PartialShare : 0;
            if (share > 0)
            {
                job.BaseSalary = Math.Round(job.BaseSalary * (decimal)(1 + percent / 100.0 * share), 2, MidpointRounding.AwayFromZero);
            }

            world.WriteJournal(character.Id, share >= 1 ? FullJournal : share > 0 ? PartialJournal : NoneJournal);
            return share;
        }

        /// <summary>
        /// Probabilidad de recibir todo y de recibir algo (todo o la mitad). Desempeño 50,
        /// respeto 50 y economía normal (1) dejan las de la encuesta; cada punto de desempeño
        /// sobre o bajo 50 mueve un 1 %, el respeto del jefe medio punto, y pedir más de un
        /// 10 % baja un 1 % por cada punto extra.
        /// </summary>
        public static (double Full, double Some) Chances(int performance, int bossRespect, double economyFactor, double percent)
        {
            double push = (performance - Job.StartingPerformance) / 100.0
                + (bossRespect - NeutralRespect) / 200.0
                + (economyFactor - 1) / 2
                - Math.Max(0, percent - 10) / 100.0;
            double full = Clamp(FullChance + push);
            return (full, Clamp(full + PartialChance));
        }

        private static double Clamp(double value) => Math.Max(0.02, Math.Min(0.95, value));
    }
}
