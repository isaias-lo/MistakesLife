using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una fila del campo "vida_previa" de country.json: marcas que se dan por
    /// hechas en alguien de cierta edad que el jugador no vivió de cerca.
    /// Se usan en el legado: si el jugador sigue con un hijo de 40 años, ese hijo
    /// ya terminó el colegio aunque nadie lo haya jugado, y no le deben aparecer
    /// los eventos del liceo.
    /// </summary>
    public sealed class PriorLifeDefinition
    {
        /// <summary>Desde esta edad (años cumplidos) se dan por hechas las marcas.</summary>
        [JsonProperty("edad", Required = Required.Always)]
        public int Age { get; set; }

        /// <summary>Marcas que recibe, por ejemplo "egresado_media".</summary>
        [JsonProperty("marcas", Required = Required.Always)]
        public List<string> Flags { get; set; } = new List<string>();

        /// <summary>
        /// Tipo de vivienda que ya arrienda a esa edad (de "economia.vivienda"), o
        /// null. Quien sigue con un hijo de 30 años lo recibe con su contrato al
        /// precio de su comuna, sin volver a pagar la garantía.
        /// </summary>
        [JsonProperty("vivienda")]
        public string? Housing { get; set; }

        /// <summary>La compra de comida que ya hace a esa edad (de "economia.supermercado"), o null.</summary>
        [JsonProperty("compra")]
        public string? GroceryBasket { get; set; }
    }
}
