using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Devices
{
    /// <summary>
    /// Fase de consecuencias, al final de la semana: reparte el correo. Revisa
    /// lo que se anotó en el diario desde la última vez y, si un correo de
    /// packs/*/mail/ escucha esa línea ("al_anotar"), le llega a quien tiene
    /// cuenta de correo. Así los sistemas y los eventos no necesitan saber del
    /// correo: el contenido decide qué llega por correo (un rechazo de trabajo,
    /// una cobranza, la devolución de impuestos).
    /// Lo que se anota al elegir una opción (entre una semana y otra) llega la
    /// semana siguiente, con la fecha en que pasó.
    /// </summary>
    public sealed class MailSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.correo";

        private readonly ContentDatabase _content;

        public MailSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Consequences;

        public void Tick(World world, WeekContext context)
        {
            Deliver(world, _content);
        }

        /// <summary>Entrega los correos de lo anotado en el diario desde la última vez.</summary>
        public static void Deliver(World world, ContentDatabase content)
        {
            IReadOnlyList<JournalEntry> journal = world.Journal;
            foreach (Character character in world.Characters.Where(c => c.Mail != null && c.IsAlive).OrderBy(c => c.Id))
            {
                MailAccount mail = character.Mail!;
                for (int i = Math.Min(mail.JournalCursor, journal.Count); i < journal.Count; i++)
                {
                    JournalEntry entry = journal[i];
                    if (entry.CharacterId != character.Id)
                    {
                        continue;
                    }

                    foreach (MailDefinition definition in content.MailsForJournal(entry.TextKey))
                    {
                        mail.Receive(entry.Date, definition.Id);
                    }
                }

                mail.JournalCursor = journal.Count;
            }
        }
    }
}
