namespace MistakesLife.Core.Content
{
    /// <summary>Un problema encontrado al cargar o validar un paquete.</summary>
    public sealed class ContentError
    {
        public ContentError(string source, string message)
        {
            Source = source;
            Message = message;
        }

        /// <summary>Dónde está el problema (archivo e id).</summary>
        public string Source { get; }

        public string Message { get; }

        public override string ToString() => $"{Source}: {Message}";
    }
}
