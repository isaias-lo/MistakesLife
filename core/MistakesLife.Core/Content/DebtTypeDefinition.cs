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
        /// Deuda rotativa (como una tarjeta de crédito): en vez de cuotas fijas, cada
        /// mes se cobra esta parte de lo que se debe (0.05 = 5 %). Si falta, se paga en cuotas.
        /// </summary>
        [JsonProperty("pago_minimo")]
        public double? MinimumPaymentRate { get; set; }

        /// <summary>Con pago mínimo: lo menos que se cobra al mes, en dinero del inicio del mundo (o todo, si se debe menos).</summary>
        [JsonProperty("pago_minimo_monto")]
        public decimal MinimumPaymentAmount { get; set; }

        /// <summary>
        /// Si se puede repactar cuando hay cuotas atrasadas: en cuántas cuotas nuevas
        /// queda la deuda. Si falta, no se puede repactar.
        /// </summary>
        [JsonProperty("cuotas_repactacion")]
        public int? RescheduleInstallments { get; set; }

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
        /// <summary>
        /// Si es true, cada mes paga con todo el dinero que haya (hasta lo que se
        /// debe), como una línea de crédito o un sobregiro de la cuenta corriente.
        /// Igual debe alcanzar al menos el pago mínimo para no quedar atrasada.
        /// </summary>
        [JsonProperty("se_paga_con_el_saldo")]
        public bool PaysFromBalance { get; set; }

        /// <summary>
        /// Si la deuda se borra cuando muere quien la tiene (por un seguro o por ley,
        /// como el CAE en Chile), en vez de pasar a la herencia.
        /// </summary>
        [JsonProperty("se_extingue_al_morir")]
        public bool EndsAtDeath { get; set; }

        [JsonProperty("condicion_pago")]
        public string? PaymentCondition { get; set; }

        [JsonIgnore]
        public Expression? CompiledPaymentCondition { get; set; }
    }
}
