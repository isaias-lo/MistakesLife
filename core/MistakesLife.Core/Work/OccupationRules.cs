using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Las reglas de los oficios (carreras con bloque "oficio", que se trabajan sin título):
    /// cada mes se cierra la meta. Cuánto se cumple depende del desempeño, de la suerte del
    /// mes y de cómo anda la economía; lo variable (comisión, propinas o trato) sale de ese
    /// cumplimiento y de la temporada. Los meses seguidos bajo la meta los leen los eventos
    /// (llamado de atención, amonestación, despido): el núcleo solo lleva la cuenta.
    /// </summary>
    public static class OccupationRules
    {
        /// <summary>Cumplimiento de quien tiene desempeño 0; cada punto de desempeño suma <see cref="PerformanceWeight"/>.</summary>
        public const int BaseGoal = 66;

        /// <summary>Con desempeño 50 se cumple, en promedio, el 106 % de la meta: la meta se puede, pero no sobra.</summary>
        public const double PerformanceWeight = 0.8;

        /// <summary>La suerte del mes: hasta 12 puntos arriba o abajo (un mes de pocas ventas, un cliente grande).</summary>
        public const int Luck = 12;

        public const int MinPercent = 30;
        public const int MaxPercent = 160;

        /// <summary>Desde aquí, un mes muy bueno sube el desempeño en 1.</summary>
        public const int GreatMonth = 125;

        /// <summary>Bajo esto, un mes muy malo baja el desempeño en 1.</summary>
        public const int BadMonth = 85;

        /// <summary>Meses seguidos cumpliendo que dejan atrás una amonestación.</summary>
        public const int MonthsToClearWarning = 6;

        /// <summary>La meta: el 100 %.</summary>
        public const int Goal = 100;

        /// <summary>
        /// Un desafío (dejarlo a cargo del local un fin de semana largo, de la cuadrilla en un
        /// hormigonado) se logra si ese mes cumple al menos esto de la meta.
        /// </summary>
        public const int ChallengeGoal = 118;

        public const string ChallengeWonJournal = "diario.oficio.desafio_logrado";
        public const string ChallengeLostJournal = "diario.oficio.desafio_fallido";

        /// <summary>El oficio de ese trabajo, o null si no es un oficio.</summary>
        public static CareerDefinition? CareerOf(ContentDatabase content, Job? job)
        {
            if (job?.Position == null || !content.TryGetCareerArea(job.Position.AreaId, out CareerAreaDefinition? area))
            {
                return null;
            }

            CareerDefinition career = content.GetCareer(area!.CareerId);
            return career.IsOccupation ? career : null;
        }

        /// <summary>
        /// Cuánto cumple de la meta un mes (en porcentaje): el desempeño, la suerte y la
        /// economía (en una crisis se vende y se construye menos).
        /// </summary>
        public static int MonthPercent(int performance, GameRandom random, double economy)
        {
            double percent = (BaseGoal + performance * PerformanceWeight + random.NextInt(-Luck, Luck + 1)) * economy;
            return Math.Max(MinPercent, Math.Min(MaxPercent, (int)Math.Round(percent)));
        }

        /// <summary>
        /// Lo variable de un mes con ese cumplimiento, en la moneda de hoy: lo típico del oficio
        /// (frente al sueldo) por el cumplimiento y por la temporada de ese mes.
        /// </summary>
        public static decimal Variable(OccupationDefinition occupation, decimal salary, int percent, int month) =>
            Math.Round(salary * (decimal)(occupation.TypicalVariable * percent / 100.0 * occupation.SeasonOf(month)), MidpointRounding.AwayFromZero);

        /// <summary>
        /// Cierra el mes que terminó: cumplimiento, meses bajo la meta, lo variable y el
        /// desempeño. Las propinas llegan directo a su dinero; la comisión y el trato quedan
        /// para la liquidación de este mes. Un trabajo que recién empieza no cierra mes.
        /// </summary>
        public static void CloseMonth(World world, ContentDatabase content, Character character, GameDate date)
        {
            Job? job = character.Job;
            CareerDefinition? career = CareerOf(content, job);
            if (job == null || career == null || character.Residence == null)
            {
                job?.ClearGoal();
                return;
            }

            if (job.MonthsInJob == 0)
            {
                return;
            }

            OccupationDefinition occupation = career.Occupation!;
            GameRandom random = world.Random.Fork("oficio.meta." + character.Id + "." + date.TotalWeeks);
            int percent = MonthPercent(job.Performance, random, EconomyFactor(world, content, character));
            // El mes que terminó es el anterior al de hoy (la semana es la primera del mes).
            int month = date.Month == 1 ? 12 : date.Month - 1;
            decimal variable = Variable(occupation, job.Salary, percent, month);
            bool tips = occupation.VariableKind == OccupationDefinition.Tips;
            job.CloseGoal(percent, tips ? 0 : variable, tips ? variable : 0);
            if (tips)
            {
                character.Money += variable;
            }

            if (job.Challenge != null)
            {
                // El desafío se juega en el mes que terminó: si lo logra, gana su marca (la que
                // pide el cargo siguiente); si no, lo puede volver a intentar más adelante.
                if (percent >= ChallengeGoal)
                {
                    character.SetFlag(job.Challenge);
                    world.WriteJournal(character.Id, ChallengeWonJournal);
                }
                else
                {
                    world.WriteJournal(character.Id, ChallengeLostJournal);
                }

                job.Challenge = null;
            }

            if (percent >= GreatMonth)
            {
                job.Performance++;
            }
            else if (percent < BadMonth)
            {
                job.Performance--;
            }
        }

        /// <summary>Cómo pesa la economía: la mitad del efecto que tiene en las contrataciones.</summary>
        private static double EconomyFactor(World world, ContentDatabase content, Character character)
        {
            CountryDefinition country = content.GetCountry(character.Residence!.CountryId);
            string? phaseId = world.EconomyOf(country.Id)?.PhaseId;
            double hiring = (phaseId == null ? null : country.Economy?.Cycle?.Phase(phaseId))?.HiringFactor ?? 1;
            return (1 + hiring) / 2;
        }
    }
}
