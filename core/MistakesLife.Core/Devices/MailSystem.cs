using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
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
            DeliverPeriodic(world, _content);
        }

        /// <summary>
        /// Los correos que llegan cada cierto tiempo (la liquidación de cada mes, la cartola de
        /// la AFP), solo a quien cumple su condición esa semana.
        /// </summary>
        public static void DeliverPeriodic(World world, ContentDatabase content)
        {
            IReadOnlyList<MailDefinition> due = content.PeriodicMails.Where(m => m.IsDueOn(world.Date)).ToList();
            if (due.Count == 0)
            {
                return;
            }

            foreach (Character character in world.Characters.Where(c => c.Mail != null && c.IsAlive).OrderBy(c => c.Id))
            {
                foreach (MailDefinition definition in due)
                {
                    if (Applies(world, content, character, definition))
                    {
                        character.Mail!.Receive(world.Date, definition.Id);
                    }
                }
            }
        }

        /// <summary>Si el correo le corresponde a esta persona ahora (sin condición, siempre).</summary>
        private static bool Applies(World world, ContentDatabase content, Character character, MailDefinition definition) =>
            definition.CompiledConditions == null
            || definition.CompiledConditions.Evaluate(new EventScope(world, content, character, null, readOnly: true)).AsBool(definition.Id);

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
                        if (!Applies(world, content, character, definition))
                        {
                            continue;
                        }

                        mail.Receive(entry.Date, definition.Id);
                    }
                }

                mail.JournalCursor = journal.Count;
            }
        }
    }
}
