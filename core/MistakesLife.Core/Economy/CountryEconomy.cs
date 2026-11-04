using System;
using System.Collections.Generic;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// El estado de la economía de un país durante la partida: en qué régimen
    /// de inflación está, cuánta inflación hay y cuánto subieron los precios.
    /// </summary>
    public sealed class CountryEconomy
    {
        /// <summary>Meses de historia que se guardan para calcular la inflación de los últimos 12 meses.</summary>
        private const int HistoryMonths = 12;

        private readonly Queue<double> _indexHistory = new Queue<double>();

        public CountryEconomy(string countryId, string regimeId, double annualInflation)
        {
            CountryId = countryId ?? throw new ArgumentNullException(nameof(countryId));
            RegimeId = regimeId ?? throw new ArgumentNullException(nameof(regimeId));
            AnnualInflation = annualInflation;
        }

        /// <summary>Fase actual del ciclo económico (por ejemplo "economia.ciclo.normal"), o null si el país no tiene ciclo.</summary>
        public string? PhaseId { get; internal set; }

        /// <summary>Por ejemplo "pais.chile".</summary>
        public string CountryId { get; }

        /// <summary>Régimen de inflación actual, por ejemplo "economia.inflacion.estable".</summary>
        public string RegimeId { get; private set; }

        /// <summary>Ritmo actual de la inflación, como tasa anual (0.03 = 3 % al año).</summary>
        public double AnnualInflation { get; internal set; }

        /// <summary>
        /// Índice de precios: 1 cuando empieza el mundo. Si vale 2, todo cuesta el
        /// doble que al principio. Los montos de los paquetes se escriben en dinero
        /// del inicio y se multiplican por este índice (función precio()).
        /// </summary>
        public double PriceIndex { get; private set; } = 1;

        /// <summary>
        /// Cuánto subieron los precios en los últimos 12 meses (0.03 = 3 %). Antes de
        /// cumplirse el primer año, el ritmo actual.
        /// </summary>
        public double InflationLast12Months =>
            _indexHistory.Count < HistoryMonths ? AnnualInflation : (PriceIndex / _indexHistory.Peek()) - 1;

        /// <summary>Cambia de régimen, con una nueva inflación anual.</summary>
        /// <summary>Índices de precios de los últimos meses, del más antiguo al más nuevo (para guardar).</summary>
        internal IEnumerable<double> IndexHistory => _indexHistory;

        /// <summary>Repone el estado guardado de la economía (al cargar una partida).</summary>
        internal void Restore(double priceIndex, IEnumerable<double> indexHistory)
        {
            PriceIndex = priceIndex;
            _indexHistory.Clear();
            foreach (double index in indexHistory)
            {
                _indexHistory.Enqueue(index);
            }
        }

        internal void ChangeRegime(string regimeId, double annualInflation)
        {
            RegimeId = regimeId ?? throw new ArgumentNullException(nameof(regimeId));
            AnnualInflation = annualInflation;
        }

        /// <summary>Pasa un mes: los precios suben según la inflación anual actual.</summary>
        internal void AdvanceMonth()
        {
            _indexHistory.Enqueue(PriceIndex);
            if (_indexHistory.Count > HistoryMonths)
            {
                _indexHistory.Dequeue();
            }

            PriceIndex *= Math.Pow(1 + AnnualInflation, 1.0 / 12);
        }
    }
}
