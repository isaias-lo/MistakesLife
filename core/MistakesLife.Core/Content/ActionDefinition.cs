using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Algo que el jugador hace con el tiempo parado y que da resultado al tiro (archivos de
    /// acciones/ de un paquete, D3 en docs/roadmap.md): estudiar para la prueba, juntarse con un
    /// amigo, hacer deporte, descansar. Gasta tardes libres del mes (<see cref="Life.FreeTimeRules"/>).
    /// Su id es la clave de su nombre en lang/; <c>id.por_que</c> es la línea que explica por qué
    /// tiene sentido ahora y <c>id.resultado</c> lo que pasó (los dos pueden usar <c>{persona}</c>).
    /// </summary>
    public sealed class ActionDefinition
    {
        /// <summary>Los grupos de la pantalla principal donde puede ir una acción.</summary>
        public static readonly string[] Groups = { "gente", "estudios", "trabajo", "salud", "dinero" };

        /// <summary>
        /// Lo que puede cambiar una acción: las barras del jugador, el cariño de la persona con
        /// quien la hace (<c>carino</c>) y lo preparada que está la prueba anunciada (<c>preparar_prueba</c>).
        /// </summary>
        public static readonly string[] Changes = { "felicidad", "salud", "inteligencia", "apariencia", "energia", "carino", "preparar_prueba" };

        /// <summary>Lo máximo que una acción mueve una barra o el cariño, para arriba o para abajo.</summary>
        public const int MaxChange = 8;

        /// <summary>Id estable: <c>accion.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>En qué grupo aparece (ver <see cref="Groups"/>).</summary>
        [JsonProperty("grupo", Required = Required.Always)]
        public string Group { get; set; } = string.Empty;

        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Condición extra (lenguaje de condiciones), por ejemplo tener una prueba anunciada.</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        /// <summary>
        /// Con quién se hace (amigo, pareja, companero, hijo...): la acción aparece una vez por
        /// cada persona que ocupa ese lugar en la vida del jugador.
        /// </summary>
        [JsonProperty("con")]
        public string? With { get; set; }

        /// <summary>Cuántas tardes libres gasta.</summary>
        [JsonProperty("tardes")]
        public int Afternoons { get; set; } = 1;

        /// <summary>Cuántas veces se puede hacer al mes (con cada persona, si dice <see cref="With"/>).</summary>
        [JsonProperty("veces_al_mes")]
        public int PerMonth { get; set; } = 1;

        /// <summary>Lo que cambia al hacerla (ver <see cref="Changes"/>): es también lo que se muestra antes de elegirla.</summary>
        [JsonProperty("cambia")]
        public Dictionary<string, int> Effects { get; set; } = new Dictionary<string, int>();

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>La clave de la línea que explica por qué tiene sentido ahora.</summary>
        [JsonIgnore]
        public string WhyKey => Id + ".por_que";

        /// <summary>La clave de lo que pasó al hacerla.</summary>
        [JsonIgnore]
        public string ResultKey => Id + ".resultado";

        /// <summary>Lo que cambia en <paramref name="key"/> (0 si no lo dice).</summary>
        public int Change(string key) => Effects.TryGetValue(key, out int value) ? value : 0;
    }
}
