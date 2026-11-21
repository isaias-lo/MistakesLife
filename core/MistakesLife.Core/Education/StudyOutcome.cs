using System.Collections.Generic;
using MistakesLife.Core.Life;

namespace MistakesLife.Core.Education
{
    /// <summary>Cómo terminó una tarde de estudio: cuántas acertó, cuánto subió el ramo y lo demás que cambió.</summary>
    public sealed class StudyOutcome
    {
        public StudyOutcome(int correct, int total, int masteryBefore, int masteryAfter, IReadOnlyList<ActionChange> changes)
        {
            Correct = correct;
            Total = total;
            MasteryBefore = masteryBefore;
            MasteryAfter = masteryAfter;
            Changes = changes;
        }

        public int Correct { get; }

        public int Total { get; }

        public int MasteryBefore { get; }

        public int MasteryAfter { get; }

        /// <summary>Lo demás que cambió (energía, inteligencia, el cariño de quien estudió contigo, la prueba más preparada).</summary>
        public IReadOnlyList<ActionChange> Changes { get; }
    }
}
