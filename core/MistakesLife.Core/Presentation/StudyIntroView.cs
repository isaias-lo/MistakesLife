using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Antes de estudiar un ramo (boceto D5aTexto): el texto breve de la unidad que va y con quién estudiar.</summary>
    public sealed class StudyIntroView
    {
        public StudyIntroView(string subjectId, string name, string caption, string reading, int mastery, LifeBarLevel level, IReadOnlyList<StudyPartnerView> partners, string note)
        {
            SubjectId = subjectId;
            Name = name;
            Caption = caption;
            Reading = reading;
            Mastery = mastery;
            Level = level;
            Partners = partners;
            Note = note;
        }

        public string SubjectId { get; }

        /// <summary>"Matemática".</summary>
        public string Name { get; }

        /// <summary>"5º básico · Fracciones y números mixtos".</summary>
        public string Caption { get; }

        /// <summary>El texto breve de la unidad (con párrafos separados por una línea en blanco).</summary>
        public string Reading { get; }

        public int Mastery { get; }

        public LifeBarLevel Level { get; }

        /// <summary>Con quién puede estudiar: primero sin nadie, después su curso y su profe jefe.</summary>
        public IReadOnlyList<StudyPartnerView> Partners { get; }

        /// <summary>"Son 3 preguntas de 5º básico, del programa del Mineduc. Gasta 1 tarde libre."</summary>
        public string Note { get; }
    }
}
