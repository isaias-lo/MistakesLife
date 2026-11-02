namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// Reglas de equilibrio del sistema de semillas. Valores iniciales para
    /// probar; se ajustarán con el simulador de vidas (tarea 7).
    /// </summary>
    public sealed class SeedRules
    {
        /// <summary>Semanas que cuenta como "un mes" para el límite.</summary>
        public int WeeksPerMonth { get; set; } = 4;

        /// <summary>Máximo de semillas que despiertan por mes, para que no sea un castigo constante.</summary>
        public int MaxAwakeningsPerMonth { get; set; } = 1;

        /// <summary>
        /// Probabilidad semanal de despertar con fuerza 100 (con fuerza 50 es la mitad).
        /// </summary>
        public double WeeklyWakeChanceAtFullStrength { get; set; } = 0.25;

        /// <summary>Fuerza que pierde una semilla cuando muere uno de sus testigos.</summary>
        public int StrengthLostPerDeadWitness { get; set; } = 15;
    }
}
