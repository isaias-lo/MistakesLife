using MistakesLife.Core.Game;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Saving
{
    /// <summary>Rearma la máquina del tiempo desde los datos guardados.</summary>
    internal static class TimeMachineReader
    {
        public static void Fill(TimeMachine machine, TimeMachineData? data)
        {
            if (data == null)
            {
                return;
            }

            machine.SetCharges(data.Charges);
            machine.Enabled = data.Enabled;
            foreach (string stage in data.UsedStages)
            {
                machine.MarkStageUsed(stage);
            }

            foreach (SnapshotData s in data.Snapshots)
            {
                SnapshotKind kind = s.Kind switch
                {
                    TimeMachineWriter.StageStartKind => SnapshotKind.StageStart,
                    TimeMachineWriter.BeforeDecisionKind => SnapshotKind.BeforeDecision,
                    _ => throw new SaveFormatException($"La partida guardada tiene una foto del mundo de un tipo desconocido ('{s.Kind}')."),
                };
                machine.Add(new WorldSnapshot(s.Id, kind, new GameDate(s.Week), s.PlayerId, s.PlayerAge, s.StageId, s.EventId, s.Data));
            }

            foreach (IntuitionData i in data.Intuitions)
            {
                machine.RememberChoice(i.EventId, i.CharacterId, new GameDate(i.Week), i.Option);
            }

            machine.NextSnapshotId = System.Math.Max(machine.NextSnapshotId, data.NextSnapshotId);
        }
    }
}
