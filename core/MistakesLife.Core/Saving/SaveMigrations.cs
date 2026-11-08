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

            // 2 → 3: aparecen las relaciones (cariño, confianza, respeto, rencor y
            // lazos). Las partidas viejas no tenían ninguna guardada: todos parten
            // con lo que corresponde a su lazo familiar, sin amigos ni rivales.
            [2] = save =>
            {
                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    character["relationships"] ??= new JArray();
                }
            },

            // 3 → 4: aparecen las áreas, los cargos y el desempeño. Los trabajos
            // guardados antes no tenían área ni cargo: siguen igual (sin escalera) y
            // con desempeño normal; el próximo trabajo de su carrera ya tendrá cargo.
            [3] = save =>
            {
                foreach (JObject job in save.SelectTokens("world.characters[*].job").OfType<JObject>())
                {
                    job["performance"] ??= Economy.Job.StartingPerformance;
                    job["monthsInPosition"] ??= 0;
                }
            },

            // 4 → 5: aparece volver al pasado. Las partidas viejas no tienen fotos
            // del mundo: reciben las cargas de una partida nueva y la primera foto
            // se saca apenas se sigue jugando (el inicio de la etapa en que van).
            [4] = save =>
            {
                save["timeMachine"] ??= new JObject { ["charges"] = Game.TimeMachine.StartingCharges };
            },

            // 5 → 6: aparecen las empresas. Los trabajos guardados antes no tenían
            // empresa: siguen igual (sin tope de cargo ni sueldo por tamaño) y el
            // próximo trabajo de su carrera ya llega a una empresa. Las fotos del
            // mundo guardadas adentro se convierten solas al volver a ellas.
            [5] = save =>
            {
                if (save["world"] is JObject world)
                {
                    world["companies"] ??= new JArray();
                    world["nextCompanyId"] ??= 1;
                }
            },

            // 6 → 7: aparece la jubilación de reparto (Argentina), con el sueldo
            // promedio de los últimos años. Hasta ahora solo se jugaba en Chile, que
            // no la usa: todos parten en 0.
            [6] = save =>
            {
                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    character["pensionSalaryBase"] ??= 0;
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
