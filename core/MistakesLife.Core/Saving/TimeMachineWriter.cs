using System.Linq;
using MistakesLife.Core.Game;

namespace MistakesLife.Core.Saving
{
    /// <summary>Copia la máquina del tiempo en los datos que se guardan.</summary>
    internal static class TimeMachineWriter
    {
        public const string StageStartKind = "inicio_etapa";
        public const string BeforeDecisionKind = "antes_de_decidir";

        public static TimeMachineData Write(TimeMachine machine) => new TimeMachineData
        {
            Charges = machine.Charges,
            Enabled = machine.Enabled,
            NextSnapshotId = machine.NextSnapshotId,
            UsedStages = machine.UsedStages.OrderBy(s => s, System.StringComparer.Ordinal).ToList(),
            Snapshots = machine.Snapshots.Select(s => new SnapshotData
            {
                Id = s.Id,
                Kind = s.Kind == SnapshotKind.StageStart ? StageStartKind : BeforeDecisionKind,
                Week = s.Date.TotalWeeks,
                PlayerId = s.PlayerId,
                PlayerAge = s.PlayerAgeInYears,
                StageId = s.StageId,
                EventId = s.EventId,
                Data = s.Data,
            }).ToList(),
            Intuitions = machine.Intuitions.Select(i => new IntuitionData
            {
                EventId = i.EventId,
                CharacterId = i.CharacterId,
                Week = i.Date.TotalWeeks,
                Option = i.Option,
            }).ToList(),
        };
    }
}
