using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un tipo de deuda del país, por ejemplo "deuda.chile.cae". Su id es también
    /// la clave de su nombre en lang/.
    /// </summary>
    public sealed class DebtTypeDefinition
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Interés al año (0.3 = 30 %). Si es reajustable, es por sobre la inflación.</summary>
        [JsonProperty("tasa_anual", Required = Required.Always)]
        public double AnnualRate { get; set; }

        /// <summary>En cuántas cuotas mensuales se paga.</summary>
        [JsonProperty("cuotas", Required = Required.Always)]
        public int Installments { get; set; }

        /// <summary>
        /// Si la deuda sube con la inflación (como lo que está en UF en Chile). Si no,
        /// la inflación la achica: con el tiempo vale menos.
        /// </summary>
        [JsonProperty("reajustable")]
        public bool Indexed { get; set; }

        /// <summary>
        /// Si al endeudarse la persona recibe el dinero (un crédito de consumo) o se
        /// paga directo a otro (el crédito universitario paga a la institución).
        /// </summary>
        [JsonProperty("entrega_dinero")]
        public bool GivesMoney { get; set; } = true;

        /// <summary>
        /// Condición del lenguaje para que se cobren las cuotas; mientras no se
        /// cumple, la deuda solo suma intereses (por ejemplo, "!jugador.estudia"
        /// para empezar a pagar al terminar de estudiar). Si falta, se cobra siempre.
        /// </summary>
        [JsonProperty("condicion_pago")]
        public string? PaymentCondition { get; set; }

        [JsonIgnore]
        public Expression? CompiledPaymentCondition { get; set; }
    }
}
