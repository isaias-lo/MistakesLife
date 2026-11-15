namespace MistakesLife.Core.Presentation
{
    /// <summary>La foto de un capítulo: el día del momento que le dio el título, dónde y con quién.</summary>
    public sealed class ChapterPhotoView
    {
        public ChapterPhotoView(string when, string? place, string? person, int? personId)
        {
            When = when;
            Place = place;
            Person = person;
            PersonId = personId;
        }

        /// <summary>El día ("14 de mayo").</summary>
        public string When { get; }

        /// <summary>Dónde ("Tu casa"), si se sabe.</summary>
        public string? Place { get; }

        /// <summary>Con quién (su nombre), si había alguien.</summary>
        public string? Person { get; }

        /// <summary>Id de esa persona, para dibujar su retrato; null si no había nadie.</summary>
        public int? PersonId { get; }
    }
}
