using System.Collections.Generic;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// La cuenta del jugador en un banco de su país. Guarda la cartola de los últimos dos
    /// años: cada entrada y salida de plata con su fecha y su causa.
    /// </summary>
    public sealed class BankAccount
    {
        /// <summary>Cuántas semanas de cartola se guardan (dos años).</summary>
        public const int KeptWeeks = 2 * GameDate.WeeksPerYear;

        private readonly List<BankMovement> _movements = new List<BankMovement>();

        public BankAccount(string bankId, GameDate opened)
        {
            BankId = bankId;
            Opened = opened;
        }

        /// <summary>El banco (id de economia.bancos).</summary>
        public string BankId { get; }

        /// <summary>Cuándo se abrió.</summary>
        public GameDate Opened { get; }

        /// <summary>La cartola, de la más antigua a la más nueva.</summary>
        public IReadOnlyList<BankMovement> Movements => _movements;

        /// <summary>Anota un movimiento y borra los que tienen más de dos años.</summary>
        public void Add(BankMovement movement)
        {
            _movements.Add(movement);
            int old = 0;
            while (old < _movements.Count && movement.Date.WeeksSince(_movements[old].Date) > KeptWeeks)
            {
                old++;
            }

            if (old > 0)
            {
                _movements.RemoveRange(0, old);
            }
        }
    }
}
