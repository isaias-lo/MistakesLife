namespace MistakesLife.Core.Presentation
{
    /// <summary>Una app del celular, lista para dibujar su ícono en la pantalla del celular.</summary>
    public sealed class AppView
    {
        public AppView(string appId, string name, int unread)
        {
            AppId = appId;
            Name = name;
            Unread = unread;
        }

        /// <summary>Id de la app ("app.correo"): con él la pantalla elige el ícono y qué abrir.</summary>
        public string AppId { get; }

        /// <summary>El nombre de la app, ya traducido.</summary>
        public string Name { get; }

        /// <summary>Cuántas cosas nuevas tiene (en el correo, los correos sin leer). 0 si nada.</summary>
        public int Unread { get; }

        /// <summary>Si es el correo, que abre la bandeja.</summary>
        public bool IsMail => AppId == MailAppId;

        /// <summary>Id de la app del correo.</summary>
        public const string MailAppId = "app.correo";
    }
}
