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

            // 7 → 8: el arriendo sale de un contrato que eligió el jugador (antes era
            // un gasto fijo con las marcas vive_independiente y arrienda_pieza). Quien
            // vivía por su cuenta recibe su contrato (pieza o departamento) sin
            // garantía; el arriendo toma el precio de su comuna el primer mes.
            [7] = save =>
            {
                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    if (!(character["flags"] is JArray flags))
                    {
                        continue;
                    }

                    bool room = flags.Any(f => (string?)f == "arrienda_pieza");
                    foreach (JToken old in flags.Where(f => (string?)f == "arrienda_pieza").ToList())
                    {
                        old.Remove();
                    }

                    if (character["lease"] == null && flags.Any(f => (string?)f == Economy.HousingRules.LivesOnOwnFlag) && character["communeId"] != null)
                    {
                        character["lease"] = new JObject
                        {
                            ["typeId"] = room ? "vivienda.pieza" : "vivienda.departamento",
                            ["communeId"] = character["communeId"],
                            ["baseRent"] = 0,
                            ["adjustedPriceIndex"] = 1,
                            ["monthsSinceAdjustment"] = 0,
                            ["monthsLeft"] = 12,
                            ["deposit"] = 0,
                        };
                    }
                }
            },

            // 8 → 9: aparece la búsqueda de trabajo. Las partidas viejas no tenían
            // ninguna: quien estaba buscando ("buscando_trabajo") elige ahora qué y
            // cómo buscar con los eventos, como alguien que recién empieza.
            [8] = save => { },

            // 9 → 10: aparecen las noticias del mundo (temblores, temporales, crisis,
            // elecciones). Las partidas viejas no tenían ninguna: el diario de
            // noticias empieza vacío y se llena desde la semana siguiente.
            [9] = save =>
            {
                if (save["world"] is JObject world)
                {
                    world["news"] ??= new JArray();
                }
            },

            // 10 → 11: la comida deja de ser un monto fijo y sale de la compra que
            // elige el jugador. Quien ya vivía por su cuenta sigue con la canasta
            // básica (lo mínimo para comer bien) hasta que decida otra.
            [10] = save =>
            {
                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    if (character["groceryBasketId"] == null && character["flags"] is JArray flags
                        && flags.Any(f => (string?)f == Economy.HousingRules.LivesOnOwnFlag))
                    {
                        character["groceryBasketId"] = "compra.basica";
                    }
                }
            },

            // 11 → 12: aparecen el celular y el correo. En las partidas viejas nadie
            // tiene: los eventos se los dan (los papás, comprarlo, crear el correo).
            [11] = save => { },

            // 12 → 13: aparecen los servicios que contrata el jugador, y el plan del
            // celular deja de ser un monto fijo. Las partidas viejas no tenían
            // ninguno: el jugador elige su plan con los eventos, como alguien que recién empieza.
            [12] = save => { },

            // 13 → 14: llegan los juzgados (causas judiciales) y la unión civil de cada
            // persona (casada o con unión civil). Los guardados viejos no tienen causas y
            // nadie estaba unido ante la ley.
            [13] = save =>
            {
                if (save["world"] is JObject world)
                {
                    world["courtCases"] ??= new JArray();
                }
            },

            // 14 → 15: aparece con quién viven los hijos de papás separados (el cuidado
            // personal): las partidas viejas no lo habían decidido. Y el monto de las
            // causas pasa a guardarse en dinero del inicio del mundo (para que una
            // pensión se reajuste con los precios): se divide por el índice de precios
            // del país donde vive el dueño de la causa.
            [14] = save =>
            {
                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    character["livesWithId"] ??= JValue.CreateNull();
                    character["sharedCustody"] ??= false;
                }

                foreach (JObject courtCase in save.SelectTokens("world.courtCases[*]").OfType<JObject>())
                {
                    int owner = courtCase.Value<int?>("ownerId") ?? 0;
                    string? country = save.SelectTokens("world.characters[*]").OfType<JObject>()
                        .FirstOrDefault(c => c.Value<int?>("id") == owner)?.Value<string?>("countryId");
                    double index = save.SelectTokens("world.economies[*]").OfType<JObject>()
                        .FirstOrDefault(e => e.Value<string?>("countryId") == country)?.Value<double?>("priceIndex") ?? 1;
                    if (index > 0)
                    {
                        courtCase["amount"] = (courtCase.Value<decimal?>("amount") ?? 0) / (decimal)index;
                    }
                }
            },

            // 15 → 16: aparecen la antigüedad en el trabajo, el último trabajo que dejó y
            // la herencia pendiente de trámite. Las partidas viejas no contaron la
            // antigüedad: se toma la del cargo (lo más parecido que había); no recuerdan su
            // último trabajo ni tienen herencias pendientes.
            [15] = save =>
            {
                foreach (JObject job in save.SelectTokens("world.characters[*].job").OfType<JObject>())
                {
                    job["monthsInJob"] ??= job.Value<int?>("monthsInPosition") ?? 0;
                }

                foreach (JObject character in save.SelectTokens("world.characters[*]").OfType<JObject>())
                {
                    character["lastJob"] ??= JValue.CreateNull();
                    character["pendingInheritance"] ??= 0;
                }
            },

            // 16 → 17: aparece Mensajito (las conversaciones con la gente de la vida del
            // jugador). En las partidas viejas nadie tiene chats: empiezan la próxima
            // semana, sin mensajes por cosas de antes.
            [16] = save => { },
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
