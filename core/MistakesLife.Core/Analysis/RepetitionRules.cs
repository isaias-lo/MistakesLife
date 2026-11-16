using System;
using System.Collections.Generic;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;

namespace MistakesLife.Core.Analysis
{
    /// <summary>
    /// Regla de repeticiones del rediseño (docs/rediseno-vida.md, R7): un contenido sale una vez
    /// por vida, salvo que haya una razón real para repetirlo. Aquí cuenta como razón que cambie
    /// la persona con quien pasa o la forma de contarlo (otra variante del texto). Lo que pide el
    /// jugador con un botón (<c>a_pedido</c>) no cuenta: lo eligió él. Un texto que nombra la
    /// empresa que te llamó (<c>{empresa}</c>) tampoco: cada entrevista es en otra.
    /// </summary>
    public static class RepetitionRules
    {
        /// <summary>
        /// Cuántas veces, por evento, se volvió a leer el mismo texto con la misma persona (o sin
        /// persona) en los eventos vividos por un personaje, en el orden en que pasaron.
        /// </summary>
        public static Dictionary<string, int> RepeatsWithoutReason(IEnumerable<EventRecord> lived, ContentDatabase content)
        {
            var repeats = new Dictionary<string, int>(StringComparer.Ordinal);
            var timesBefore = new Dictionary<string, int>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (EventRecord record in lived)
            {
                if (!content.TryGetEvent(record.EventId, out EventDefinition? definition) || definition!.OnRequest != null)
                {
                    continue;
                }

                if (content.Text(definition.ResolvedTextKey).Contains(Presentation.LifePresenter.CompanyMark))
                {
                    continue;
                }

                string counter = definition.PerPerson ? record.EventId + "|" + record.PersonId : record.EventId;
                timesBefore.TryGetValue(counter, out int before);
                timesBefore[counter] = before + 1;
                string key = record.EventId + "|" + definition.TextKeyFor(before) + "|" + (record.PersonId?.ToString() ?? "-");
                if (!seen.Add(key))
                {
                    repeats[record.EventId] = repeats.TryGetValue(record.EventId, out int n) ? n + 1 : 1;
                }
            }

            return repeats;
        }
    }
}
