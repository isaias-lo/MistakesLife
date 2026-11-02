using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de personajes: decide cada semana si alguien muere de causas naturales.
    /// La probabilidad sale de la tabla de mortalidad de su país (edad y sexo) y
    /// sube si su salud es muy mala. Aplica a todos los personajes, no solo al
    /// jugador: los padres también envejecen y pueden morir.
    ///
    /// Cuando muere el jugador o alguien de su familia cercana (madre, padre,
    /// pareja o un hijo), se escribe en el diario ("diario.muerte",
    /// "diario.muerte_madre", "diario.muerte_padre", "diario.muerte_pareja",
    /// "diario.muerte_hija", "diario.muerte_hijo") y el tiempo se detiene (día clave).
    /// Lo que deja se reparte como herencia según la ley de su país; después
    /// quien muere deja de tener pareja, y su pareja también.
    /// </summary>
    public sealed class MortalitySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.mortalidad";

        /// <summary>Con salud igual o mayor que esta, el riesgo es el de la tabla.</summary>
        public const int HealthyThreshold = 40;

        /// <summary>Cuántas veces sube el riesgo con salud 0.</summary>
        public const double ZeroHealthRiskMultiplier = 10;

        /// <summary>Motivo para detener el tiempo cuando muere el jugador.</summary>
        public const string StopReasonPlayerDeath = "dia_clave.muerte";

        /// <summary>Motivo para detener el tiempo cuando muere alguien de su familia cercana.</summary>
        public const string StopReasonParentDeath = "dia_clave.muerte_familiar";

        private readonly ContentDatabase _content;

        public MortalitySystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Characters;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character character in world.Characters.Where(c => c.IsAlive && c.Residence != null).OrderBy(c => c.Id).ToList())
            {
                CountryDefinition country = _content.GetCountry(character.Residence!.CountryId);
                double chance = WeeklyDeathProbability(country, character, context.Date);
                if (chance > 0 && context.RandomFor(SystemId + "." + character.Id).Chance(chance))
                {
                    int? playerPartnerId = world.Player.PartnerId;
                    character.Die(context.Date, Character.CauseNatural);
                    Economy.InheritanceSettlement.Settle(world, _content, character);
                    if (character.PartnerId.HasValue)
                    {
                        FamilyFactory.EndPartnership(world, character);
                    }

                    Announce(world, context, character, playerPartnerId);
                }
            }
        }

        /// <summary>Probabilidad de morir esta semana de causas naturales.</summary>
        public static double WeeklyDeathProbability(CountryDefinition country, Character character, GameDate date)
        {
            double annual = country.AnnualDeathProbability(character.AgeInYears(date), character.Sex);
            annual = Math.Min(1, annual * HealthRiskMultiplier(character.Stats.Health));
            if (annual >= 1)
            {
                return 1;
            }

            // Si en un año la probabilidad es q, en una semana es la que repetida
            // 52 veces da q: 1 - (1 - q) elevado a 1/52.
            return 1 - Math.Pow(1 - annual, 1.0 / GameDate.WeeksPerYear);
        }

        /// <summary>
        /// Cuánto multiplica la salud el riesgo de la tabla: 1 con salud 40 o más,
        /// y sube en línea recta hasta 10 con salud 0.
        /// </summary>
        public static double HealthRiskMultiplier(int health)
        {
            if (health >= HealthyThreshold)
            {
                return 1;
            }

            double missing = (double)(HealthyThreshold - health) / HealthyThreshold;
            return 1 + (missing * (ZeroHealthRiskMultiplier - 1));
        }

        private static void Announce(World world, WeekContext context, Character dead, int? partnerIdBefore)
        {
            Character player = world.Player;
            if (dead.Id == player.Id)
            {
                world.WriteJournal(player.Id, "diario.muerte");
                context.RequestStop(StopReasonPlayerDeath);
                return;
            }

            if (!player.IsAlive)
            {
                return;
            }

            string? journalKey = null;
            if (dead.Id == player.MotherId)
            {
                journalKey = "diario.muerte_madre";
            }
            else if (dead.Id == player.FatherId)
            {
                journalKey = "diario.muerte_padre";
            }
            else if (dead.Id == player.SecondParentId)
            {
                journalKey = dead.Gender == Gender.Female ? "diario.muerte_madre" : "diario.muerte_padre";
            }
            else if (dead.Id == partnerIdBefore)
            {
                journalKey = "diario.muerte_pareja";
            }
            else if (dead.IsChildOf(player.Id))
            {
                journalKey = dead.Gender == Gender.Female ? "diario.muerte_hija" : "diario.muerte_hijo";
            }

            if (journalKey != null)
            {
                world.WriteJournal(player.Id, journalKey);
                context.RequestStop(StopReasonParentDeath);
            }
        }
    }
}
