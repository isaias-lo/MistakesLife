using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Education
{
    /// <summary>
    /// Fase de sistemas, justo después de <see cref="CareerSystem"/>: la universidad, el CFT
    /// o el instituto del jugador por dentro.
    /// <list type="bullet">
    /// <item>Al empezar a estudiar arma su sección: compañeros con nombre y el profe del
    /// semestre ("diario.seccion.nueva").</item>
    /// <item>Al terminar cada semestre le pone nota a cada ramo del plan. Si queda algún
    /// ramo rojo, ese semestre no cuenta: lo vuelve a cursar ("diario.estudios.ramo_reprobado"),
    /// y la carrera dura más que lo formal, como en la vida real.</item>
    /// <item>Después pasa al semestre siguiente: profe nuevo, y alguien de la sección a veces no sigue.</item>
    /// <item>Cada mes, los compañeros que se tienen cariño de verdad se vuelven amigos.</item>
    /// </list>
    /// Va después de <see cref="CareerSystem"/> y antes de los eventos, para que un evento
    /// de titulación ya vea el semestre corregido.
    /// </summary>
    public sealed class UniversitySystem : IWeeklySystem
    {
        public const string SystemId = "sistema.universidad";

        private readonly ContentDatabase _content;

        public UniversitySystem(ContentDatabase content)
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

            CareerStudies? studies = player.Studies;
            if (studies == null)
            {
                if (UniversityRules.IsSection(_content, player.SchoolClass))
                {
                    player.SchoolClass = null;
                }

                return;
            }

            GameRandom random = context.RandomFor(SystemId);
            if (!UniversityRules.InSection(player))
            {
                studies.GradedSemesters = Math.Min(studies.GradedSemesters, studies.CompletedSemesters);
                player.SchoolClass = UniversityRules.Form(world, _content, player, random);
                world.WriteJournal(player.Id, "diario.seccion.nueva");
                return;
            }

            if (studies.GradedSemesters > studies.CompletedSemesters)
            {
                // El contenido le restó un semestre (botó un ramo): no hay notas que poner.
                studies.GradedSemesters = studies.CompletedSemesters;
            }

            GradeScaleDefinition? scale = _content.GetCountry(player.Residence.CountryId).GradeScale;
            if (studies.CompletedSemesters > studies.GradedSemesters && scale != null)
            {
                CloseSemester(world, player, studies, scale, random);
                return;
            }

            if (context.Date.IsFirstWeekOfMonth && SchoolRules.NewFriendships(world, player).Count > 0)
            {
                world.WriteJournal(player.Id, "diario.curso.amistad");
            }
        }

        private void CloseSemester(World world, Character player, CareerStudies studies, GradeScaleDefinition scale, GameRandom random)
        {
            int semester = studies.CompletedSemesters;
            SortedDictionary<string, double> marks = UniversityRules.SemesterMarks(world, _content, player, semester, scale, random);
            studies.SemestersTaken++;
            studies.LastMarks.Clear();
            studies.LastMarksSemester = semester;
            foreach (KeyValuePair<string, double> mark in marks)
            {
                studies.LastMarks[mark.Key] = mark.Value;
            }

            if (marks.Count > 0)
            {
                studies.SemesterAverages.Add(GradeRules.Round(marks.Values.Average(), scale));
            }

            if (marks.Values.Any(m => m < scale.Pass))
            {
                studies.CompletedSemesters--;
                player.Stats.Happiness -= 3;
                world.WriteJournal(player.Id, "diario.estudios.ramo_reprobado");
            }
            else
            {
                world.WriteJournal(player.Id, "diario.seccion.semestre_aprobado");
            }

            studies.GradedSemesters = studies.CompletedSemesters;
            studies.Preparation = 0;
            foreach (string line in UniversityRules.Advance(world, _content, player, random))
            {
                world.WriteJournal(player.Id, line);
            }
        }
    }
}
