namespace MistakesLife.Core.Presentation
{
    /// <summary>Una etapa en la línea del proceso de la carpeta del juzgado.</summary>
    public sealed class CourtStageView
    {
        public CourtStageView(string stageId, string name, int weeks, bool done, bool current)
        {
            StageId = stageId;
            Name = name;
            Weeks = weeks;
            Done = done;
            Current = current;
        }

        public string StageId { get; }

        /// <summary>El nombre de la etapa en el idioma del jugador ("Notificación").</summary>
        public string Name { get; }

        /// <summary>El plazo de la etapa en semanas.</summary>
        public int Weeks { get; }

        /// <summary>Si ya pasó.</summary>
        public bool Done { get; }

        /// <summary>Si es la etapa en que va la causa.</summary>
        public bool Current { get; }
    }
}
