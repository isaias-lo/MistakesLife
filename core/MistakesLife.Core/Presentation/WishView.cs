namespace MistakesLife.Core.Presentation
{
    /// <summary>Algo que el personaje quiere, listo para la nota "lo que quiero".</summary>
    public sealed class WishView
    {
        public WishView(int index, string text, bool highlighted, bool open, string? state)
        {
            Index = index;
            Text = text;
            Highlighted = highlighted;
            Open = open;
            State = state;
        }

        /// <summary>La posición para marcarlo con <see cref="LifePresenter.HighlightWish"/>.</summary>
        public int Index { get; }

        /// <summary>La frase, con el nombre de la persona si es con alguien ("Que Tomás me hable de nuevo").</summary>
        public string Text { get; }

        /// <summary>Si lo persigue: va con destacador.</summary>
        public bool Highlighted { get; }

        /// <summary>Si todavía se puede cumplir (la etapa sigue).</summary>
        public bool Open { get; }

        /// <summary>Cómo terminó, en palabras ("Se cumplió"); null si sigue abierto.</summary>
        public string? State { get; }
    }
}
