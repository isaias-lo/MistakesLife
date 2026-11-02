namespace MistakesLife.Core.Scripting
{
    /// <summary>
    /// Lo que una expresión puede ver: nombres raíz (jugador, mundo) y funciones
    /// (semilla, plantar, diario…).
    /// </summary>
    public interface IScriptScope
    {
        /// <summary>
        /// Si es true, la expresión es una condición: no puede cambiar nada.
        /// </summary>
        bool ReadOnly { get; }

        Value GetRoot(string name);

        Value CallFunction(string name, Value[] args);
    }
}
