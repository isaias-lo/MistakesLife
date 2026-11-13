namespace MistakesLife.Core.Presentation
{
    /// <summary>Un correo de la bandeja, ya traducido, para la pantalla del correo.</summary>
    public sealed class MailView
    {
        /// <summary>Cuántas letras, como mucho, lleva la vista previa del cuerpo en la bandeja.</summary>
        public const int PreviewLength = 70;

        public MailView(int index, string mailId, string sender, string subject, string body, string topic, string dateLabel, string ageLabel, bool read)
            : this(index, mailId, sender, subject, body, topic, dateLabel, ageLabel, string.Empty, read)
        {
        }

        public MailView(int index, string mailId, string sender, string subject, string body, string topic, string dateLabel, string ageLabel, string whenLabel, bool read)
        {
            Index = index;
            MailId = mailId;
            Sender = sender;
            Subject = subject;
            Body = body;
            Topic = topic;
            DateLabel = dateLabel;
            AgeLabel = ageLabel;
            WhenLabel = whenLabel;
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

        /// <summary>Hace cuánto llegó, como en la bandeja de un correo de verdad ("Esta semana", "Hace 3 meses").</summary>
        public string WhenLabel { get; }

        public bool Read { get; }

        /// <summary>El comienzo del cuerpo, cortado en una palabra, para la línea gris de la bandeja.</summary>
        public string Preview
        {
            get
            {
                if (Body.Length <= PreviewLength)
                {
                    return Body;
                }

                int cut = Body.LastIndexOf(' ', PreviewLength);
                return Body.Substring(0, cut > 0 ? cut : PreviewLength).TrimEnd(',', '.', ';', ':') + "...";
            }
        }
    }
}
