using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MistakesLife.Core.Saving
{
    /// <summary>
    /// Pasos para adaptar partidas guardadas con un formato viejo al formato actual.
    /// Regla: cada vez que cambia lo que se guarda, se sube
    /// <see cref="CoreInfo.SaveFormatVersion"/> y se agrega aquí un paso que lleva
    /// un guardado de la versión anterior a la nueva (por ejemplo, poner un valor
    /// por defecto a un dato nuevo). Así las partidas viejas nunca se pierden.
    /// </summary>
    internal static class SaveMigrations
    {
        /// <summary>
        /// Pasos por versión de origen: el paso con clave N convierte un guardado
        /// de la versión N en uno de la versión N + 1.
        /// </summary>
        private static readonly IReadOnlyDictionary<int, Action<JObject>> Steps = new Dictionary<int, Action<JObject>>
        {
            // 1 → 2: aparecen los meses cotizados. Las partidas viejas no los
            // contaron, así que parten en 0. El fondo de pensión ya ahorrado no
            // cambia: solo se pierde el conteo de meses anteriores.
            [1] = save =>
            {
                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    character["contributedMonths"] ??= 0;
                }
            },
        };

        /// <summary>Lleva un guardado de cualquier versión anterior a la actual.</summary>
        public static void MigrateToCurrent(JObject save, int version)
        {
            for (int from = version; from < CoreInfo.SaveFormatVersion; from++)
            {
                if (!Steps.TryGetValue(from, out Action<JObject>? step))
                {
                    throw new SaveFormatException($"No hay cómo convertir un guardado de la versión {from} a la {from + 1}.");
                }

                step(save);
                save["version"] = from + 1;
            }
        }
    }
}
