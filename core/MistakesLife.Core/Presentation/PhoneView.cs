using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// El celular del jugador para la pantalla: si tiene, sus apps y cuántos correos
    /// no ha leído. Sin celular, igual puede tener correo (se lee en el computador de la casa).
    /// </summary>
    public sealed class PhoneView
    {
        public PhoneView(bool hasPhone, bool hasMail, int unreadMail, IReadOnlyList<AppView> apps)
        {
            HasPhone = hasPhone;
            HasMail = hasMail;
            UnreadMail = unreadMail;
            Apps = apps;
        }

        public bool HasPhone { get; }

        public bool HasMail { get; }

        /// <summary>Correos sin leer (para el punto rojo del botón Celular).</summary>
        public int UnreadMail { get; }

        /// <summary>Las apps instaladas, en el orden en que las instaló (vacío si no tiene celular).</summary>
        public IReadOnlyList<AppView> Apps { get; }
    }
}
