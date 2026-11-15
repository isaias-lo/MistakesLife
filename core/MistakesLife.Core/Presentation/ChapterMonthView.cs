using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Un mes de un capítulo: su nombre ("mayo") y las líneas del diario de ese mes.</summary>
    public sealed class ChapterMonthView
    {
        public ChapterMonthView(string month, IReadOnlyList<string> lines)
        {
            Month = month;
            Lines = lines;
        }

        public string Month { get; }

        public IReadOnlyList<string> Lines { get; }
    }
}
