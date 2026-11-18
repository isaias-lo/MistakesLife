namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una persona en la lista de Tu gente (D4a, boceto D4ListaA): su cara (iniciales), su
    /// nombre y edad, qué es tuyo, su vida en una línea y la relación con barra y palabra.
    /// Nada inventado: la vida solo dice lo que el juego sabe de esa persona.
    /// </summary>
    public sealed class PersonRowView
    {
        public PersonRowView(int id, string name, string initials, int ageInYears, bool isAlive, string relation, string life, int closeness, string feeling, LifeBarLevel level)
        {
            Id = id;
            Name = name;
            Initials = initials;
            AgeInYears = ageInYears;
            IsAlive = isAlive;
            Relation = relation;
            Life = life;
            Closeness = closeness;
            Feeling = feeling;
            Level = level;
        }

        /// <summary>Id del personaje, para abrir su ficha (<see cref="LifePresenter.PersonCard"/>).</summary>
        public int Id { get; }

        /// <summary>Nombre y primer apellido ("Carolina Pérez").</summary>
        public string Name { get; }

        /// <summary>Las iniciales, para la cara ("CP").</summary>
        public string Initials { get; }

        /// <summary>Edad en años (a la fecha de su muerte si murió).</summary>
        public int AgeInYears { get; }

        public bool IsAlive { get; }

        /// <summary>Qué es tuyo: "Mamá", "Amistad", "Compañera de curso".</summary>
        public string Relation { get; }

        /// <summary>Su vida en una línea ("Tu curso, 8º B", "Trabaja contigo en Andes Software"); vacío si el juego no sabe nada más.</summary>
        public string Life { get; }

        /// <summary>Lo largo de la barra, de 0 a 100: el cariño que esa persona te tiene (vacía si murió).</summary>
        public int Closeness { get; }

        /// <summary>La relación en palabras ("muy unidos", "te guarda rencor"); vacío si murió.</summary>
        public string Feeling { get; }

        /// <summary>El color de la barra: verde, amarilla o roja (roja también si te guarda rencor).</summary>
        public LifeBarLevel Level { get; }
    }
}
