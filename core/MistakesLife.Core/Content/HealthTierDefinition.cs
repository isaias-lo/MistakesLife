using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tramo de la previsión pública según lo que gana la persona, por ejemplo
    /// "prevision.chile.fonasa.tramo_b" (hasta un sueldo mínimo). Su id es también la
    /// clave de su nombre en lang/.
    /// </summary>
    public sealed class HealthTierDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Hasta cuántos sueldos mínimos de ingreso imponible llega el tramo (1,46 =
        /// hasta 1,46 veces el mínimo). El último tramo no lo lleva: no tiene tope.
        /// </summary>
        [JsonProperty("hasta_sueldos_minimos")]
        public double? UpToMinimumWages { get; set; }
    }
}
