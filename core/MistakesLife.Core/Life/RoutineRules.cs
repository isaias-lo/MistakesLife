using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las reglas del horario: desde qué edad se arma, para qué etapa, qué se puede
    /// poner en cada bloque y hacia dónde lleva tu vida si sigues así.
    /// </summary>
    public static class RoutineRules
    {
        /// <summary>Desde esta edad el jugador arma su horario (antes lo deciden sus papás).</summary>
        public const int MinAge = 6;

        /// <summary>Lo más lejos de 50 que puede llevar el ánimo un horario (de 15 a 85).</summary>
        public const int MaxMoodPull = 35;

        /// <summary>
        /// La etapa para la que vale un horario: la etapa educativa, la carrera que estudia
        /// o "trabajo" y "libre" de adulto. Si cambia, toca armar otro.
        /// </summary>
        public static string StageKey(World world, ContentDatabase content, Character character)
        {
            if (character.Studies != null)
            {
                return "estudios." + character.Studies.LevelId;
            }

            // La etapa del colegio se calcula por edad igual que en EducationSystem, que
            // corre después en la semana: así el horario nuevo calza con la etapa nueva
            // desde la primera semana y no se pide dos veces.
            string? stage = character.Residence == null
                ? character.EducationStageId
                : content.GetCountry(character.Residence.CountryId).StageForAge(character.AgeInYears(world.Date))?.Id;
            if (stage != null)
            {
                return stage;
            }

            return character.Job != null ? "trabajo" : "libre";
        }

        /// <summary>Las actividades que <paramref name="character"/> puede poner hoy en <paramref name="block"/>.</summary>
        public static IReadOnlyList<ActivityDefinition> Options(World world, ContentDatabase content, Character character, RoutineBlock block)
        {
            return content.Activities.Where(a => a.FitsBlock(block) && IsAvailable(world, content, character, a)).ToList();
        }

        /// <summary>Si la actividad sigue teniendo sentido (edad, condiciones y que exista la persona con quien se hace).</summary>
        public static bool IsAvailable(World world, ContentDatabase content, Character character, ActivityDefinition activity)
        {
            int age = character.AgeInYears(world.Date);
            if (age < activity.MinAge || age > activity.MaxAge)
            {
                return false;
            }

            if (activity.With != null && ChatSystem.PeopleAs(world, character, activity.With).Count == 0)
            {
                return false;
            }

            if (activity.CompiledConditions == null)
            {
                return true;
            }

            var scope = new EventScope(world, content, character, null, readOnly: true);
            return activity.CompiledConditions.Evaluate(scope).AsBool(activity.Id);
        }

        /// <summary>
        /// Un horario armado según la personalidad (para el simulador y como sugerencia
        /// para el jugador, que lo puede cambiar entero). Cerca de un cuarto de los
        /// bloques queda libre.
        /// </summary>
        public static Routine Suggest(World world, ContentDatabase content, Character character, GameRandom random)
        {
            var routine = new Routine(StageKey(world, content, character));
            var scope = new EventScope(world, content, character, null, readOnly: true);
            for (int day = 0; day < Routine.Days; day++)
            {
                foreach (RoutineBlock block in new[] { RoutineBlock.Afternoon, RoutineBlock.Night })
                {
                    if (random.Chance(0.25))
                    {
                        continue;
                    }

                    IReadOnlyList<ActivityDefinition> options = Options(world, content, character, block);
                    if (options.Count == 0)
                    {
                        continue;
                    }

                    double[] weights = options
                        .Select(a => a.CompiledTendency == null ? 50 : Math.Max(0, a.CompiledTendency.Evaluate(scope).AsNumber("tendencia de " + a.Id)))
                        .ToArray();
                    routine.Set(day, block, PickWeighted(options, weights, random)?.Id);
                }
            }

            return routine;
        }

        /// <summary>
        /// Cuánto suma el horario en <paramref name="key"/> (animo, mente, salud, carino)
        /// a la semana, contando solo lo que todavía se puede hacer.
        /// </summary>
        public static int Total(World world, ContentDatabase content, Character character, Routine routine, string key)
        {
            int total = 0;
            foreach (string? id in routine.Slots)
            {
                ActivityDefinition? activity = id == null ? null : content.Activity(id);
                if (activity != null && IsAvailable(world, content, character, activity))
                {
                    total += activity.Contribution(key);
                }
            }

            return total;
        }

        /// <summary>El ánimo hacia el que tiende el jugador con este horario (de 15 a 85).</summary>
        public static int MoodTarget(int moodTotal) => 50 + Math.Max(-MaxMoodPull, Math.Min(MaxMoodPull, moodTotal * 2));

        /// <summary>
        /// La mente hacia la que tiende el jugador con este horario (de 15 a 90). Se mueve
        /// un punto al mes: estudiar sirve, pero de a poco, y dejarlo también se nota.
        /// </summary>
        public static int MindTarget(int mindTotal) => Math.Max(15, Math.Min(90, 50 + (mindTotal * 2)));

        /// <summary>
        /// Cuánto cambia la salud en un mes con este total semanal: un punto si el horario
        /// la cuida o la descuida harto (4 o más), nada si no. El envejecimiento hace el resto.
        /// </summary>
        public static int HealthStep(int healthTotal) => Math.Abs(healthTotal) >= 4 ? Math.Sign(healthTotal) : 0;

        private static ActivityDefinition? PickWeighted(IReadOnlyList<ActivityDefinition> options, double[] weights, GameRandom random)
        {
            double total = weights.Sum();
            if (total <= 0)
            {
                return null;
            }

            double roll = random.NextDouble() * total;
            for (int i = 0; i < options.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    return options[i];
                }
            }

            return options[options.Count - 1];
        }
    }
}
