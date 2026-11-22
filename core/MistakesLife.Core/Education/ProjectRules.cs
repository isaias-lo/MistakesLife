using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Los ramos de la carrera que se evalúan con un proyecto y no con pruebas (D5a4): talleres,
    /// electivos y el seminario de tesis. Cada semestre el proyecto tiene sus <c>entregas</c>
    /// (en la malla), repartidas en el semestre. Cada entrega es un momento con tu equipo de
    /// la sección o tu profe guía (<c>jugador.entrega_pendiente</c>), y lo que decides
    /// (<c>jugador.entregar(esfuerzo)</c>) deja su calidad. La nota del ramo sale de esas entregas.
    /// </summary>
    public static class ProjectRules
    {
        /// <summary>El prefijo de la marca del área del electivo: <c>electivo_carrera.software.area.datos</c>.</summary>
        public const string ElectiveFlagPrefix = "electivo_";

        /// <summary>Con cuántos compañeros de la sección se trabaja en equipo (los que más te quieren).</summary>
        public const int TeamSize = 2;

        /// <summary>La marca que deja elegir el electivo en esa área.</summary>
        public static string ElectiveFlag(string areaId) => ElectiveFlagPrefix + areaId;

        /// <summary>Los ramos con proyecto del semestre que cursa.</summary>
        public static IReadOnlyList<CourseDefinition> ProjectCourses(ContentDatabase content, CareerStudies studies) =>
            UniversityRules.Courses(content, studies, studies.CompletedSemesters + 1).Where(c => c.Deliveries > 0).ToList();

        /// <summary>Cuántas entregas lleva en ese ramo este semestre.</summary>
        public static int Done(CareerStudies studies, string courseId) =>
            studies.ProjectDeliveries.TryGetValue(courseId, out List<int>? done) ? done.Count : 0;

        /// <summary>La semana del semestre en que toca la entrega número <paramref name="number"/> (repartidas parejo).</summary>
        public static int DueWeek(CourseDefinition course, int number) =>
            number * CareerStudies.WeeksPerSemester / (course.Deliveries + 1);

        /// <summary>El ramo cuya entrega ya toca y no se ha hecho (el primero de la malla), o null.</summary>
        public static CourseDefinition? Pending(ContentDatabase content, CareerStudies studies)
        {
            foreach (CourseDefinition course in ProjectCourses(content, studies))
            {
                int done = Done(studies, course.Id);
                if (done < course.Deliveries && studies.WeeksIntoSemester >= DueWeek(course, done + 1))
                {
                    return course;
                }
            }

            return null;
        }

        /// <summary>Tu equipo del proyecto: los compañeros de la sección que más te quieren.</summary>
        public static IReadOnlyList<Character> Team(World world, Character character) =>
            SchoolRules.Classmates(world, character)
                .OrderByDescending(c => c.RelationshipWith(character.Id)?.Affection ?? 0)
                .ThenBy(c => c.Id)
                .Take(TeamSize)
                .ToList();

        /// <summary>
        /// La calidad de una entrega, de 0 a 100: parte en 30, suma por tu inteligencia (hasta
        /// 30) y tu energía (hasta 15), por el esfuerzo que decidiste (de -1, dejarlo para el
        /// último día, a 2, hacerlo todo tú: 12 puntos cada uno) y por lo responsable que es tu
        /// equipo (de -5 a 5).
        /// </summary>
        public static int Quality(World world, Character character, int effort)
        {
            IReadOnlyList<Character> team = Team(world, character);
            double teamBonus = team.Count == 0 ? 0 : (team.Average(c => c.Personality.Conscientiousness) - 50) / 10.0;
            double quality = 30 + (0.30 * character.Stats.Smarts) + (0.15 * character.Stats.Energy) + (12 * effort) + teamBonus;
            return (int)Math.Round(Math.Max(0, Math.Min(100, quality)));
        }

        /// <summary>Hace la entrega que toca (la de <see cref="Pending"/>), con ese esfuerzo (de -1 a 2).</summary>
        public static void Deliver(World world, ContentDatabase content, Character character, int effort)
        {
            CareerStudies studies = character.Studies ?? throw new InvalidOperationException("No está estudiando una carrera.");
            CourseDefinition course = Pending(content, studies) ?? throw new InvalidOperationException("No hay ninguna entrega que toque ahora.");
            if (!studies.ProjectDeliveries.TryGetValue(course.Id, out List<int>? done))
            {
                done = studies.ProjectDeliveries[course.Id] = new List<int>();
            }

            done.Add(Quality(world, character, Math.Max(-1, Math.Min(2, effort))));
        }

        /// <summary>
        /// La nota del ramo con proyecto: el promedio de la calidad de sus entregas llevado a la
        /// escala del país (50 de calidad es la nota con que se aprueba). Una entrega que no
        /// alcanzó a ser momento (porque el tiempo no se detuvo ahí) cuenta como hecha sin
        /// esfuerzo especial.
        /// </summary>
        public static double Mark(World world, Character character, CourseDefinition course, GradeScaleDefinition scale)
        {
            List<int> qualities = character.Studies!.ProjectDeliveries.TryGetValue(course.Id, out List<int>? done) ? new List<int>(done) : new List<int>();
            while (qualities.Count < course.Deliveries)
            {
                qualities.Add(Quality(world, character, 0));
            }

            double quality = qualities.Average() / 100.0;
            double mark = quality <= 0.5
                ? scale.Min + ((scale.Pass - scale.Min) * quality / 0.5)
                : scale.Pass + ((scale.Max - scale.Pass) * (quality - 0.5) / 0.5);
            return GradeRules.Round(mark, scale);
        }

        /// <summary>El área de su electivo en esa carrera (por su marca), o null si no lo ha elegido.</summary>
        public static string? ElectiveArea(ContentDatabase content, Character character, string careerId) =>
            content.GetCareer(careerId).Areas.Select(a => a.Id).FirstOrDefault(a => character.HasFlag(ElectiveFlag(a)));

        /// <summary>Elige el área del electivo: debe ser un área de su carrera, y se elige una sola vez.</summary>
        public static void ChooseElective(ContentDatabase content, Character character, string areaId)
        {
            CareerStudies studies = character.Studies ?? throw new InvalidOperationException("No está estudiando una carrera.");
            CareerDefinition career = content.GetCareer(studies.CareerId);
            if (!career.Areas.Any(a => a.Id == areaId))
            {
                throw new ArgumentException($"El área {areaId} no es de {career.Id}.", nameof(areaId));
            }

            if (ElectiveArea(content, character, career.Id) == null)
            {
                character.SetFlag(ElectiveFlag(areaId));
            }
        }
    }
}
