namespace MistakesLife.Core.Saving
{
    /// <summary>Un correo de la bandeja, guardado (desde la versión 10).</summary>
    internal sealed class MailMessageData
    {
        public int Week { get; set; }

        public string MailId { get; set; } = string.Empty;

        public bool Read { get; set; }
    }
}
