using MistakesLife.Core.Time;

namespace MistakesLife.Core.Worlds
{
    /// <summary>Una noticia que pasó en un país (o en una de sus ciudades), con el titular que se usó.</summary>
    public sealed class NewsRecord
    {
        public NewsRecord(GameDate date, string newsId, string countryId, string? cityId, string textKey)
        {
            Date = date;
            NewsId = newsId;
            CountryId = countryId;
            CityId = cityId;
            TextKey = textKey;
        }

        public GameDate Date { get; }

        /// <summary>Id de la noticia, por ejemplo "noticia.chile.terremoto".</summary>
        public string NewsId { get; }

        public string CountryId { get; }

        /// <summary>La ciudad, si la noticia es solo de una ciudad (un temporal en Santiago); null si es del país.</summary>
        public string? CityId { get; }

        /// <summary>Clave del titular que salió (la noticia o una de sus variantes).</summary>
        public string TextKey { get; }
    }
}
