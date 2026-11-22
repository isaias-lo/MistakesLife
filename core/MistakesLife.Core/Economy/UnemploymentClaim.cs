using System.Collections.Generic;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// El cobro del seguro de desempleo por el último trabajo que perdió. Nace cuando
    /// deja un trabajo con contrato y dura mientras no tenga otro: así cada trabajo
    /// perdido se cobra una sola vez. Ver <see cref="UnemploymentInsuranceSystem"/>.
    /// </summary>
    public sealed class UnemploymentClaim
    {
        private readonly List<UnemploymentPayment> _payments = new List<UnemploymentPayment>();

        public UnemploymentClaim(GameDate start, bool hasRight, bool solidarity, decimal salaryBase)
        {
            Start = start;
            HasRight = hasRight;
            Solidarity = solidarity;
            SalaryBase = salaryBase;
            Finished = !hasRight;
        }

        /// <summary>La semana en que se notó que quedó sin trabajo. Los pagos parten el mes siguiente.</summary>
        public GameDate Start { get; }

        /// <summary>Si tenía las cotizaciones mínimas para cobrar.</summary>
        public bool HasRight { get; }

        /// <summary>Si el fondo común puede completar sus pagos (despido sin culpa suya y meses seguidos con el empleador).</summary>
        public bool Solidarity { get; }

        /// <summary>El sueldo promedio con que se calculan los pagos, en dinero de los paquetes.</summary>
        public decimal SalaryBase { get; }

        /// <summary>Si ya no quedan pagos (o nunca hubo derecho).</summary>
        public bool Finished { get; internal set; }

        /// <summary>Los pagos que ya llegaron, en orden.</summary>
        public IReadOnlyList<UnemploymentPayment> Payments => _payments;

        internal void Add(UnemploymentPayment payment) => _payments.Add(payment);
    }
}
