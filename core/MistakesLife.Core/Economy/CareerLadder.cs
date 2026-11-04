using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Las reglas de la escalera de cargos, iguales para toda carrera: dónde empieza
    /// alguien en un área, cuándo puede ascender y cuánto gana en cada cargo. Las áreas,
    /// los cargos y sus requisitos los define cada paquete de carrera.
    /// </summary>
    public static class CareerLadder
    {
        /// <summary>
        /// El desempeño se acerca un punto al mes a este valor más la mitad de la
        /// responsabilidad: quien es responsable rinde mejor con los años (35 a 85).
        /// </summary>
        public const int BasePerformance = 35;

        /// <summary>
        /// Dónde vuelve a empezar quien entra a un área. Con experiencia (su cargo actual o
        /// el último que tuvo en esa carrera): en la misma área, en el mismo cargo; en otra
        /// área de la carrera, un cargo más abajo (el costo de cambiarse). Sin experiencia,
        /// en el cargo inicial.
        /// </summary>
        public static CareerPositionDefinition StartingPosition(ContentDatabase content, Character character, CareerAreaDefinition area)
        {
            CareerPosition? experience = character.Job?.Position ?? character.LastPosition;
            if (experience == null || !content.TryGetCareerArea(experience.AreaId, out CareerAreaDefinition? previousArea) || previousArea!.CareerId != area.CareerId)
            {
                return area.EntryPosition;
            }

            CareerPositionDefinition previous = content.GetCareerPosition(experience.PositionId);
            if (previousArea.Id == area.Id)
            {
                return previous;
            }

            return AtDepth(area, previous.Depth - 1);
        }

        /// <summary>El cargo a esa altura de la escalera, siguiendo el primer camino.</summary>
        private static CareerPositionDefinition AtDepth(CareerAreaDefinition area, int depth)
        {
            CareerPositionDefinition position = area.EntryPosition;
            while (depth > 0 && position.NextPositions.Count > 0)
            {
                position = position.NextPositions[0];
                depth--;
            }

            return position;
        }

        /// <summary>
        /// ¿Puede trabajar en esa área? Siempre hace falta un título de esa carrera
        /// (cualquier nivel), y además los requisitos propios del área.
        /// </summary>
        public static bool CanEnter(ContentDatabase content, Character character, CareerAreaDefinition area, IScriptScope scope) =>
            character.Degrees.Any(d => content.GetCareerLevel(d).CareerId == area.CareerId)
            && (area.CompiledRequirements == null || area.CompiledRequirements.IsTrue(scope));

        /// <summary>
        /// Los cargos a los que puede ascender ahora: los siguientes de su cargo cuyo
        /// tiempo mínimo, desempeño mínimo y requisitos cumple.
        /// </summary>
        public static IReadOnlyList<CareerPositionDefinition> EligiblePromotions(ContentDatabase content, Job job, IScriptScope scope)
        {
            if (job.Position == null)
            {
                return Array.Empty<CareerPositionDefinition>();
            }

            return content.GetCareerPosition(job.Position.PositionId).NextPositions
                .Where(p => job.MonthsInPosition >= p.MinMonths && job.Performance >= p.MinPerformance)
                .Where(p => p.CompiledRequirements == null || p.CompiledRequirements.IsTrue(scope))
                .ToList();
        }

        /// <summary>Sueldo bruto (en dinero del inicio del mundo) de ese cargo en esa área, a partir del sueldo de su título.</summary>
        public static decimal Salary(double degreeSalary, CareerAreaDefinition area, CareerPositionDefinition position) =>
            Math.Round((decimal)(degreeSalary * area.SalaryRelative * position.SalaryRelative), MidpointRounding.AwayFromZero);

        /// <summary>
        /// Lo que crea un trabajo en un cargo: el desempeño y el reajuste siguen igual al
        /// ascender o bajar dentro de la misma área, y vuelven a empezar en un trabajo nuevo.
        /// </summary>
        public static Job Place(Character character, JobKind kind, decimal baseSalary, double priceIndex, CareerAreaDefinition area, CareerPositionDefinition position)
        {
            var job = new Job(kind, baseSalary, priceIndex, new CareerPosition(area.Id, position.Id));
            Job? previous = character.Job;
            if (previous?.Position != null && previous.Position.AreaId == area.Id)
            {
                job.Performance = previous.Performance;
                if (previous.Position.PositionId == position.Id)
                {
                    job.MonthsInPosition = previous.MonthsInPosition;
                }
            }

            character.Job = job;
            return job;
        }

        /// <summary>Pasa un mes en el cargo: suma experiencia y el desempeño se acerca a lo que rinde su personalidad.</summary>
        public static void AdvanceMonth(Character character)
        {
            Job? job = character.Job;
            if (job?.Position == null)
            {
                return;
            }

            job.MonthsInPosition++;
            int target = BasePerformance + character.Personality.Conscientiousness / 2;
            job.Performance += Math.Sign(target - job.Performance);
        }
    }
}
