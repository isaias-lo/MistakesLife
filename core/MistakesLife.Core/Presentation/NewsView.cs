namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una noticia lista para el diario de noticias (el diario mural de la casa): el
    /// titular en el idioma del jugador, de qué trata y cuándo salió.
    /// </summary>
    public sealed class NewsView
    {
        public NewsView(string newsId, string headline, string topic, string dateLabel, string ageLabel, int weeksAgo, bool isLocal)
        {
            NewsId = newsId;
            Headline = headline;
            Topic = topic;
            DateLabel = dateLabel;
            AgeLabel = ageLabel;
            WeeksAgo = weeksAgo;
            IsLocal = isLocal;
        }

        /// <summary>Id de la noticia (para elegir un dibujo o un objeto 3D).</summary>
        public string NewsId { get; }

        /// <summary>El titular, ya traducido.</summary>
        public string Headline { get; }

        /// <summary>sismo, clima, economia, politica, sociedad, viral o salud.</summary>
        public string Topic { get; }

        /// <summary>La fecha del mundo en que salió ("Año 2031, semana 9").</summary>
        public string DateLabel { get; }

        /// <summary>La edad que tenía el jugador cuando salió ("34 años").</summary>
        public string AgeLabel { get; }

        /// <summary>Hace cuántas semanas salió (0 = esta semana).</summary>
        public int WeeksAgo { get; }

        /// <summary>Si es una noticia de su ciudad (un temporal en Santiago) y no del país entero.</summary>
        public bool IsLocal { get; }
    }
}
