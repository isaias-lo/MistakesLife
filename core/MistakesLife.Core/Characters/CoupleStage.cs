namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// En qué etapa va una pareja (D4d1). La unión ante la ley (matrimonio, unión civil) va
    /// aparte, en <see cref="Character.UnionTypeId"/>, porque se puede estar casado y separado de hecho.
    /// </summary>
    public enum CoupleStage
    {
        /// <summary>Están saliendo: se están conociendo, todavía no son pareja formal.</summary>
        Dating,

        /// <summary>Pololean (son novios): pareja formal, cada uno en su casa.</summary>
        Steady,

        /// <summary>Viven juntos: comparten la casa, la comida y los gastos.</summary>
        LivingTogether,
    }
}
