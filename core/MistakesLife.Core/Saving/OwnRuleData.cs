namespace MistakesLife.Core.Saving
{
    /// <summary>Una regla propia del jugador, guardada (desde la versión 21 del formato).</summary>
    internal sealed class OwnRuleData
    {
        public string Key { get; set; } = string.Empty;

        public string Choice { get; set; } = string.Empty;

        public int TimesUsed { get; set; }

        public int? LastUsedWeek { get; set; }
    }
}
