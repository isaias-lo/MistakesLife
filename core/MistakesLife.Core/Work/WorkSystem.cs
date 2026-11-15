using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Fase de sistemas, después de la búsqueda de trabajo: el trabajo del jugador por dentro.
    /// <list type="bullet">
    /// <item>Al entrar a un trabajo (con contrato o informal) arma su equipo: jefe o jefa y
    /// colegas con nombre ("diario.pega.equipo_nuevo"). A honorarios no hay equipo.</item>
    /// <item>Al dejar el trabajo el equipo se deshace; quienes estaban quedan como colegas.</item>
    /// <item>Cada año alguien se va, a veces llega alguien y a veces cambia el jefe.</item>
    /// <item>Cada mes, el cariño sube un poco con quienes ya viviste algo, y los colegas que se
    /// quieren de verdad se vuelven amigos.</item>
    /// </list>
    /// Va antes de los eventos, para que un momento "con jefe" ya tenga a quién.
    /// </summary>
    public sealed class WorkSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.pega";

        private readonly ContentDatabase _content;

        public WorkSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Residence == null)
            {
                return;
            }

            if (!WorkRules.HasTeamJob(player))
            {
                player.Team = null;
                return;
            }

            GameRandom random = context.RandomFor(SystemId);
            int months = player.Job!.MonthsInJob;
            if (player.Team == null || months < player.Team.MonthsInJob)
            {
                player.Team = WorkRules.Form(world, _content, player, random);
                world.WriteJournal(player.Id, "diario.pega.equipo_nuevo");
                return;
            }

            bool newMonth = months > player.Team.MonthsInJob;
            player.Team.MonthsInJob = months;
            if (!newMonth)
            {
                return;
            }

            if (months % 12 == 0)
            {
                foreach (string line in WorkRules.Year(world, _content, player, random))
                {
                    world.WriteJournal(player.Id, line);
                }
            }

            WorkRules.ShareMonth(world, player);
            if (WorkRules.NewFriendships(world, player).Count > 0)
            {
                world.WriteJournal(player.Id, "diario.pega.amistad");
            }
        }
    }
}
