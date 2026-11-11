using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.supermercado" de country.json: las compras de comida que el
    /// jugador puede elegir cuando vive por su cuenta. Cada compra tiene su costo
    /// por persona al mes, sacado de la canasta básica oficial del país; el jugador
    /// elige cuál hace (jugador.elegir_compra('compra.basica')) y lo que paga cada
    /// mes sale de esa decisión, no de un monto fijo.
    /// </summary>
    public sealed class GroceriesDefinition
    {
        /// <summary>
        /// Cuánto come un hijo menor de edad comparado con un adulto (1 = igual).
        /// En Chile la canasta es por persona (1); en Argentina se mide por
        /// "adulto equivalente" y un niño come en promedio menos.
        /// </summary>
        [JsonProperty("factor_hijo")]
        public decimal ChildFactor { get; set; } = 1;

        [JsonProperty("compras", Required = Required.Always)]
        public List<GroceryBasketDefinition> Baskets { get; set; } = new List<GroceryBasketDefinition>();

        /// <summary>La compra con ese id, o null si el país no la tiene.</summary>
        public GroceryBasketDefinition? Basket(string id) => Baskets.FirstOrDefault(b => b.Id == id);
    }
}
