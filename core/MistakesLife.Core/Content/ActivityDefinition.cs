using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Algo que el jugador puede poner en su horario (archivos de actividades/ de un
    /// paquete), por ejemplo "actividad.estudiar" o "actividad.plaza_amigos". Su id es
    /// también la clave de su nombre en lang/. Lo que aporta se suma cada semana por
    /// cada bloque del horario en que está (un bloque es una tarde o una noche).
    /// </summary>
    public sealed class ActivityDefinition
    {
        /// <summary>Lo que puede mover una actividad.</summary>
        public static readonly string[] Contributions = { "animo", "mente", "salud", "carino", "energia", "apariencia" };

        /// <summary>Los bloques del día que se pueden llenar.</summary>
        public static readonly string[] Blocks = { "tarde", "noche" };

        /// <summary>Lo máximo que una actividad puede mover por bloque, para arriba o para abajo.</summary>
        public const int MaxContribution = 3;

        /// <summary>Id estable: <c>actividad.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Condición extra (lenguaje de condiciones), por ejemplo tener celular.</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        /// <summary>En qué bloques tiene sentido; vacío (lo normal si no se escribe) = en los dos.</summary>
        [JsonProperty("franjas")]
        public List<string> AllowedBlocks { get; set; } = new List<string>();

        /// <summary>
        /// Con quién se hace (madre, padre, pareja, hijo, amigo, colega): solo se puede
        /// elegir si esa persona existe, y el cariño se mueve con ella.
        /// </summary>
        [JsonProperty("con")]
        public string? With { get; set; }

        /// <summary>Cuánto mueve cada cosa por bloque a la semana (animo, mente, salud, carino), de -3 a 3.</summary>
        [JsonProperty("aporta")]
        public Dictionary<string, int> Contributes { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Si es tiempo muerto (la tele, el celular): un bloque con esto cuenta como tarde libre
        /// para hacer algo con el tiempo parado (<see cref="Life.FreeTimeRules"/>), igual que uno vacío.
        /// </summary>
        [JsonProperty("tiempo_libre")]
        public bool IsFreeTime { get; set; }

        /// <summary>Qué tan probable es que alguien la elija sin el jugador (simulador, horario sugerido).</summary>
        [JsonProperty("tendencia")]
        public string? Tendency { get; set; }

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public Expression? CompiledTendency { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>Lo que aporta en <paramref name="key"/> (0 si no lo dice).</summary>
        public int Contribution(string key) => Contributes.TryGetValue(key, out int value) ? value : 0;

        /// <summary>Si se puede poner en ese bloque del día.</summary>
        public bool FitsBlock(Life.RoutineBlock block) => AllowedBlocks.Count == 0 || AllowedBlocks.Contains(Blocks[(int)block]);
    }
}
