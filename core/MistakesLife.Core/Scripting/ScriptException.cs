using System;

namespace MistakesLife.Core.Scripting
{
    /// <summary>Error al leer o ejecutar una condición o un efecto.</summary>
    public sealed class ScriptException : Exception
    {
        public ScriptException(string message)
            : base(message)
        {
        }
    }
}
