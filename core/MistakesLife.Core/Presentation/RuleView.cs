namespace MistakesLife.Core.Presentation
{
    /// <summary>Una regla propia del jugador, lista para mostrar ("Cuando hay prueba → Estudiar").</summary>
    public sealed class RuleView
    {
        public RuleView(string key, string situation, string choice, int timesUsed)
        {
            Key = key;
            Situation = situation;
            Choice = choice;
            TimesUsed = timesUsed;
        }

        /// <summary>Para borrarla con <see cref="LifePresenter.RemoveRule"/>.</summary>
        public string Key { get; }

        /// <summary>Cuándo se usa ("Cuando hay prueba o tarea").</summary>
        public string Situation { get; }

        /// <summary>Qué hace ("Estudiar").</summary>
        public string Choice { get; }

        /// <summary>Cuántas veces se eligió sola.</summary>
        public int TimesUsed { get; }
    }
}
