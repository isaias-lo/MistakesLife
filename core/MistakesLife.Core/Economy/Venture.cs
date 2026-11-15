namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Un negocio con un socio (ver <see cref="Work.VentureRules"/>): con quién, si es formal
    /// (una empresa constituida) o de palabra, cuánto puso el jugador y cómo le va.
    /// </summary>
    public sealed class Venture
    {
        public Venture(int partnerId, bool formal, decimal contribution)
            : this(partnerId, formal, contribution, 1)
        {
        }

        public Venture(int partnerId, bool formal, decimal contribution, double luck)
        {
            Luck = luck;
            PartnerId = partnerId;
            Formal = formal;
            Contribution = contribution;
        }

        /// <summary>El socio o la socia (id de personaje).</summary>
        public int PartnerId { get; }

        /// <summary>Si es una empresa constituida (en Chile, una SpA de Empresa en un Día) o un trato de palabra.</summary>
        public bool Formal { get; }

        /// <summary>Lo que puso el jugador al empezar, en dinero del inicio del mundo.</summary>
        public decimal Contribution { get; }

        /// <summary>
        /// Qué tan bueno salió el negocio (1 = como el promedio): se elige al abrir, porque unos
        /// negocios andan mucho mejor que otros (ver <see cref="Content.VenturesDefinition.Spread"/>).
        /// </summary>
        public double Luck { get; }

        /// <summary>Meses abierto.</summary>
        public int MonthsOpen { get; internal set; }

        /// <summary>Lo que le tocó al jugador el último mes (su parte, después de impuestos), en la moneda de hoy.</summary>
        public decimal LastShare { get; internal set; }

        /// <summary>Todo lo que ha recibido el jugador del negocio, en la moneda de cada mes.</summary>
        public decimal TotalShare { get; internal set; }
    }
}
