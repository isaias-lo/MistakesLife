using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una forma de hacer la compra de comida, por ejemplo "compra.basica" (la
    /// canasta básica oficial), "compra.justa" (sin ultraprocesados ni comida
    /// fuera) o "compra.con_antojos" (como compra de verdad la gente).
    /// </summary>
    public sealed class GroceryBasketDefinition
    {
        /// <summary>Id que empieza con "compra.".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Lo que cuesta al mes para una persona, en dinero del inicio del mundo.</summary>
        [JsonProperty("costo_persona", Required = Required.Always)]
        public decimal PerPerson { get; set; }
    }
}
