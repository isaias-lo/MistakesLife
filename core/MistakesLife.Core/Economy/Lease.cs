using System;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// El contrato de arriendo que firmó el jugador: qué arrienda, dónde, cuánto
    /// paga y cuánto le queda. El arriendo de cada mes sale de aquí (no de un monto
    /// fijo del paquete): es consecuencia de lo que el jugador eligió.
    /// </summary>
    public sealed class Lease
    {
        public Lease(string typeId, string communeId, decimal baseRent, double adjustedPriceIndex, int monthsLeft, decimal deposit)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                throw new ArgumentException("El contrato necesita el tipo de vivienda.", nameof(typeId));
            }

            if (baseRent < 0 || deposit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRent), "El arriendo y la garantía no pueden ser negativos.");
            }

            TypeId = typeId;
            CommuneId = communeId ?? string.Empty;
            BaseRent = baseRent;
            AdjustedPriceIndex = adjustedPriceIndex;
            MonthsLeft = monthsLeft;
            Deposit = deposit;
        }

        /// <summary>Tipo de vivienda arrendada, por ejemplo "vivienda.pieza".</summary>
        public string TypeId { get; }

        /// <summary>Comuna donde está.</summary>
        public string CommuneId { get; }

        /// <summary>
        /// Arriendo mensual acordado, en dinero del inicio del mundo. 0 en un contrato
        /// que vino de un guardado viejo: toma el precio de la comuna el primer mes.
        /// </summary>
        public decimal BaseRent { get; set; }

        /// <summary>Índice de precios del último reajuste: entre reajustes el arriendo no cambia aunque haya inflación.</summary>
        public double AdjustedPriceIndex { get; set; }

        /// <summary>Meses desde el último reajuste.</summary>
        public int MonthsSinceAdjustment { get; set; }

        /// <summary>Meses que le quedan al contrato. En 0 sigue mes a mes hasta que se renueve o se deje.</summary>
        public int MonthsLeft { get; set; }

        /// <summary>Garantía que dejó al firmar (a precios de ese día); se devuelve al irse.</summary>
        public decimal Deposit { get; }

        /// <summary>Lo que se paga este mes, a precios del último reajuste.</summary>
        public decimal MonthlyRent => PayrollCalculator.ToToday(BaseRent, AdjustedPriceIndex);
    }
}
