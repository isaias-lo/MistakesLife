using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un correo que puede llegar a la bandeja (archivos de mail/ de un paquete),
    /// por ejemplo "correo.busqueda.rechazo". Sus textos son el id (el cuerpo),
    /// id + ".asunto" y su remitente. Llega cuando se anota en el diario alguna
    /// de las líneas de "al_anotar" (ver <see cref="Devices.MailSystem"/>), cada cierto
    /// tiempo ("cada": la liquidación de cada mes, la cartola de la AFP) o cuando un
    /// evento hace jugador.recibir_correo('id'). Si tiene "condiciones", solo llega a
    /// quien las cumple en ese momento (regla "nada se inventa": un correo periódico
    /// siempre viene de algo que el personaje tiene, como un trabajo o una tarjeta).
    /// </summary>
    public sealed class MailDefinition
    {
        /// <summary>Temas de correo: la pantalla les pone su ícono y los ordena.</summary>
        public static readonly string[] Topics = { "trabajo", "estudio", "banco", "estado", "juzgado", "personal", "publicidad", "estafa" };

        /// <summary>Cada cuánto puede llegar un correo periódico: la primera semana de cada mes, de enero, mayo y septiembre, o de un mes del año.</summary>
        public static readonly string[] Periods = { "mes", "cuatrimestre", "anio" };

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

        /// <summary>Uno de <see cref="Periods"/>, o null si no es periódico. Necesita <see cref="Conditions"/>.</summary>
        [JsonProperty("cada")]
        public string? Every { get; set; }

        /// <summary>Con "cada": "anio", el mes (1 a 12) en que llega, en su primera semana.</summary>
        [JsonProperty("en_mes")]
        public int? InMonth { get; set; }

        /// <summary>Condición sobre quien lo recibe (por ejemplo "jugador.trabaja"), o vacía si llega siempre.</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>Si este correo periódico llega en la semana de <paramref name="date"/>.</summary>
        public bool IsDueOn(Time.GameDate date)
        {
            if (Every == null || !date.IsFirstWeekOfMonth)
            {
                return false;
            }

            switch (Every)
            {
                case "mes":
                    return true;
                case "cuatrimestre":
                    return date.Month % 4 == 1;
                case "anio":
                    return date.Month == (InMonth ?? 1);
                default:
                    return false;
            }
        }

        /// <summary>Clave del asunto.</summary>
        [JsonIgnore]
        public string SubjectKey => Id + ".asunto";
    }
}
