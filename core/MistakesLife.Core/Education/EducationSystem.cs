using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Fase de sistemas: mantiene al día la etapa educativa del jugador según su
    /// edad y las etapas de su país. Al cambiar de etapa escribe en el diario
    /// "diario.&lt;id de la etapa&gt;" (por ejemplo diario.etapa.chile.basica).
    /// Versión 1: solo por edad; la elección de colegio y el rendimiento llegan después.
    /// </summary>
    public sealed class EducationSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.educacion";

        private readonly ContentDatabase _content;

        public EducationSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Residence == null)
            {
                return;
            }

            CountryDefinition country = _content.GetCountry(player.Residence.CountryId);
            string? stageId = country.StageForAge(player.AgeInYears(context.Date))?.Id;
            if (stageId == player.EducationStageId)
            {
                return;
            }

            player.EducationStageId = stageId;
            if (stageId != null)
            {
                world.WriteJournal(player.Id, "diario." + stageId);
            }
        }
    }
}
