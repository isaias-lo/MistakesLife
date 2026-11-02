namespace MistakesLife.Core.Economy
{
    /// <summary>Cómo trabaja alguien: con contrato o a honorarios.</summary>
    public enum JobKind
    {
        /// <summary>Con contrato (en el lenguaje, 'dependiente'): le descuentan cotizaciones e impuesto cada mes.</summary>
        Employee,

        /// <summary>A honorarios ('honorarios'): le retienen una parte y el resto se arregla en la declaración anual.</summary>
        Freelance,
    }
}
