namespace MistakesLife.Core.Presentation
{
    /// <summary>Lo que contesta el banco al pedir tu cuenta (D6-1c): si quedó lista y qué cambia.</summary>
    public sealed class BankAnswerView
    {
        public BankAnswerView(bool opened, string title, string message)
        {
            Opened = opened;
            Title = title;
            Message = message;
        }

        /// <summary>Si la cuenta quedó abierta.</summary>
        public bool Opened { get; }

        /// <summary>"Tu cuenta corriente está lista" o "El banco te dijo que no".</summary>
        public string Title { get; }

        /// <summary>Qué pasó con tu efectivo, cuánto cobra y qué te llega; o el motivo del rechazo.</summary>
        public string Message { get; }
    }
}
