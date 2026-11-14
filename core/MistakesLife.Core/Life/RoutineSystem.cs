using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de decisiones: aplica el horario del jugador. Es lo primero de cada semana,
    /// porque es lo que el jugador eligió.
    /// <list type="bullet">
    /// <item>Al llegar a los 6 años y cada vez que cambia de etapa (colegio, estudios,
    /// trabajo) le sugiere un horario según su forma de ser y, si hay alguien jugando
    /// (<see cref="StopsForNewRoutine"/>), detiene el tiempo para que lo arme ("dia_clave.horario").</item>
    /// <item>Cada semana el ánimo se acerca un punto al ánimo que deja el horario, así no
    /// se queda pegado en 0 ni en 100.</item>
    /// <item>Cada mes la mente se acerca un punto a la que deja el horario, la salud sube
    /// o baja un punto si el horario la cuida o la descuida harto, y el cariño con la
    /// gente con quien pasas tiempo crece.</item>
    /// </list>
    /// </summary>
    public sealed class RoutineSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.horario";

        /// <summary>Razón con que se detiene el tiempo para armar el horario de una etapa nueva.</summary>
        public const string StopReasonNewRoutine = "dia_clave.horario";

        private readonly ContentDatabase _content;

        public RoutineSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>
        /// Si al sugerir un horario nuevo detiene el tiempo para que el jugador lo arme.
        /// Lo encienden las pantallas (la consola y Unity); el simulador y las pruebas que
        /// solo hacen pasar los años lo dejan apagado y viven con el horario sugerido.
        /// </summary>
        public bool StopsForNewRoutine { get; set; }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Decisions;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.AgeInYears(context.Date) < RoutineRules.MinAge)
            {
                return;
            }

            string stage = RoutineRules.StageKey(world, _content, player);
            if (player.Routine == null || player.Routine.StageKey != stage)
            {
                player.Routine = RoutineRules.Suggest(world, _content, player, context.RandomFor(SystemId));
                if (StopsForNewRoutine)
                {
                    context.RequestStop(StopReasonNewRoutine);
                }
            }

            Apply(world, player, player.Routine, context);
        }

        private void Apply(World world, Character player, Routine routine, WeekContext context)
        {
            int target = RoutineRules.MoodTarget(RoutineRules.Total(world, _content, player, routine, "animo"));
            player.Stats.Happiness += Math.Sign(target - player.Stats.Happiness);

            if (!context.Date.IsFirstWeekOfMonth)
            {
                return;
            }

            int mind = RoutineRules.MindTarget(RoutineRules.Total(world, _content, player, routine, "mente"));
            player.Stats.Smarts += Math.Sign(mind - player.Stats.Smarts);
            player.Stats.Health += RoutineRules.HealthStep(RoutineRules.Total(world, _content, player, routine, "salud"));

            foreach (string? id in routine.Slots)
            {
                ActivityDefinition? activity = id == null ? null : _content.Activity(id);
                if (activity?.With == null || activity.Contribution("carino") == 0 || !RoutineRules.IsAvailable(world, _content, player, activity))
                {
                    continue;
                }

                foreach (Character other in ChatSystem.PeopleAs(world, player, activity.With))
                {
                    RelationshipRules.Edit(world, player, other).Affection += activity.Contribution("carino");
                    RelationshipRules.Edit(world, other, player).Affection += activity.Contribution("carino");
                }
            }
        }
    }
}
