using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Life;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Game
{
    /// <summary>
    /// Una partida lista para jugar: arma el mundo y el ciclo semanal con todos
    /// los sistemas, en el orden correcto. La usan la consola, el simulador de
    /// vidas y, más adelante, Unity. Ninguno de ellos debe armar el ciclo a mano.
    /// </summary>
    public sealed class LifeSession
    {
        private readonly WeekCycle _cycle;
        private int _journalRead;

        public LifeSession(ContentDatabase content, ulong seed)
            : this(content, seed, new SeedRules(), new EventRules())
        {
        }

        public LifeSession(ContentDatabase content, ulong seed, SeedRules seedRules, EventRules eventRules)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            World = WorldFactory.CreateNewLife(seed, content);
            Events = new EventSystem(content, eventRules);
            _cycle = new WeekCycle()
                .Register(new EducationSystem(content))
                .Register(new CareerSystem())
                .Register(new AgingSystem())
                .Register(new MortalitySystem(content))
                .Register(new SeedSystem(seedRules))
                .Register(Events)
                .Register(new BirthdaySystem());
        }

        public ContentDatabase Content { get; }

        public World World { get; }

        public EventSystem Events { get; }

        /// <summary>El evento que espera la decisión del jugador, si lo hay.</summary>
        public PendingEvent? Pending => World.PendingEvents.FirstOrDefault(p => p.CharacterId == World.PlayerId);

        public int PlayerAgeInYears => World.Player.AgeInYears(World.Date);

        /// <summary>
        /// Avanza hasta <paramref name="maxWeeks"/> semanas; se detiene antes si
        /// llega un evento o un día clave. Devuelve las semanas que pasaron.
        /// </summary>
        public int Advance(int maxWeeks)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Primero hay que responder el evento pendiente.");
            }

            return _cycle.AdvanceWeeks(World, maxWeeks);
        }

        /// <summary>Responde el evento pendiente con una de sus opciones disponibles.</summary>
        public void Choose(int optionIndex)
        {
            PendingEvent pending = Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            Events.Choose(World, pending, optionIndex);
        }

        /// <summary>Entradas nuevas del diario del jugador desde la última vez que se pidieron.</summary>
        public IReadOnlyList<JournalEntry> TakeNewJournalEntries()
        {
            var entries = World.Journal.Skip(_journalRead).Where(e => e.CharacterId == World.PlayerId).ToList();
            _journalRead = World.Journal.Count;
            return entries;
        }

        /// <summary>Texto para el jugador en el idioma dado.</summary>
        public string Text(string key, string language = ContentDatabase.BaseLanguage) => Content.Text(key, language);
    }
}
