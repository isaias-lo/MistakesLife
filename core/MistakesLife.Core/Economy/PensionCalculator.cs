using System;
using MistakesLife.Core.Content;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Las cuentas de las pensiones. El fondo y las pensiones se guardan en dinero
    /// del inicio del mundo (sin inflación, como la UF en Chile), así la inflación
    /// no se come la pensión; al pagarla se lleva a precios de hoy.
    /// </summary>
    public static class PensionCalculator
    {
        /// <summary>Cuánto crece el fondo en un mes: la rentabilidad anual repartida en 12 meses.</summary>
        public static double MonthlyReturn(double annualReturn) => Math.Pow(1 + annualReturn, 1.0 / 12) - 1;

        /// <summary>
        /// La pensión fija mensual que paga un fondo durante los años de pensión del
        /// país, ganando su rentabilidad mientras se paga (como una renta vitalicia).
        /// </summary>
        public static decimal MonthlyPension(decimal fund, PensionAccountDefinition rules)
        {
            int months = Math.Max(1, rules.PensionYears * 12);
            double rate = MonthlyReturn(rules.RealAnnualReturn);
            if (fund <= 0)
            {
                return 0;
            }

            if (rate <= 0)
            {
                return fund / months;
            }

            double factor = rate / (1 - Math.Pow(1 + rate, -months));
            return fund * (decimal)factor;
        }

        /// <summary>
        /// Cuánto de la pensión del Estado recibe alguien con esa pensión propia:
        /// completa hasta "completa_hasta", nada desde "sin_pension_desde" y, entre
        /// medio, baja en línea recta.
        /// </summary>
        public static decimal UniversalPension(decimal ownPension, UniversalPensionDefinition rules)
        {
            if (rules.FullUpTo.HasValue && rules.NoneFrom.HasValue && rules.NoneFrom > rules.FullUpTo)
            {
                if (ownPension >= rules.NoneFrom.Value)
                {
                    return 0;
                }

                if (ownPension > rules.FullUpTo.Value)
                {
                    return rules.Amount * (rules.NoneFrom.Value - ownPension) / (rules.NoneFrom.Value - rules.FullUpTo.Value);
                }
            }

            return rules.Amount;
        }
    }
}
