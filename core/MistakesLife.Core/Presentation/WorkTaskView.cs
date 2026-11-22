namespace MistakesLife.Core.Presentation
{
    /// <summary>Una tarea del mes en la agenda de la pega (D5b), lista para mostrar.</summary>
    public sealed class WorkTaskView
    {
        public WorkTaskView(string id, string text, string due, string state, bool done, bool late)
        {
            Id = id;
            Text = text;
            Due = due;
            State = state;
            Done = done;
            Late = late;
        }

        /// <summary>La tarea del paquete ("tarea.contabilidad.iva").</summary>
        public string Id { get; }

        /// <summary>Qué hay que hacer ("Declarar el IVA (F29)").</summary>
        public string Text { get; }

        /// <summary>El plazo ("viernes 20").</summary>
        public string Due { get; }

        /// <summary>Cómo va ("Hecha", "Salió con errores", "Atrasada", "Pendiente").</summary>
        public string State { get; }

        /// <summary>Si ya la hizo (se tacha en la agenda).</summary>
        public bool Done { get; }

        /// <summary>Si la dejó para después (se marca en rojo).</summary>
        public bool Late { get; }
    }
}
