using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Fase de sistemas: la votación del centro de alumnos (D5a3b). Cuando llega la semana de
    /// votar de quien postuló (<see cref="StudentCouncilRules"/>), sale el resultado: gana o
    /// pierde, con su línea en el diario. Los momentos del año (la campaña, la celebración,
    /// pedirle algo a la dirección) están en los paquetes y miran <c>jugador.resultado_centro</c>.
    /// </summary>
    public sealed class StudentCouncilSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.centro_alumnos";

        private readonly ContentDatabase _content;

        public StudentCouncilSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            SchoolRecord? record = player.SchoolRecord;
            if (!player.IsAlive || record?.CouncilRole == null || record.CouncilResult != null
                || context.Date.TotalWeeks < record.CouncilVoteWeek)
            {
                return;
            }

            if (player.Residence == null || !_content.GetCountry(player.Residence.CountryId).IsSchoolTime(context.Date))
            {
                // En vacaciones no se vota: la votación se hace al volver a clases.
                return;
            }

            bool won = context.RandomFor(SystemId).Chance(StudentCouncilRules.WinChance(world, player, record.CouncilRole));
            record.CouncilResult = won ? "gano" : "perdio";
            if (won)
            {
                player.SetFlag("fue_centro_alumnos");
                player.SetFlag("fue_centro_alumnos_" + record.CouncilRole);
            }

            world.WriteJournal(player.Id, won ? "diario.centro.gano." + record.CouncilRole : "diario.centro.perdio");
        }
    }
}
