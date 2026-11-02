using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// La liquidación de sueldo de un mes, como la que recibe una persona real:
    /// sueldo bruto, cada descuento y el líquido que llega a la cuenta.
    /// </summary>
    public sealed class Payslip
    {
        /// <summary>Clave del texto de la línea del impuesto mensual.</summary>
        public const string IncomeTaxId = "descuento.impuesto_mensual";

        public Payslip(GameDate date, JobKind kind, decimal gross, IReadOnlyList<PayslipLine> lines, decimal taxable)
        {
            Taxable = taxable;
            Date = date;
            Kind = kind;
            Gross = gross;
            Lines = lines;
        }

        public GameDate Date { get; }

        public JobKind Kind { get; }

        /// <summary>Sueldo bruto del mes.</summary>
        public decimal Gross { get; }

        /// <summary>Descuentos, en orden; el impuesto mensual va al final si lo hay.</summary>
        public IReadOnlyList<PayslipLine> Lines { get; }

        /// <summary>Sueldo sobre el que se calcula el impuesto: bruto menos las cotizaciones que lo rebajan.</summary>
        public decimal Taxable { get; }

        /// <summary>Lo que llega a la cuenta: bruto menos todos los descuentos.</summary>
        public decimal Net => Gross - Lines.Sum(l => l.Amount);

        /// <summary>Lo descontado por un descuento (0 si no está).</summary>
        public decimal Amount(string id) => Lines.Where(l => l.Id == id).Sum(l => l.Amount);
    }
}
