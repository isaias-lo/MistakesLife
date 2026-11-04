namespace MistakesLife.Core.Saving
{
    /// <summary>Una línea de dinero guardada (un descuento o un gasto).</summary>
    internal sealed class MoneyLineData
    {
        public string Id { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}
