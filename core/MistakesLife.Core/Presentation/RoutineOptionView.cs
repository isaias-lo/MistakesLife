namespace MistakesLife.Core.Presentation
{
    /// <summary>Una actividad que se puede pegar en un bloque del horario.</summary>
    public sealed class RoutineOptionView
    {
        public RoutineOptionView(string activityId, string text)
        {
            ActivityId = activityId;
            Text = text;
        }

        public string ActivityId { get; }

        public string Text { get; }
    }
}
