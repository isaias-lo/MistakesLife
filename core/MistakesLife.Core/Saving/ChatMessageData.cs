namespace MistakesLife.Core.Saving
{
    /// <summary>Un mensaje de Mensajito guardado (desde la versión 15).</summary>
    internal sealed class ChatMessageData
    {
        public int Week { get; set; }

        public string TextKey { get; set; } = string.Empty;

        public bool FromPlayer { get; set; }
    }
}
