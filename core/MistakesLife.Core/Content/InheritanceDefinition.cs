using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.herencia" de country.json: qué pasa con el dinero y las
    /// deudas de alguien cuando muere, según la ley del país.
    /// </summary>
    public sealed class InheritanceDefinition
    {
        /// <summary>
        /// Si los herederos responden por las deudas solo hasta lo que heredan
        /// ("beneficio de inventario"): si las deudas son más que los bienes, no
        /// heredan nada pero tampoco pagan. Si es false, pagan su parte de lo que falta.
        /// </summary>
        [JsonProperty("beneficio_inventario")]
        public bool LimitedToEstate { get; set; } = true;

        /// <summary>
        /// Cuántas partes de un hijo recibe la pareja (en Chile, el cónyuge recibe
        /// el doble que cada hijo: 2).
        /// </summary>
        [JsonProperty("porciones_pareja")]
        public double PartnerShares { get; set; } = 1;
    }
}
