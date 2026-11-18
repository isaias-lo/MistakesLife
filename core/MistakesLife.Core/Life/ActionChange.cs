namespace MistakesLife.Core.Life
{
    /// <summary>Algo que cambió al hacer una acción: qué (como en "cambia" de acciones/) y cuánto de verdad.</summary>
    public sealed class ActionChange
    {
        public ActionChange(string key, int delta)
        {
            Key = key;
            Delta = delta;
        }

        /// <summary>felicidad, salud, inteligencia, apariencia, energia, carino o preparar_prueba.</summary>
        public string Key { get; }

        /// <summary>Cuánto cambió de verdad (una barra llena no sube más).</summary>
        public int Delta { get; }
    }
}
