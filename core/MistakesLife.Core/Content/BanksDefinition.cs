using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.bancos" de country.json: los bancos del país donde el jugador puede
    /// abrir su cuenta. Mientras no tiene cuenta maneja efectivo; con cuenta, cada peso que
    /// entra o sale queda anotado en su cartola.
    /// </summary>
    public sealed class BanksDefinition
    {
        /// <summary>Desde qué edad se puede abrir una cuenta a nombre propio (Chile: 18).</summary>
        [JsonProperty("edad_minima")]
        public int MinAge { get; set; } = 18;

        /// <summary>Los bancos, en el orden en que se ofrecen.</summary>
        [JsonProperty("lista", Required = Required.Always)]
        public List<BankDefinition> Banks { get; set; } = new List<BankDefinition>();

        /// <summary>Línea del diario al abrir la cuenta.</summary>
        [JsonProperty("diario_cuenta_abierta")]
        public string? OpenedJournal { get; set; }

        /// <summary>Línea del diario cuando el banco dice que no porque pide contrato.</summary>
        [JsonProperty("diario_rechazo_contrato")]
        public string? RefusedContractJournal { get; set; }

        /// <summary>Línea del diario cuando el banco dice que no porque el sueldo no alcanza.</summary>
        [JsonProperty("diario_rechazo_renta")]
        public string? RefusedIncomeJournal { get; set; }

        /// <summary>Línea del diario cuando todavía no tiene la edad para abrir una cuenta.</summary>
        [JsonProperty("diario_rechazo_edad")]
        public string? RefusedAgeJournal { get; set; }

        /// <summary>Busca un banco por id, o null.</summary>
        public BankDefinition? Bank(string id) => Banks.FirstOrDefault(b => b.Id == id);
    }
}
