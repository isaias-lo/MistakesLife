namespace MistakesLife.Core.Presentation
{
    /// <summary>Otra persona de la vida del jugador, lista para mostrar.</summary>
    public sealed class PersonView
    {
        public PersonView(int id, string name, string relation, int ageInYears, bool isAlive)
            : this(id, name, relation, ageInYears, isAlive, string.Empty)
        {
        }

        public PersonView(int id, string name, string relation, int ageInYears, bool isAlive, string feeling)
        {
            Feeling = feeling;
            Id = id;
            Name = name;
            Relation = relation;
            AgeInYears = ageInYears;
            IsAlive = isAlive;
        }

        public int Id { get; }

        /// <summary>Nombre y apellido.</summary>
        public string Name { get; }

        /// <summary>Qué es del jugador (Mamá, Pareja, Hija...), en su idioma.</summary>
        public string Relation { get; }

        /// <summary>Edad en años (a la fecha de su muerte si murió).</summary>
        public int AgeInYears { get; }

        public bool IsAlive { get; }

        /// <summary>
        /// Cómo se nota la relación desde afuera ("Muy unidos", "Hay rencor"...), en el
        /// idioma de la partida. Vacío si no aplica (por ejemplo, si la persona murió).
        /// Es una palabra y no un número: en la vida real nadie ve el cariño en cifras.
        /// </summary>
        public string Feeling { get; }
    }
}
