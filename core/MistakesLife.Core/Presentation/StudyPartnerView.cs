namespace MistakesLife.Core.Presentation
{
    /// <summary>Con quién se puede estudiar una tarde: sin nadie, alguien de tu curso o tu profe jefe.</summary>
    public sealed class StudyPartnerView
    {
        public StudyPartnerView(int? personId, bool teacher, string name, string caption, string initials)
        {
            PersonId = personId;
            Teacher = teacher;
            Name = name;
            Caption = caption;
            Initials = initials;
        }

        /// <summary>La persona de tu curso; null si es estudiar sin nadie o con la profe.</summary>
        public int? PersonId { get; }

        /// <summary>Si es tu profe jefe.</summary>
        public bool Teacher { get; }

        /// <summary>"Sola", "Javiera" o "La profe Carolina".</summary>
        public string Name { get; }

        /// <summary>"solo tú", "compañera · tu mejor amiga", "te explica lo que falles".</summary>
        public string Caption { get; }

        /// <summary>"JS" para el círculo.</summary>
        public string Initials { get; }
    }
}
