using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un objeto que aparece en la escena 3D cuando se cumple su condición, para que lo que
    /// se ve cambie con la vida: libros si estudia, el diploma si se tituló, cuentas rojas si
    /// está moroso. Su id (<c>escena.casa.libros</c>) le dice a la pantalla qué dibujar y dónde;
    /// no lleva texto para el jugador.
    /// </summary>
    public sealed class SceneObjectDefinition
    {
        /// <summary>Lugares de la escena donde puede ir un objeto.</summary>
        public static readonly string[] Places = { "casa", "barrio", "trabajo" };

        /// <summary>Id estable: <c>escena.&lt;lugar&gt;.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Dónde se dibuja: "casa", "barrio" o "trabajo".</summary>
        [JsonProperty("lugar", Required = Required.Always)]
        public string Place { get; set; } = string.Empty;

        /// <summary>Cuándo se ve, en el lenguaje de condiciones (sobre el jugador).</summary>
        [JsonProperty("condiciones", Required = Required.Always)]
        public string Conditions { get; set; } = string.Empty;

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }
    }
}
