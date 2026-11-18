using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Lo que pasó al hacer algo con el tiempo parado, al tiro (D3): qué hiciste, cómo te fue y lo que cambió.</summary>
    public sealed class ActionResultView
    {
        public ActionResultView(string dateLabel, string title, string text, IReadOnlyList<ChangeView> changes, FreeTimeView freeTime)
        {
            DateLabel = dateLabel;
            Title = title;
            Text = text;
            Changes = changes;
            FreeTime = freeTime;
        }

        /// <summary>"14 de marzo".</summary>
        public string DateLabel { get; }

        /// <summary>"Juntarte con la Javiera".</summary>
        public string Title { get; }

        /// <summary>Lo que pasó, en una o dos frases.</summary>
        public string Text { get; }

        /// <summary>Lo que cambió de verdad (en verde lo que subió bien, en rojo lo que costó).</summary>
        public IReadOnlyList<ChangeView> Changes { get; }

        /// <summary>Las tardes libres que quedan después.</summary>
        public FreeTimeView FreeTime { get; }
    }
}
