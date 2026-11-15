using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Fase de decisiones, antes del horario: lleva el curso del jugador.
    /// <list type="bullet">
    /// <item>Al empezar las clases de una etapa con curso (básica, media) arma su curso:
    /// compañeros con nombre y profe jefe ("diario.curso.nuevo").</item>
    /// <item>Cada año, el primer día de clases, pasa de curso (ver <see cref="SchoolRules.Advance"/>).</item>
    /// <item>Cada mes, los compañeros que se tienen cariño de verdad se vuelven amigos ("diario.curso.amistad").</item>
    /// <item>Al terminar el colegio, el curso se cierra; los compañeros siguen en el mundo.</item>
    /// </list>
    /// Va antes del horario para que el horario del año ya conozca a tus compañeros.
    /// </summary>
    public sealed class SchoolSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.colegio";

        private readonly ContentDatabase _content;

        public SchoolSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Decisions;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Residence == null)
            {
                return;
            }

            EducationStageDefinition? stage = SchoolRules.ClassStage(world, _content, player);
            if (stage == null)
            {
                player.SchoolClass = null;
                return;
            }

            CountryDefinition country = _content.GetCountry(player.Residence.CountryId);
            if (!country.IsSchoolTime(context.Date))
            {
                return;
            }

            int year = SchoolRules.SchoolYearIndex(country, context.Date) ?? context.Date.Year;
            SchoolClass? cls = player.SchoolClass;
            if (cls == null || cls.StageId != stage.Id)
            {
                player.SchoolClass = SchoolRules.Form(world, _content, player, stage, year, context.RandomFor(SystemId));
                world.WriteJournal(player.Id, "diario.curso.nuevo");
                return;
            }

            if (cls.SchoolYear != year)
            {
                foreach (string line in SchoolRules.Advance(world, _content, player, year, context.RandomFor(SystemId)))
                {
                    world.WriteJournal(player.Id, line);
                }
            }

            if (context.Date.IsFirstWeekOfMonth && SchoolRules.NewFriendships(world, player).Count > 0)
            {
                world.WriteJournal(player.Id, "diario.curso.amistad");
            }
        }
    }
}
