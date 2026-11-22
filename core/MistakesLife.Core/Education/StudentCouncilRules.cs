using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// El centro de alumnos (D5a3b): se postula a un cargo de la lista
    /// (<c>jugador.postular_centro('presidencia')</c>) y, unas semanas después, vota el colegio.
    /// Se gana según cómo te ven los de tu curso (el cariño que te tienen) y lo suelto que eres.
    /// Si gana, queda la marca <c>fue_centro_alumnos</c>, que cuenta en las entrevistas de trabajo.
    /// </summary>
    public static class StudentCouncilRules
    {
        /// <summary>Los cargos de la lista. La presidencia es la más difícil de ganar.</summary>
        public static readonly IReadOnlyList<string> Roles = new[] { "presidencia", "tesoreria", "secretaria" };

        /// <summary>Semanas entre postular y la votación (la campaña).</summary>
        public const int CampaignWeeks = 3;

        /// <summary>
        /// La probabilidad de ganar: parte en 10 %, sube hasta 55 puntos con el cariño que te
        /// tiene tu curso (en promedio) y hasta 20 con la extraversión. La presidencia la pelean
        /// más listas: cuesta 10 puntos más. Entre 5 % y 85 %.
        /// </summary>
        public static double WinChance(World world, Character character, string role)
        {
            IReadOnlyList<Character> classmates = SchoolRules.Classmates(world, character);
            double affection = classmates.Count == 0
                ? 30
                : classmates.Average(c => c.RelationshipWith(character.Id)?.Affection ?? 0);
            double chance = 0.10 + (0.55 * affection / 100) + (0.20 * character.Personality.Extraversion / 100)
                - (role == "presidencia" ? 0.10 : 0);
            return Math.Max(0.05, Math.Min(0.85, chance));
        }

        /// <summary>Postula al cargo: el colegio vota en <see cref="CampaignWeeks"/> semanas.</summary>
        public static void Run(World world, Character character, string role)
        {
            if (!Roles.Contains(role))
            {
                throw new ArgumentException($"El cargo {role} no existe (presidencia, tesoreria o secretaria).", nameof(role));
            }

            SchoolRecord record = character.SchoolRecord ??= new SchoolRecord();
            record.CouncilRole = role;
            record.CouncilVoteWeek = world.Date.TotalWeeks + CampaignWeeks;
            record.CouncilResult = null;
            character.SetFlag("lista_centro_alumnos");
        }
    }
}
