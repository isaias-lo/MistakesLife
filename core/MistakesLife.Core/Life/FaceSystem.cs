using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de sistemas: le arma la cara a cada persona nueva del mundo (un hijo que nace, una
    /// pareja, un compañero), para que Unity siempre tenga qué dibujar. Ver <see cref="FaceRules"/>.
    /// No usa el azar de la semana: cada cara sale de su propio azar.
    /// </summary>
    public sealed class FaceSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.rostro";

        private readonly ContentDatabase _content;

        public FaceSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            foreach (Character person in world.Characters)
            {
                if (person.Face == null && person.IsAlive)
                {
                    FaceRules.Ensure(world, _content, person);
                }
            }
        }
    }
}
