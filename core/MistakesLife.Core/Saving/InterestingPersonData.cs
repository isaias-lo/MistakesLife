namespace MistakesLife.Core.Saving
{
    /// <summary>Alguien interesante de tu horario (Characters.InterestingPerson), guardado (desde la versión 41).</summary>
    internal sealed class InterestingPersonData
    {
        public int PersonId { get; set; }

        public string Source { get; set; } = string.Empty;

        public int SinceWeek { get; set; }

        public int Declines { get; set; }
    }
}
