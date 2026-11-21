using System;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de personajes: la gente interesante de tu horario (D4b2). Cada semana mira si
    /// aparece alguien nuevo que podrías conocer en tu pega o en tu carrera; las reglas están en
    /// <see cref="InterestingPeopleRules"/>.
    /// </summary>
    public sealed class InterestingPeopleSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.gente_interesante";

        private readonly ContentDatabase _content;

        public InterestingPeopleSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Characters;

        public void Tick(World world, WeekContext context) =>
            InterestingPeopleRules.Week(world, _content, world.Player, context.RandomFor(SystemId));
    }
}
