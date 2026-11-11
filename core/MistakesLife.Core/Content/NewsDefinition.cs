using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una noticia del mundo tal como se escribe en un paquete (carpeta news/): un
    /// temblor, un temporal, una crisis, unas elecciones o algo que se vuelve viral.
    /// Le pasa al país (o a una ciudad), no al jugador: no cambia su vida por sí sola.
    /// Lo que le pase al jugador lo deciden los eventos que la leen con
    /// <c>noticia_reciente('noticia.x')</c>, según lo que eligió antes.
    /// </summary>
    public sealed class NewsDefinition
    {
        /// <summary>Temas que existen, para que la pantalla sepa cómo mostrar la noticia.</summary>
        public static readonly string[] Topics = { "sismo", "clima", "economia", "politica", "sociedad", "viral", "salud" };

        /// <summary>Id estable, por ejemplo "noticia.chile.terremoto".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Clave del titular. Por defecto, el mismo id.</summary>
        [JsonProperty("texto")]
        public string? TextKey { get; set; }

        /// <summary>Otros titulares para cuando la noticia vuelve a pasar en el mismo país.</summary>
        [JsonProperty("variantes")]
        public List<string> Variants { get; set; } = new List<string>();

        /// <summary>De qué trata: sismo, clima, economia, politica, sociedad, viral o salud.</summary>
        [JsonProperty("tema", Required = Required.Always)]
        public string Topic { get; set; } = string.Empty;

        /// <summary>Probabilidad real de que pase en un año (0 a 1) mientras se cumplen sus condiciones.</summary>
        [JsonProperty("probabilidad_anual", Required = Required.Always)]
        public double AnnualProbability { get; set; }

        /// <summary>
        /// Cuándo puede pasar. Solo puede leer el mundo (<c>mundo.mes</c>, <c>mundo.ciclo</c>,
        /// <c>ley(...)</c>, <c>noticia_reciente(...)</c>): una noticia no depende del jugador.
        /// </summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>Semanas mínimas antes de que vuelva a pasar en el mismo país.</summary>
        [JsonProperty("enfriamiento_semanas")]
        public int CooldownWeeks { get; set; }

        /// <summary>
        /// Si se pone, la noticia solo pasa en esas ciudades (por ejemplo, un temporal
        /// en Santiago). Si no, es del país entero.
        /// </summary>
        [JsonProperty("ciudades")]
        public List<string> Cities { get; set; } = new List<string>();

        /// <summary>
        /// Lo que cambia en el país: <c>cambiar_ciclo</c>, <c>cambiar_inflacion</c> o
        /// <c>cambiar_ley</c>. Nunca el jugador.
        /// </summary>
        [JsonProperty("efectos")]
        public List<string> Effects { get; set; } = new List<string>();

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>El país del paquete donde viene la noticia, o null si es del paquete base (pasa en cualquier país).</summary>
        [JsonIgnore]
        public string? CountryId { get; internal set; }

        [JsonIgnore]
        public string ResolvedTextKey => string.IsNullOrEmpty(TextKey) ? Id : TextKey!;

        /// <summary>El titular y sus variantes, en orden.</summary>
        [JsonIgnore]
        public IEnumerable<string> AllTextKeys
        {
            get
            {
                yield return ResolvedTextKey;
                foreach (string variant in Variants)
                {
                    yield return variant;
                }
            }
        }

        /// <summary>Oportunidad semanal equivalente a la probabilidad anual.</summary>
        [JsonIgnore]
        public double WeeklyProbability => 1.0 - System.Math.Pow(1.0 - AnnualProbability, 1.0 / Time.GameDate.WeeksPerYear);

        /// <summary>El titular que toca según cuántas veces pasó antes en ese lugar (0 = la primera).</summary>
        public string TextKeyFor(int timesBefore)
        {
            if (Variants.Count == 0 || timesBefore <= 0)
            {
                return ResolvedTextKey;
            }

            int index = timesBefore % (Variants.Count + 1);
            return index == 0 ? ResolvedTextKey : Variants[index - 1];
        }

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public List<Effect> CompiledEffects { get; } = new List<Effect>();
    }
}
