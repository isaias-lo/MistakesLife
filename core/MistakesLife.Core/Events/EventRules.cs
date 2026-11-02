namespace MistakesLife.Core.Events
{
    /// <summary>Reglas de equilibrio de los eventos. Se ajustarán con el simulador de vidas.</summary>
    public sealed class EventRules
    {
        /// <summary>Probabilidad de que ocurra un evento al azar en una semana tranquila.</summary>
        public double WeeklyRandomEventChance { get; set; } = 0.2;
    }
}
