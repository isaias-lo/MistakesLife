namespace MistakesLife.Core.Scripting
{
    /// <summary>
    /// Una función llamada con un texto fijo, por ejemplo semilla('copio_paes'), o un
    /// método (su nombre empieza con un punto: ".endeudarse" para jugador.endeudarse('deuda.chile.cae', 1000)).
    /// La validación de paquetes la usa para revisar que esos ids existan.
    /// </summary>
    public readonly struct ScriptReference
    {
        public ScriptReference(string function, string argument)
        {
            Function = function;
            Argument = argument;
        }

        public string Function { get; }

        public string Argument { get; }
    }
}
