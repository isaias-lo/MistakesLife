using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Lo que pasó en un toque de "Seguir viviendo" o al elegir una opción:
    /// cuánto tiempo corrió y cómo cambiaron salud, felicidad, inteligencia y dinero.
    /// La pantalla lo muestra como "Lo que pasó" para que el tiempo se sienta pasar.
    /// </summary>
    public sealed class StepSummary
    {
        public StepSummary(int weeks, string elapsedLabel, IReadOnlyList<ChangeView> changes)
        {
            Weeks = weeks;
            ElapsedLabel = elapsedLabel;
            Changes = changes;
        }

        /// <summary>Semanas que pasaron (0 si solo se eligió una opción).</summary>
        public int Weeks { get; }

        /// <summary>Frase lista para mostrar ("Pasaron 2 años y 3 semanas"), vacía si no pasó tiempo.</summary>
        public string ElapsedLabel { get; }

        /// <summary>Los cambios que se notan, en orden: salud, felicidad, inteligencia y dinero.</summary>
        public IReadOnlyList<ChangeView> Changes { get; }

        /// <summary>Verdadero si no hay nada que contar (no pasó tiempo ni cambió nada).</summary>
        public bool IsEmpty => Weeks == 0 && Changes.Count == 0;
    }
}
