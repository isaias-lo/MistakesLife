namespace MistakesLife.Core.Economy
{
    /// <summary>Cómo trabaja alguien: con contrato, a honorarios o informal (sin contrato).</summary>
    public enum JobKind
    {
        /// <summary>Con contrato (en el lenguaje, 'dependiente'): le descuentan cotizaciones e impuesto cada mes.</summary>
        Employee,

        /// <summary>A honorarios ('honorarios'): le retienen una parte y el resto se arregla en la declaración anual.</summary>
        Freelance,

        /// <summary>
        /// Informal, sin contrato ('informal'): recibe el sueldo completo, pero sin
        /// cotizaciones ni impuesto. Esos meses no suman a su pensión (lagunas).
        /// </summary>
        Informal,
    }
}
