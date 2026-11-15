namespace MistakesLife.Core.Presentation
{
    /// <summary>Un compañero del curso, listo para la lista de tu sala.</summary>
    public sealed class ClassmateView
    {
        public ClassmateView(int id, string name, string? bond, int affection)
        {
            Id = id;
            Name = name;
            Bond = bond;
            Affection = affection;
        }

        /// <summary>Id del personaje (para abrir su ficha).</summary>
        public int Id { get; }

        /// <summary>Nombre y apellido ("Antonia Rojas").</summary>
        public string Name { get; }

        /// <summary>Qué son, en palabras ("Amistad", "Rival"); null si solo son compañeros.</summary>
        public string? Bond { get; }

        /// <summary>Cuánto cariño le tienes, de 0 a 100.</summary>
        public int Affection { get; }
    }
}
