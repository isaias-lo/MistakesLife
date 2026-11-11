using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un correo que puede llegar a la bandeja (archivos de mail/ de un paquete),
    /// por ejemplo "correo.busqueda.rechazo". Sus textos son el id (el cuerpo),
    /// id + ".asunto" y su remitente. Llega cuando se anota en el diario alguna
    /// de las líneas de "al_anotar" (ver <see cref="Devices.MailSystem"/>) o
    /// cuando un evento hace jugador.recibir_correo('id').
    /// </summary>
    public sealed class MailDefinition
    {
        /// <summary>Temas de correo: la pantalla les pone su ícono y los ordena.</summary>
        public static readonly string[] Topics = { "trabajo", "estudio", "banco", "estado", "juzgado", "personal", "publicidad", "estafa" };

        /// <summary>Id estable: <c>correo.&lt;tema o sistema&gt;.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Clave de texto de quien lo manda: <c>remitente.banco</c>.</summary>
        [JsonProperty("remitente", Required = Required.Always)]
        public string Sender { get; set; } = string.Empty;

        /// <summary>Uno de <see cref="Topics"/>.</summary>
        [JsonProperty("tema", Required = Required.Always)]
        public string Topic { get; set; } = string.Empty;

        /// <summary>Líneas del diario que hacen llegar este correo (por ejemplo "diario.busqueda.rechazo").</summary>
        [JsonProperty("al_anotar")]
        public List<string> OnJournal { get; set; } = new List<string>();

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>Clave del asunto.</summary>
        [JsonIgnore]
        public string SubjectKey => Id + ".asunto";
    }
}
