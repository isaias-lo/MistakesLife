using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// Reglas del celular y del correo: recibir un celular (con las apps que
    /// vienen de fábrica), perderlo y crear la cuenta de correo.
    /// </summary>
    public static class PhoneRules
    {
        /// <summary>Recibe un celular nuevo, con las apps de fábrica. Reemplaza al anterior.</summary>
        public static Phone GivePhone(World world, ContentDatabase content, Character character)
        {
            var phone = new Phone(world.Date);
            foreach (AppDefinition app in content.Apps.Where(a => a.Preinstalled))
            {
                phone.Install(app.Id);
            }

            character.Phone = phone;
            return phone;
        }

        /// <summary>Pierde el celular (se lo roban, se le rompe): se van sus apps.</summary>
        public static void LosePhone(Character character)
        {
            character.Phone = null;
        }

        /// <summary>
        /// Crea su cuenta de correo. Solo le llegan correos de lo que pase desde
        /// ahora. Si ya tenía una, no pasa nada.
        /// </summary>
        public static MailAccount CreateMail(World world, Character character)
        {
            if (character.Mail == null)
            {
                character.Mail = new MailAccount(world.Date, world.Journal.Count);
            }

            return character.Mail;
        }

        /// <summary>Le llega un correo, si tiene cuenta (sin cuenta, el correo nunca llega).</summary>
        public static void Receive(World world, Character character, string mailId)
        {
            if (string.IsNullOrWhiteSpace(mailId))
            {
                throw new ArgumentException("Falta el id del correo.", nameof(mailId));
            }

            character.Mail?.Receive(world.Date, mailId);
        }
    }
}
