using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>La cuenta del banco con su cartola, guardada (desde la versión 51 del formato).</summary>
    internal sealed class BankAccountData
    {
        public string BankId { get; set; } = "";

        public int OpenedWeek { get; set; }

        public List<BankMovementData> Movements { get; set; } = new List<BankMovementData>();
    }
}
