using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.deudas" de country.json: los tipos de deuda que existen en
    /// el país (crédito universitario, crédito de consumo…) y cuándo alguien pasa
    /// al registro de morosos (en Chile, DICOM).
    /// </summary>
    public sealed class DebtsDefinition
    {
        /// <summary>Meses seguidos sin pagar una cuota para quedar en el registro de morosos.</summary>
        [JsonProperty("meses_para_moroso")]
        public int MonthsToDelinquent { get; set; } = 3;

        /// <summary>
        /// Meses seguidos sin pagar tras los que la deuda prescribe: ya no se puede
        /// cobrar y se borra. Si falta, nunca prescribe.
        /// </summary>
        [JsonProperty("meses_prescripcion")]
        public int? MonthsToPrescription { get; set; }

        [JsonProperty("tipos", Required = Required.Always)]
        public List<DebtTypeDefinition> Types { get; set; } = new List<DebtTypeDefinition>();

        /// <summary>El tipo de deuda con ese id, o null.</summary>
        public DebtTypeDefinition? Type(string id) => Types.Find(t => t.Id == id);
    }
}
