namespace MistakesLife.Core.Saving
{
    /// <summary>Una línea de la cartola, guardada (desde la versión 51 del formato).</summary>
    internal sealed class BankMovementData
    {
        public int Week { get; set; }

        public string Id { get; set; } = "";

        public decimal Amount { get; set; }

        public string Detail { get; set; } = "";
    }
}
