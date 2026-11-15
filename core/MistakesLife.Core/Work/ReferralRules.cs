using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// Recomendar a alguien en tu pega (jugador.recomendar): el "pituto". La probabilidad de que
    /// lo contraten es la misma que tiene en el país una entrevista que llega por un contacto
    /// (economia.busqueda_trabajo: exito_entrevista + exito_extra de la forma que necesita
    /// contacto). Si entra, queda en tu equipo y se recuerda que lo recomendaste tú.
    /// </summary>
    public static class ReferralRules
    {
        public const string HiredJournal = "diario.pega.recomendado_entra";
        public const string NotHiredJournal = "diario.pega.recomendado_no";

        /// <summary>Las líneas del diario que escribe una recomendación.</summary>
        public static readonly string[] Journals = { HiredJournal, NotHiredJournal };

        /// <summary>Probabilidad de que contraten a quien recomiendas, en ese país (0 si no hay búsqueda por contactos).</summary>
        public static double HireChance(JobSearchDefinition? search)
        {
            JobSearchMethodDefinition? contacts = search?.Methods.FirstOrDefault(m => m.NeedsContact);
            if (search == null || contacts == null)
            {
                return 0;
            }

            return Math.Max(0.05, Math.Min(0.95, search.InterviewSuccess + contacts.ExtraSuccess));
        }

        /// <summary>Recomienda a esa persona en el equipo del jugador. Devuelve si la contrataron.</summary>
        public static bool Recommend(World world, ContentDatabase content, Character player, Character person)
        {
            WorkTeam team = player.Team ?? throw new InvalidOperationException("No tiene equipo.");
            JobSearchDefinition? search = player.Residence == null ? null : content.GetCountry(player.Residence.CountryId).Economy?.JobSearch;
            GameRandom random = world.Random.Fork("recomendar." + person.Id + "." + world.Date.TotalWeeks);
            bool hired = random.Chance(HireChance(search));
            if (hired)
            {
                team.ColleagueIds.Add(person.Id);
                team.RecommendedIds.Add(person.Id);
            }

            world.WriteJournal(player.Id, hired ? HiredJournal : NotHiredJournal);
            return hired;
        }
    }
}
