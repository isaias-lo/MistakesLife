using System;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Courts
{
    /// <summary>
    /// Los juzgados (fase de sistemas): cada semana corren los plazos de las causas
    /// abiertas del jugador. Una etapa que no espera a nadie (la notificación, el plazo
    /// para contestar) pasa sola a la siguiente cuando se cumple su plazo; la última
    /// termina la causa. Las etapas que esperan (la audiencia, la sentencia) las
    /// mueven los eventos con lo que decide el jugador. Si la causa es de las que
    /// terminan con la muerte de la otra parte (un divorcio), se cierra sola.
    /// </summary>
    public sealed class CourtSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.juzgados";

        /// <summary>Resultado de una causa que terminó porque se cumplió el plazo de su última etapa.</summary>
        public const string FinishedOutcome = "terminada";

        /// <summary>Resultado de una causa que termina porque murió la otra parte.</summary>
        public const string DeathOutcome = "fallecimiento";

        public CourtSystem(ContentDatabase content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public ContentDatabase Content { get; }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            foreach (CourtCase courtCase in world.CourtCases.Where(c => c.IsOpen && c.OwnerId == world.PlayerId).ToList())
            {
                if (!Content.TryGetCourtCase(courtCase.TypeId, out CourtCaseDefinition? definition))
                {
                    continue;
                }

                if (definition!.EndsIfOtherPartyDies && courtCase.OtherPartyId.HasValue
                    && world.TryGetCharacter(courtCase.OtherPartyId.Value, out Characters.Character? other) && !other.IsAlive)
                {
                    courtCase.Close(DeathOutcome, world.Date);
                    world.WriteJournal(courtCase.OwnerId, definition.OutcomeTextKey(DeathOutcome));
                    continue;
                }

                CourtStageDefinition stage = definition.Stages[courtCase.StageIndex];
                if (stage.Waits || world.Date.WeeksSince(courtCase.StageStarted) < stage.Weeks)
                {
                    continue;
                }

                Advance(world, definition, courtCase);
            }
        }

        /// <summary>Pasa la causa a su etapa siguiente; si estaba en la última, termina.</summary>
        public static void Advance(World world, CourtCaseDefinition definition, CourtCase courtCase)
        {
            if (courtCase.StageIndex + 1 < definition.Stages.Count)
            {
                courtCase.MoveTo(courtCase.StageIndex + 1, world.Date);
            }
            else
            {
                courtCase.Close(FinishedOutcome, world.Date);
            }
        }
    }
}
