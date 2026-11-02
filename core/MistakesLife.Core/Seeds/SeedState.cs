namespace MistakesLife.Core.Seeds
{
    /// <summary>Etapa del ciclo de vida de una semilla.</summary>
    public enum SeedState
    {
        /// <summary>Plantada y esperando. El jugador no sabe cuándo volverá.</summary>
        Dormant,

        /// <summary>Despertó: espera que el motor de eventos la convierta en una escena.</summary>
        Awake,

        /// <summary>Su evento ya se jugó y se cerró.</summary>
        Resolved,

        /// <summary>Se olvidó: pasó demasiado tiempo o su fuerza llegó a cero.</summary>
        Expired,
    }
}
