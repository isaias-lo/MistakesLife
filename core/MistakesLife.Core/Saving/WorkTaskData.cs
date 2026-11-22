namespace MistakesLife.Core.Saving
{
    /// <summary>Una tarea del mes guardada (desde la versión 49, D5b).</summary>
    internal sealed class WorkTaskData
    {
        public string Id { get; set; } = string.Empty;

        public int DueWeek { get; set; }

        public bool Postponed { get; set; }

        public int? Result { get; set; }
    }
}
