namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las partes del día que llena el jugador. La mañana no aparece: la ponen el
    /// colegio, los estudios o el trabajo.
    /// </summary>
    public enum RoutineBlock
    {
        /// <summary>Después de clases o del trabajo.</summary>
        Afternoon = 0,

        /// <summary>Después de comer, antes de dormir.</summary>
        Night = 1,
    }
}
