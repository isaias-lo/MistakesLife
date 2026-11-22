namespace MistakesLife.Core.Work
{
    /// <summary>Una tarea del mes en la pega (D5b): cuál es, para cuándo y cómo salió.</summary>
    public sealed class WorkTask
    {
        public WorkTask(string id, int dueWeek)
        {
            Id = id;
            DueWeek = dueWeek;
        }

        /// <summary>La tarea del paquete de la carrera ("tarea.contabilidad.iva").</summary>
        public string Id { get; }

        /// <summary>
        /// La semana en que vence (contada desde el inicio del calendario, como
        /// <see cref="Time.GameDate.TotalWeeks"/>): ese viernes es el plazo.
        /// </summary>
        public int DueWeek { get; internal set; }

        /// <summary>Si ya la dejó para después una vez (la segunda vez no se puede: ya está atrasada).</summary>
        public bool Postponed { get; internal set; }

        /// <summary>
        /// Cómo salió, en puntos de cómo te ve tu jefa (positivo bien, negativo mal); null mientras
        /// no se ha hecho.
        /// </summary>
        public int? Result { get; internal set; }

        /// <summary>Si ya la hizo (bien o mal).</summary>
        public bool Done => Result.HasValue;

        /// <summary>
        /// Cuántas de las tres preguntas acertó al resolverla con lo que sabe (D5b2), justo antes de
        /// elegir esa forma. No se guarda: se usa al tiro en <see cref="WorkTaskRules.Do"/>.
        /// </summary>
        public int? QuizCorrect { get; internal set; }
    }
}
