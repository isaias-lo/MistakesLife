using System;
using System.Collections.Generic;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// El horario del jugador: qué hace cada tarde y cada noche de la semana (lunes a
    /// domingo). Se arma una vez por etapa (colegio, estudios, trabajo) y la vida sigue
    /// sola con él hasta que el jugador lo cambia. Un bloque vacío es tiempo libre.
    /// </summary>
    public sealed class Routine
    {
        public const int Days = 7;

        public const int BlocksPerDay = 2;

        private readonly string?[] _slots = new string?[Days * BlocksPerDay];

        public Routine(string stageKey)
        {
            StageKey = stageKey ?? throw new ArgumentNullException(nameof(stageKey));
        }

        /// <summary>Para qué etapa se armó (ver <see cref="RoutineRules.StageKey(Worlds.World, Content.ContentDatabase, Characters.Character)"/>). Al cambiar de etapa se arma otro.</summary>
        public string StageKey { get; }

        /// <summary>Si el jugador ya lo miró y lo dejó como quería (si no, es una sugerencia).</summary>
        public bool Confirmed { get; set; }

        /// <summary>Todos los bloques en orden: lunes tarde, lunes noche, martes tarde...</summary>
        public IReadOnlyList<string?> Slots => _slots;

        /// <summary>Lo que hay en un bloque (null si está libre). <paramref name="day"/> va de 0 (lunes) a 6 (domingo).</summary>
        public string? Get(int day, RoutineBlock block) => _slots[Index(day, block)];

        public void Set(int day, RoutineBlock block, string? activityId) => _slots[Index(day, block)] = activityId;

        private static int Index(int day, RoutineBlock block)
        {
            if (day < 0 || day >= Days)
            {
                throw new ArgumentOutOfRangeException(nameof(day), "El día va de 0 (lunes) a 6 (domingo).");
            }

            return (day * BlocksPerDay) + (int)block;
        }
    }
}
