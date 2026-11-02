namespace MistakesLife.Core.Scripting
{
    /// <summary>
    /// Una función llamada con un texto fijo, por ejemplo semilla('copio_paes').
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
