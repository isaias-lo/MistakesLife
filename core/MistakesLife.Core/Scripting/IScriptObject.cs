namespace MistakesLife.Core.Scripting
{
    /// <summary>
    /// Un objeto del juego visible desde las condiciones y efectos
    /// (por ejemplo "jugador" o el resultado de semilla('x')).
    /// </summary>
    public interface IScriptObject
    {
        /// <summary>Lee un dato: jugador.edad, semilla('x').fuerza, semilla('x').cerrar…</summary>
        Value Get(string member, IScriptScope scope);

        /// <summary>Cambia un dato: jugador.salud -= 10.</summary>
        void Set(string member, Value value, IScriptScope scope);

        /// <summary>Llama un método: jugador.tiene('postulando').</summary>
        Value Call(string method, Value[] args, IScriptScope scope);
    }
}
