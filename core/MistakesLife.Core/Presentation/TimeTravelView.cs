using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Volver al pasado, listo para mostrar: cuántas cargas quedan y a qué momentos
    /// se puede volver ahora (solo los permitidos: con carga y etapa sin volver a vivir).
    /// </summary>
    public sealed class TimeTravelView
    {
        public TimeTravelView(int charges, string chargesLabel, IReadOnlyList<MomentView> moments)
        {
            Charges = charges;
            ChargesLabel = chargesLabel;
            Moments = moments;
        }

        public int Charges { get; }

        /// <summary>Explica cuántas cargas quedan y la regla de una vez por etapa.</summary>
        public string ChargesLabel { get; }

        /// <summary>Momentos a los que se puede volver, del más antiguo al más nuevo. Vacío si no se puede.</summary>
        public IReadOnlyList<MomentView> Moments { get; }

        /// <summary>Si hay que mostrar el botón "Volver al pasado".</summary>
        public bool CanGoBack => Moments.Count > 0;
    }
}
