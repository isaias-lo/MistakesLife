using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Tu ficha (D6-1a): se abre al tocar tu nombre en la cabecera. Tu nombre completo, tu edad y
    /// cuándo naciste, y lo que eres hoy en filas (colegio, estudios, trabajo, dónde vives, pareja,
    /// hijos, salud). Solo van las filas que existen en tu vida.
    /// </summary>
    public sealed class ProfileView
    {
        public ProfileView(string name, string ageLine, IReadOnlyList<ProfileRowView> rows)
        {
            Name = name;
            AgeLine = ageLine;
            Rows = rows;
        }

        /// <summary>Tu nombre completo ("Vicente Flores Muñoz").</summary>
        public string Name { get; }

        /// <summary>Tu edad y tu cumpleaños ("27 años · naciste el 3 de febrero").</summary>
        public string AgeLine { get; }

        /// <summary>Las filas de la ficha, en orden.</summary>
        public IReadOnlyList<ProfileRowView> Rows { get; }
    }
}
