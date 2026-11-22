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
            // causas pasa a guardarse en dinero de los paquetes (para que una
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

            // 17 → 18: en Mensajito el jugador puede escribir primero. En las partidas viejas no
            // le había escrito a nadie: ningún chat espera respuesta suya.
            [17] = save => { },

            // 18 → 19: aparece el horario (qué hace el jugador cada tarde y noche). En las
            // partidas viejas nadie tiene uno: la próxima semana se le sugiere uno y se
            // detiene el tiempo para que lo arme.
            [18] = save => { },

            // Versión 19 → 20: un evento pendiente puede pasar con alguien (PersonId).
            // En partidas viejas ningún evento pendiente tiene persona: no hay nada que cambiar.
            [19] = save => { },

            // Versión 20 → 21: reglas propias y lo que quiero. Las partidas viejas no tienen
            // ninguna: las listas quedan vacías y los deseos nacen con la etapa que sigue.
            [20] = save => { },

            // Versión 21 → 22: el curso del colegio. En partidas viejas nadie tiene curso:
            // se arma la próxima semana de clases, con los amigos de su edad adentro.
            [21] = save => { },

            // Versión 22 → 23: las notas del colegio. En partidas viejas nadie tiene notas:
            // empiezan a llegar la próxima semana de clases.
            [22] = save => { },

            // Versión 23 → 24: ya no se escriben los valores que son los de siempre (0, false).
            // Los guardados viejos los traen escritos y se leen igual: no hay nada que cambiar.
            [23] = save => { },

            // Versión 24 → 25: cada semilla recuerda de dónde viene. Las semillas viejas no lo
            // saben: simplemente no muestran "Esto viene de acá".
            [24] = save => { },

            // Versión 25 → 26: notas en la universidad, el CFT y el instituto. Quien ya estudiaba
            // tiene sus semestres aprobados "con notas": las notas llegan desde el próximo.
            [25] = save =>
            {
                foreach (JToken studies in save.SelectTokens("$..Studies").ToList())
                {
                    if (studies is JObject s && s["CompletedSemesters"] != null)
                    {
                        s["GradedSemesters"] = s["CompletedSemesters"];
                    }
                }
            },

            // 26 → 27: semestres cursados (aprobados o no). En un guardado viejo, lo mejor
            // que se sabe es cuántos aprobó.
            [26] = save =>
            {
                foreach (JToken studies in save.SelectTokens("$..Studies").ToList())
                {
                    if (studies is JObject s && s["CompletedSemesters"] != null)
                    {
                        s["SemestersTaken"] = s["CompletedSemesters"];
                    }
                }
            },

            // 27 → 28: cada evento vivido guarda con quién fue (PersonId). Los viejos no lo
            // saben: quedan sin persona.
            [27] = save => { },
            // 28 → 29: el equipo del trabajo (se arma solo la próxima semana).
            [28] = save => { },
            // 29 → 30: la meta, las amonestaciones y lo variable de los oficios (todo parte en cero).
            [29] = save => { },
            // 30 → 31: los días de vacaciones legales de cada trabajo (parten en cero y se ganan mes a mes) y el último día de quien renunció con aviso (nadie).
            [30] = save => { },
            // 31 → 32: con quién comparte el arriendo y a quiénes recomendó en su equipo (nadie todavía).
            [31] = save => { },
            // 32 → 33: el negocio con un socio (nadie tiene uno todavía).
            [32] = save => { },
            // 33 → 34: la empresa que te llamó a la entrevista (quien estaba esperando una sigue sin nombre: "una empresa").
            [33] = save => { },

            // 34 → 35: aparece la previsión de salud (Fonasa o un plan de isapre) y la última
            // atención. En las partidas viejas todos están en la previsión pública, como
            // cualquiera que nunca firmó con una isapre, y nadie se ha atendido: los datos
            // que faltan ya se leen así.
            [34] = save => { },

            // 35 → 36: aparecen la energía y la apariencia. En las partidas viejas todos parten
            // descansados (la energía se ajusta sola en pocas semanas según su vida) y la
            // apariencia heredada se calcula la próxima semana, de sus papás: los datos que
            // faltan ya se leen así.
            [35] = save => { },

            // 36 → 37: aparecen las tardes libres usadas este mes en acciones con el tiempo
            // parado. En las partidas viejas nadie ha usado ninguna: el dato que falta ya se lee así.
            [36] = save => { },

            // 37 → 38: aparece en qué anda la gente que nadie juega (trabaja, busca
            // pega, estudia, jubilada) y la pega real de algunos. En las partidas viejas nadie se
            // ha mirado todavía: el dato que falta se lee como "sin mirar" y se mira la próxima semana.
            [37] = save => { },

            // 38 → 39: Mensajito cuenta cuántas veces llegó cada mensaje con tope por persona
            // (una invitación a un cumpleaños no llega todos los años). En las partidas viejas
            // todavía no llegó ninguno: el dato que falta se lee vacío.
            [38] = save => { },

            // 39 → 40: la pareja tiene etapa (saliendo, pololeo, viven juntos) y se recuerda a la
            // pareja que murió (la viudez). En las partidas viejas las parejas ya vivían juntas: el
            // dato que falta se lee como "viven juntos", y nadie quedó registrado como viudo.
            [39] = save => { },

            // 40 → 41: aparece la gente interesante de tu horario (D4b2). En las partidas viejas
            // todavía no apareció nadie: el dato que falta se lee vacío y aparecen de a poco.
            [40] = save => { },

            // 41 → 42: aparece el dominio de cada ramo con preguntas (D5a) y la recuperativa. En
            // las partidas viejas nadie ha estudiado con preguntas: el dominio que falta parte la
            // próxima semana desde la base de las clases, y ninguna prueba es recuperativa.
            [41] = save => { },

            // 42 → 43: aparecen copiar en la prueba anunciada y las faltas a clases (la cimarra,
            // D5a3). En las partidas viejas nadie copió ni faltó: los datos que faltan se leen así.
            [42] = save => { },

            // 43 → 44: aparecen el taller del colegio y la lista del centro de alumnos (D5a3b).
            // En las partidas viejas nadie está en un taller ni postuló: se leen vacíos.
            [43] = save => { },

            // 44 → 45: aparecen las entregas de los proyectos de la carrera (talleres, electivos
            // y seminario de tesis, D5a4). En las partidas viejas no hay entregas: se leen vacías.
            [44] = save => { },

            // 45 → 46: aparecen los puntajes de la PAES (NEM, ranking y pruebas, D5a5). En las
            // partidas viejas nadie la ha dado con puntaje: se leen en 0 y vacíos.
            [45] = save => { },
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
