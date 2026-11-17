namespace MistakesLife.Core.Presentation
{
    /// <summary>Las tardes libres del mes, para el contador de la pantalla principal y de cada grupo (D3).</summary>
    public sealed class FreeTimeView
    {
        public FreeTimeView(int left, int total, string label, string note)
        {
            Left = left;
            Total = total;
            Label = label;
            Note = note;
        }

        /// <summary>Las que le quedan este mes.</summary>
        public int Left { get; }

        /// <summary>Las que deja su horario en el mes (los puntitos: <see cref="Left"/> amarillos y el resto gastados).</summary>
        public int Total { get; }

        /// <summary>"5 tardes libres en marzo".</summary>
        public string Label { get; }

        /// <summary>"tu horario deja 6 este mes", o cómo conseguir más si no quedan.</summary>
        public string Note { get; }
    }
}
