namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Alguien con quien puedes ir (D5c2, bocetos BLugar y BElegir): su cara, qué es tuyo, cómo
    /// están y un motivo real para elegirlo ("Le encanta el cine", "Hace 3 meses que no salen").
    /// Nada inventado: el motivo sale de su forma de ser y de lo que han hecho juntos.
    /// </summary>
    public sealed class CompanionView
    {
        public CompanionView(int id, string name, string initials, string relation, string feeling, LifeBarLevel level, int closeness, string? reason, string tab)
        {
            Id = id;
            Name = name;
            Initials = initials;
            Relation = relation;
            Feeling = feeling;
            Level = level;
            Closeness = closeness;
            Reason = reason;
            Tab = tab;
        }

        public int Id { get; }

        /// <summary>"Tomás Rojas".</summary>
        public string Name { get; }

        /// <summary>"TR", para la cara.</summary>
        public string Initials { get; }

        /// <summary>"Amistad", "Mamá", "Compañero de curso".</summary>
        public string Relation { get; }

        /// <summary>"muy unidos".</summary>
        public string Feeling { get; }

        /// <summary>El color de cómo están.</summary>
        public LifeBarLevel Level { get; }

        /// <summary>El cariño que te tiene, de 0 a 100 (para ordenar: primero los más cercanos).</summary>
        public int Closeness { get; }

        /// <summary>El motivo real para elegirlo ahora; null si no hay uno.</summary>
        public string? Reason { get; }

        /// <summary>Su pestaña: pareja, familia, amigos, curso, pega o conocidos.</summary>
        public string Tab { get; }
    }
}
