namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// "Esto viene de acá": la foto del día en que nació lo que hoy vuelve. Fecha y edad,
    /// lugar y persona, qué pasaba y qué elegiste. Todos los textos ya vienen en el idioma del jugador.
    /// </summary>
    public sealed class SeedOriginView
    {
        public SeedOriginView(string title, string when, string? place, string? person, string moment, string? choice, int? snapshotId, string? goBackLabel)
        {
            Title = title;
            When = when;
            Place = place;
            Person = person;
            Moment = moment;
            Choice = choice;
            SnapshotId = snapshotId;
            GoBackLabel = goBackLabel;
        }

        /// <summary>El título de la tarjeta ("Esto viene de acá").</summary>
        public string Title { get; }

        /// <summary>Cuándo ("12 de marzo · 9 años").</summary>
        public string When { get; }

        /// <summary>Dónde ("La plaza"), si se sabe.</summary>
        public string? Place { get; }

        /// <summary>Con quién (su nombre, o "tu mamá"), si había alguien.</summary>
        public string? Person { get; }

        /// <summary>Qué pasaba ese día: el texto de ese momento.</summary>
        public string Moment { get; }

        /// <summary>Lo que elegiste ese día, ya como frase ("Elegiste: «...»"); null si no hubo que elegir.</summary>
        public string? Choice { get; }

        /// <summary>
        /// La foto de la máquina del tiempo a la que se puede volver para cambiarlo (la de esa
        /// decisión o la del comienzo de esa etapa); null si ya no se puede volver.
        /// </summary>
        public int? SnapshotId { get; }

        /// <summary>El texto del botón para volver ("Volver a tus 9 años"); null si no se puede volver.</summary>
        public string? GoBackLabel { get; }
    }
}
