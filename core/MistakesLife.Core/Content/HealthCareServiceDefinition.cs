using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una atención de salud, por ejemplo "atencion.chile.consulta_particular". Lo
    /// que paga cada uno depende de su previsión: en la pública, el bono si lo hay
    /// (o nada si es de la red pública); en un plan privado, el precio menos lo que
    /// cubre el plan. Su id es también la clave de su nombre en lang/.
    /// </summary>
    public sealed class HealthCareServiceDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Lo que cuesta sin ninguna cobertura, en dinero del inicio del mundo.</summary>
        [JsonProperty("precio", Required = Required.Always)]
        public decimal Price { get; set; }

        /// <summary>
        /// Lo que paga quien está en la previsión pública con ingresos (en Chile, el
        /// copago del bono de libre elección de los tramos B, C y D). Si falta, paga el
        /// precio completo. Quien está en el tramo sin ingresos no puede comprar bonos.
        /// </summary>
        [JsonProperty("bono_publico")]
        public decimal? PublicCopay { get; set; }

        /// <summary>Si solo la usa quien está en la previsión pública (el consultorio, el SAPU).</summary>
        [JsonProperty("solo_publico")]
        public bool PublicOnly { get; set; }
    }
}
