using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;

namespace MistakesLife.Core.Worlds
{
    /// <summary>
    /// Las noticias del mundo (fase de sistemas): cada semana, cada noticia del país
    /// donde vive el jugador tiene su propia oportunidad según su probabilidad anual.
    /// Una noticia le pasa al país, no al jugador: solo cambia lo del país (el ciclo
    /// económico, la inflación, una ley) y queda en el diario de noticias. Lo que le
    /// pase al jugador lo deciden los eventos que la leen con noticia_reciente(),
    /// según lo que él eligió antes (si armó la mochila de emergencia, dónde vive...).
    /// </summary>
    public sealed class NewsSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.noticias";

        public NewsSystem(ContentDatabase content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public ContentDatabase Content { get; }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Systems;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            if (!player.IsAlive || player.Residence == null)
            {
                return;
            }

            string countryId = player.Residence.CountryId;
            string cityId = player.Residence.CityId;
            GameRandom random = context.RandomFor(Id);
            var readScope = new EventScope(world, Content, player, null, readOnly: true);
            foreach (NewsDefinition news in Content.News)
            {
                // El azar se pide siempre, pase o no la noticia: así agregar una noticia
                // de otro país no cambia las de este.
                bool lucky = random.Chance(news.WeeklyProbability);
                if (!lucky || !Applies(news, countryId, cityId) || InCooldown(world, news, countryId, cityId))
                {
                    continue;
                }

                if (news.CompiledConditions != null && !news.CompiledConditions.IsTrue(readScope))
                {
                    continue;
                }

                var scope = new EventScope(world, Content, player, null, readOnly: false);
                foreach (Effect effect in news.CompiledEffects)
                {
                    effect.Apply(scope);
                }

                string? newsCity = news.Cities.Count > 0 ? cityId : null;
                world.AddNews(new NewsRecord(world.Date, news.Id, countryId, newsCity, news.TextKeyFor(world.TimesNews(news.Id, countryId))));
            }
        }

        private static bool Applies(NewsDefinition news, string countryId, string cityId) =>
            (news.CountryId == null || news.CountryId == countryId)
            && (news.Cities.Count == 0 || news.Cities.Contains(cityId));

        private static bool InCooldown(World world, NewsDefinition news, string countryId, string cityId)
        {
            if (news.CooldownWeeks <= 0)
            {
                return false;
            }

            NewsRecord? last = world.LastNews(news.Id, countryId, cityId);
            return last != null && world.Date.WeeksSince(last.Date) < news.CooldownWeeks;
        }
    }
}
