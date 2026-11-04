using System;

namespace MistakesLife.Core.Saving
{
    /// <summary>
    /// El texto o los bytes no son una partida guardada que este juego pueda abrir:
    /// están dañados, son de otro programa o vienen de una versión más nueva del juego.
    /// </summary>
    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message)
            : base(message)
        {
        }

        public SaveFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
