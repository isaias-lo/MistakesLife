using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las tardes libres del mes, el presupuesto de lo que el jugador hace con el tiempo parado
    /// (D3; Isaias eligió el contador del mes el 17-11-2026). Salen de su horario real: cada bloque
    /// de la semana (una tarde o una noche) que está vacío o con tiempo muerto (la tele, el
    /// celular: "tiempo_libre" en actividades/) es una tarde libre que puede usar este mes. Si su
    /// semana está llena de deporte, amigos o hijos, le quedan menos; si la pega o la U lo dejan
    /// sin energía, igual tiene las tardes, pero cansado rinde menos.
    /// </summary>
    public static class FreeTimeRules
    {
        /// <summary>Lo más que puede tener en un mes, aunque su semana esté vacía.</summary>
        public const int MaxPerMonth = 10;

        /// <summary>El mes de una fecha, como un solo número (año y mes).</summary>
        public static int MonthKey(GameDate date) => (date.Year * 12) + date.Month;

        /// <summary>Las tardes libres que deja su horario este mes (0 si no tiene horario).</summary>
        public static int Total(ContentDatabase content, Character character)
        {
            Routine? routine = character.Routine;
            if (routine == null)
            {
                return 0;
            }

            int free = 0;
            foreach (string? slot in routine.Slots)
            {
                ActivityDefinition? activity = slot == null ? null : content.Activity(slot);
                if (slot == null || activity == null || activity.IsFreeTime)
                {
                    free++;
                }
            }

            return Math.Min(MaxPerMonth, free);
        }

        /// <summary>Lo que ya usó este mes (nada si lo último que usó fue de otro mes).</summary>
        public static FreeTime? ThisMonth(World world, Character character) =>
            character.FreeTime != null && character.FreeTime.MonthKey == MonthKey(world.Date) ? character.FreeTime : null;

        /// <summary>Las tardes libres que le quedan este mes.</summary>
        public static int Left(World world, ContentDatabase content, Character character) =>
            Math.Max(0, Total(content, character) - (ThisMonth(world, character)?.Used ?? 0));

        /// <summary>Gasta tardes libres de este mes en lo que hizo (<paramref name="doneKey"/>).</summary>
        public static void Spend(World world, Character character, int afternoons, string doneKey)
        {
            FreeTime month = ThisMonth(world, character) ?? new FreeTime(MonthKey(world.Date));
            character.FreeTime = month;
            month.Used += afternoons;
            month.Done.Add(doneKey);
        }
    }
}
