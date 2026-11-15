using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// La universidad, el CFT y el instituto por dentro: la sección (los compañeros con
    /// nombre con quienes cursas y el profe del semestre) y las notas de cada ramo al cerrar
    /// el semestre. La sección se guarda como un curso (<see cref="SchoolClass"/>) cuyo
    /// "etapa" es el nivel de la carrera y cuyo "curso" es el semestre.
    /// </summary>
    public static class UniversityRules
    {
        /// <summary>Compañeros con nombre en la sección.</summary>
        public const int NamedClassmates = 10;

        /// <summary>Compañeros con nombre que siempre quedan, aunque otros se vayan.</summary>
        public const int MinNamedClassmates = 6;

        /// <summary>Probabilidad de que el lugar de quien se fue lo tome alguien que viene atrasado.</summary>
        public const double LateComers = 0.3;

        /// <summary>Alumnos en la sección (la mayoría sin nombre).</summary>
        public const int SectionStudents = 45;

        /// <summary>Probabilidad de que alguien de la sección no siga al semestre siguiente (deserta, se atrasa o se cambia).</summary>
        public const double LeavesPerSemester = 0.12;

        /// <summary>Profes y profesoras de la universidad: de 28 a 68 años.</summary>
        public const int MinProfessorAge = 28;

        public const int MaxProfessorAge = 68;

        /// <summary>Cuánto más cuesta una nota en la universidad que en el colegio (en la escala de 1 a 7).</summary>
        public const double HarderThanSchool = 0.5;

        /// <summary>Preparación máxima de un semestre (ver <see cref="CareerStudies.Preparation"/>).</summary>
        public const int MaxPreparation = 3;

        /// <summary>Si ese curso es una sección de una carrera (y no un curso del colegio).</summary>
        public static bool IsSection(ContentDatabase content, SchoolClass? cls) =>
            cls != null && content.HasCareerLevel(cls.StageId);

        /// <summary>Si hoy cursa en una sección: estudia una carrera y ya tiene sección.</summary>
        public static bool InSection(Character character) =>
            character.Studies != null && character.SchoolClass != null && character.SchoolClass.StageId == character.Studies.LevelId;

        /// <summary>Los ramos de ese semestre del plan que estudia.</summary>
        public static IReadOnlyList<CourseDefinition> Courses(ContentDatabase content, CareerStudies studies, int semester)
        {
            CareerDefinition career = content.GetCareer(studies.CareerId);
            return career.Courses.Where(c => c.In(studies.Institution, semester, studies.RequiredSemesters)).ToList();
        }

        /// <summary>
        /// Arma la sección del primer semestre (o del semestre en que va, si llega a mitad de
        /// carrera): compañeros nuevos con nombre, de su edad o algo mayores, y el profe.
        /// Un amigo que estudia lo mismo, al mismo tiempo, no existe todavía en el mundo: todos son nuevos.
        /// </summary>
        public static SchoolClass Form(World world, ContentDatabase content, Character player, GameRandom random)
        {
            CareerStudies studies = player.Studies!;
            int semester = studies.CompletedSemesters + 1;
            var section = new SchoolClass(studies.LevelId, semester, (1 + random.NextInt(3)).ToString(System.Globalization.CultureInfo.InvariantCulture), semester, SectionStudents);
            while (section.ClassmateIds.Count < NamedClassmates)
            {
                section.ClassmateIds.Add(NewClassmate(world, content, player, section, random).Id);
            }

            section.TeacherId = NewProfessor(world, content, player, random).Id;
            return section;
        }

        /// <summary>
        /// Pasa al semestre siguiente: profe nuevo cada año, alguien de la sección a veces no
        /// sigue y a veces llega alguien que viene atrasado. Devuelve las líneas del diario.
        /// </summary>
        public static IReadOnlyList<string> Advance(World world, ContentDatabase content, Character player, GameRandom random)
        {
            SchoolClass section = player.SchoolClass!;
            var journal = new List<string>();
            section.Level = player.Studies!.CompletedSemesters + 1;
            section.SchoolYear = section.Level;
            // El profe cambia cada año (los ramos anuales y los del mismo departamento suelen ir con la misma persona).
            if (section.Level % 2 == 1 || section.TeacherId == null || !world.TryGetCharacter(section.TeacherId.Value, out Character? professor) || !professor.IsAlive)
            {
                section.TeacherId = NewProfessor(world, content, player, random).Id;
            }

            section.ClassmateIds.RemoveAll(id => !world.TryGetCharacter(id, out Character? c) || !c.IsAlive);
            section.LeftIds.Clear();
            foreach (int id in section.ClassmateIds)
            {
                if (random.Chance(LeavesPerSemester) && (player.RelationshipWith(id)?.Bond ?? RelationshipBond.None) == RelationshipBond.None)
                {
                    section.LeftIds.Add(id);
                }
            }

            section.ClassmateIds.RemoveAll(section.LeftIds.Contains);
            if (section.LeftIds.Count > 0)
            {
                journal.Add("diario.seccion.se_fue");
            }

            // Quien se va no siempre se reemplaza: la sección se achica con los años, como en la vida
            // real. A veces llega alguien que viene atrasado o de otra carrera.
            int missing = NamedClassmates - section.ClassmateIds.Count;
            for (int i = 0; i < missing; i++)
            {
                if (section.ClassmateIds.Count < MinNamedClassmates || random.Chance(LateComers))
                {
                    section.ClassmateIds.Add(NewClassmate(world, content, player, section, random).Id);
                }
            }

            return journal;
        }

        /// <summary>
        /// Las notas finales de los ramos del semestre que se acaba de aprobar, en la escala
        /// del país. Dependen de lo mismo que en el colegio (ver <see cref="GradeRules.Mark"/>),
        /// pero cuestan un poco más, y de cuánto se preparó en el semestre.
        /// </summary>
        public static SortedDictionary<string, double> SemesterMarks(World world, ContentDatabase content, Character player, int semester, GradeScaleDefinition scale, GameRandom random)
        {
            var marks = new SortedDictionary<string, double>(StringComparer.Ordinal);
            int preparation = Math.Max(-MaxPreparation, Math.Min(MaxPreparation, player.Studies!.Preparation));
            foreach (CourseDefinition course in Courses(content, player.Studies, semester))
            {
                // Mark solo sabe de preparación positiva; un semestre de carrete (negativa) baja la nota igual.
                double mark = GradeRules.Mark(world, content, player, scale, Math.Max(0, preparation), random);
                double harder = (HarderThanSchool - (GradeRules.PreparationWeight * Math.Min(0, preparation))) / 6.0 * (scale.Max - scale.Min);
                marks[course.Id] = GradeRules.Round(Math.Max(scale.Min, mark - harder), scale);
            }

            return marks;
        }

        /// <summary>El número romano de un ramo que se repite en la malla (Cálculo I, II, III...).</summary>
        public static string Roman(int number)
        {
            string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
            return number >= 1 && number <= romans.Length ? romans[number - 1] : number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Character NewClassmate(World world, ContentDatabase content, Character player, SchoolClass section, GameRandom random)
        {
            // La mayoría entra a los 18 o 19; algunos vienen de trabajar o de otra carrera.
            int age = 18 + ((section.Level - 1) / 2) + random.NextInt(0, 3) + (random.Chance(0.15) ? random.NextInt(3, 12) : 0);
            age = Math.Max(age, Math.Min(player.AgeInYears(world.Date) - 2, age + 6));
            return SchoolRules.CreateClassmate(world, content, player, section, random, WorldFactory.BirthDateForAge(world.Date, age, random));
        }

        private static Character NewProfessor(World world, ContentDatabase content, Character player, GameRandom random)
        {
            return SchoolRules.CreateTeacher(world, content, player, new SchoolClassDefinition { FemaleTeachers = 0.45 }, random, MinProfessorAge, MaxProfessorAge);
        }
    }
}
