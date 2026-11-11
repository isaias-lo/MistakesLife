namespace MistakesLife.Core.Presentation
{
    /// <summary>Un correo de la bandeja, ya traducido, para la pantalla del correo.</summary>
    public sealed class MailView
    {
        public MailView(int index, string mailId, string sender, string subject, string body, string topic, string dateLabel, string ageLabel, bool read)
        {
            Index = index;
            MailId = mailId;
            Sender = sender;
            Subject = subject;
            Body = body;
            Topic = topic;
            DateLabel = dateLabel;
            AgeLabel = ageLabel;
            Read = read;
        }

        /// <summary>Su posición en la bandeja: se pasa a <see cref="LifePresenter.ReadMail"/> al abrirlo.</summary>
        public int Index { get; }

        /// <summary>Id del correo ("correo.busqueda.rechazo").</summary>
        public string MailId { get; }

        /// <summary>Quién lo manda ("Equipo de selección").</summary>
        public string Sender { get; }

        public string Subject { get; }

        public string Body { get; }

        /// <summary>trabajo, estudio, banco, estado, juzgado, personal, publicidad o estafa (para el ícono).</summary>
        public string Topic { get; }

        /// <summary>La fecha del mundo en que llegó ("Año 2031, semana 9").</summary>
        public string DateLabel { get; }

        /// <summary>La edad que tenía el jugador cuando llegó ("34 años").</summary>
        public string AgeLabel { get; }

        public bool Read { get; }
    }
}
