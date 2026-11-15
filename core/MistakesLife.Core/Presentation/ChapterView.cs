using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Un año de la vida como capítulo del diario: su título propio ("El año en que murió mi
    /// mamá"), la edad, lo que pasó mes a mes y la foto del momento que le dio el título.
    /// Todo sale de lo que de verdad pasó: el diario del jugador.
    /// </summary>
    public sealed class ChapterView
    {
        public ChapterView(int year, string title, string ageLabel, IReadOnlyList<ChapterMonthView> months, ChapterPhotoView? photo, bool isOpen)
        {
            Year = year;
            Title = title;
            AgeLabel = ageLabel;
            Months = months;
            Photo = photo;
            IsOpen = isOpen;
        }

        /// <summary>El año del calendario de la partida (sirve para pedirlo de nuevo, no para mostrarlo).</summary>
        public int Year { get; }

        /// <summary>El título del capítulo ("El año en que me titulé").</summary>
        public string Title { get; }

        /// <summary>La edad que tenías al terminar ese año ("9 años").</summary>
        public string AgeLabel { get; }

        /// <summary>Lo que pasó, mes a mes (solo los meses en que pasó algo).</summary>
        public IReadOnlyList<ChapterMonthView> Months { get; }

        /// <summary>La foto del momento que le dio el título; null si ese momento no tenía lugar ni persona.</summary>
        public ChapterPhotoView? Photo { get; }

        /// <summary>Si es el año que todavía se está viviendo (el capítulo no se ha cerrado).</summary>
        public bool IsOpen { get; }
    }
}
