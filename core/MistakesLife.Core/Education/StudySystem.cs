using System;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Fase de sistemas: el dominio de cada ramo del jugador (D5a). Al empezar el año escolar
    /// cada ramo parte desde su base; cada semana de clases las clases lo acercan a la base y lo
    /// que no se estudia se va olvidando (<see cref="StudyRules.Week"/>). Va antes de las notas,
    /// que ya usan el dominio de la semana.
    /// </summary>
    public sealed class StudySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.estudio";

        private readonly ContentDatabase _content;

        public StudySystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            if (world.Player.IsAlive)
            {
                StudyRules.Week(world, _content, world.Player);
            }
        }
    }
}
