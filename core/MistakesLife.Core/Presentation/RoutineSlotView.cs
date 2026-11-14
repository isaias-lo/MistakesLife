namespace MistakesLife.Core.Presentation
{
    /// <summary>Un bloque del horario: una tarde o una noche de un día.</summary>
    public sealed class RoutineSlotView
    {
        public RoutineSlotView(int day, Life.RoutineBlock block, string? activityId, string text, bool available)
        {
            Day = day;
            Block = block;
            ActivityId = activityId;
            Text = text;
            Available = available;
        }

        /// <summary>De 0 (lunes) a 6 (domingo).</summary>
        public int Day { get; }

        public Life.RoutineBlock Block { get; }

        /// <summary>Lo que hay (null si está libre).</summary>
        public string? ActivityId { get; }

        /// <summary>Lo que dice el sticker ("plaza con los amigos", "libre").</summary>
        public string Text { get; }

        /// <summary>Si todavía se puede hacer (por ejemplo, ya no hay amigos con quién ir a la plaza).</summary>
        public bool Available { get; }
    }
}
