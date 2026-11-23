using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un lugar del mapa de tu barrio (archivos de barrio/ de un paquete, D5c2): la plaza, el
    /// parque, el mall con cine, el gimnasio, el café, un bar. Los de base salen en todos los
    /// países; los del paquete de un país solo en ese país (la fonda del 18 en Chile). Su id es la
    /// clave de su nombre en lang/ y <c>id.descripcion</c> la de una línea que lo cuenta. Lo que se
    /// hace ahí son acciones (acciones/) que dicen <c>"lugar"</c> con este id, más las de
    /// <see cref="ActionIds"/>.
    /// </summary>
    public sealed class MapPlaceDefinition
    {
        /// <summary>Lo que puede abrir un lugar en vez de mostrar acciones: tu casa, tu colegio o tu pega.</summary>
        public static readonly string[] Opens = { OpensHome, OpensStudies, OpensWork };

        public const string OpensHome = "casa";

        public const string OpensStudies = "estudios";

        public const string OpensWork = "trabajo";

        /// <summary>Id estable: <c>lugar.barrio.&lt;nombre&gt;</c> en base, <c>lugar.&lt;país&gt;.&lt;nombre&gt;</c> en un país.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>La forma 3D del lugar en el mapa (Unity la dibuja): "plaza", "mall", "fonda"...</summary>
        [JsonProperty("forma", Required = Required.Always)]
        public string Shape { get; set; } = string.Empty;

        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Cuándo está (lenguaje de condiciones): la fonda solo en septiembre (<c>mundo.mes == 9</c>).</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        /// <summary>Si al tocarlo se abre tu casa, tu colegio o tu pega (ver <see cref="Opens"/>) en vez de sus acciones.</summary>
        [JsonProperty("abre")]
        public string? Open { get; set; }

        /// <summary>Acciones que ya existen y también se hacen aquí (juntarte con tu amigo, en la casa de tus amigos).</summary>
        [JsonProperty("acciones")]
        public List<string> ActionIds { get; set; } = new List<string>();

        /// <summary>
        /// Los lugares de los momentos (el <c>"lugar"</c> de un evento) que pasan aquí: la plaza del
        /// barrio es <c>lugar.plaza</c>; tu colegio, <c>lugar.sala</c> y <c>lugar.patio</c>. Así,
        /// cuando un momento detiene el tiempo, la escena va a este lugar (D5c3). El id del propio
        /// lugar también cuenta (la fonda es <c>lugar.chile.fonda</c> en el mapa y en sus momentos).
        /// </summary>
        [JsonProperty("momentos")]
        public List<string> MomentPlaces { get; set; } = new List<string>();

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>El país del paquete del lugar; null si es de base (sale en todos).</summary>
        [JsonIgnore]
        public string? CountryId { get; set; }

        /// <summary>La clave de la línea que lo cuenta.</summary>
        [JsonIgnore]
        public string DescriptionKey => Id + ".descripcion";
    }
}
