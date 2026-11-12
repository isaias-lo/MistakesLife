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
            : this(function, argument, null)
        {
        }

        public ScriptReference(string function, string argument, string? secondArgument)
        {
            Function = function;
            Argument = argument;
            SecondArgument = secondArgument;
        }

        public string Function { get; }

        public string Argument { get; }

        /// <summary>El segundo texto fijo, si lo hay (la etapa en causa_en('causa.x', 'etapa')).</summary>
        public string? SecondArgument { get; }
    }
}
