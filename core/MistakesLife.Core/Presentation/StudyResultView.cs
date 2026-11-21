using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>El resultado de la tarde de estudio (boceto D5aFin).</summary>
    public sealed class StudyResultView
    {
        public StudyResultView(string title, string score, int masteryBefore, int masteryAfter, LifeBarLevel level, string masteryText, string? testTitle, string? testText, IReadOnlyList<ChangeView> changes, string note, FreeTimeView freeTime)
        {
            Title = title;
            Score = score;
            MasteryBefore = masteryBefore;
            MasteryAfter = masteryAfter;
            Level = level;
            MasteryText = masteryText;
            TestTitle = testTitle;
            TestText = testText;
            Changes = changes;
            Note = note;
            FreeTime = freeTime;
        }

        /// <summary>"Estudiaste fracciones con la Javiera".</summary>
        public string Title { get; }

        /// <summary>"2 de 3 correctas".</summary>
        public string Score { get; }

        public int MasteryBefore { get; }

        public int MasteryAfter { get; }

        public LifeBarLevel Level { get; }

        /// <summary>"Subió de 46% a 50%".</summary>
        public string MasteryText { get; }

        /// <summary>"Prueba del jueves 14"; null si no hay prueba anunciada en el ramo.</summary>
        public string? TestTitle { get; }

        /// <summary>"Con lo que sabes hoy, te podría ir cerca de un 5,1."</summary>
        public string? TestText { get; }

        /// <summary>Lo demás que cambió (en verde lo bueno, en rojo lo que costó).</summary>
        public IReadOnlyList<ChangeView> Changes { get; }

        /// <summary>"La nota la saca Valentina: sale de su dominio, su inteligencia y su energía ese día."</summary>
        public string Note { get; }

        public FreeTimeView FreeTime { get; }
    }
}
