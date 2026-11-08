namespace MistakesLife.Core.Presentation
{
    /// <summary>Un cambio listo para mostrar, por ejemplo "Salud +3" o "Dinero -$45.000".</summary>
    public sealed class ChangeView
    {
        public ChangeView(string label, bool isGood)
        {
            Label = label;
            IsGood = isGood;
        }

        /// <summary>Texto del cambio en el idioma del jugador.</summary>
        public string Label { get; }

        /// <summary>Si el cambio es bueno para el jugador (sirve para pintarlo verde o rojo).</summary>
        public bool IsGood { get; }
    }
}
