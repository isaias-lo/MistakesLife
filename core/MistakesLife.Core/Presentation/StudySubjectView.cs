namespace MistakesLife.Core.Presentation
{
    /// <summary>Un ramo en la lista de Estudios (D5a, boceto D5aRamosA): qué se pasa ahora, cuánto lo dominas y la prueba que viene.</summary>
    public sealed class StudySubjectView
    {
        public StudySubjectView(string subjectId, string name, string topic, int mastery, string masteryLabel, LifeBarLevel level, string? average, string? upcoming, bool makeup, bool canStudy)
        {
            SubjectId = subjectId;
            Name = name;
            Topic = topic;
            Mastery = mastery;
            MasteryLabel = masteryLabel;
            Level = level;
            Average = average;
            Upcoming = upcoming;
            Makeup = makeup;
            CanStudy = canStudy;
        }

        public string SubjectId { get; }

        /// <summary>"Matemática".</summary>
        public string Name { get; }

        /// <summary>"Ahora: Fracciones y números mixtos".</summary>
        public string Topic { get; }

        /// <summary>Cuánto lo domina, de 0 a 100.</summary>
        public int Mastery { get; }

        /// <summary>"46% · A medias".</summary>
        public string MasteryLabel { get; }

        /// <summary>Verde, amarillo o rojo.</summary>
        public LifeBarLevel Level { get; }

        /// <summary>"Promedio 5,4"; null si todavía no tiene notas este año.</summary>
        public string? Average { get; }

        /// <summary>"Prueba el jueves 14" o "Recuperativa el viernes 22"; null si no hay prueba anunciada en el ramo.</summary>
        public string? Upcoming { get; }

        /// <summary>Si lo que viene es una recuperativa (se pinta rojo).</summary>
        public bool Makeup { get; }

        /// <summary>Si puede estudiarlo ahora (hay clases y le queda una tarde libre).</summary>
        public bool CanStudy { get; }
    }
}
