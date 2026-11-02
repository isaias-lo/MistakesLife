using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un nivel de una carrera: dónde se estudia, cuánto dura y quién puede entrar.
    /// Su id es también la clave del texto de la opción ("Contador auditor en un instituto")
    /// y id + ".titulo" la del título que se obtiene ("Contador auditor").
    /// </summary>
    public sealed class CareerLevelDefinition
    {
        public static readonly string[] Institutions = { "cft", "instituto", "universidad" };

        /// <summary>Por ejemplo "carrera.contabilidad.nivel.contador_auditor".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>"cft", "instituto" o "universidad".</summary>
        [JsonProperty("institucion", Required = Required.Always)]
        public string Institution { get; set; } = string.Empty;

        /// <summary>Duración formal del plan, en semestres.</summary>
        [JsonProperty("semestres", Required = Required.Always)]
        public int Semesters { get; set; }

        /// <summary>Quién puede entrar (por ejemplo "jugador.tiene('paes_alta')"). Opcional.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        [JsonIgnore]
        public string CareerId { get; internal set; } = string.Empty;

        [JsonIgnore]
        public string TitleTextKey => Id + ".titulo";
    }
}
