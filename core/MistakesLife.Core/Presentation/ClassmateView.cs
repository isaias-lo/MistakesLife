namespace MistakesLife.Core.Presentation
{
    /// <summary>Un compañero del curso, listo para la lista de tu sala.</summary>
    public sealed class ClassmateView
    {
        public ClassmateView(int id, string name, string? bond, int affection, string? memory = null)
        {
            Memory = memory;
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

        /// <summary>
        /// Lo último que viviste con esa persona, en una línea: la línea del diario que dejó
        /// ese momento ("Compartí mis galletas...") o lo que elegiste. Null si nunca han vivido nada.
        /// </summary>
        public string? Memory { get; }

        /// <summary>Cuánto cariño le tienes, de 0 a 100 (para ordenar; la pantalla no lo muestra).</summary>
        public int Affection { get; }
    }
}
