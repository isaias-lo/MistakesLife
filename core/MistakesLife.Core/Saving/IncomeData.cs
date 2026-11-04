namespace MistakesLife.Core.Saving
{
    /// <summary>Lo ganado en un año, guardado para la declaración de impuestos.</summary>
    internal sealed class IncomeData
    {
        public int Year { get; set; }

        public decimal EmployeeTaxable { get; set; }

        public decimal TaxWithheld { get; set; }

        public decimal FreelanceGross { get; set; }

        public decimal FreelanceRetention { get; set; }
    }
}
