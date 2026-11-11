namespace MistakesLife.Core.Saving
{
    /// <summary>Una noticia del mundo guardada (desde la versión 10).</summary>
    internal sealed class NewsData
    {
        public int Week { get; set; }

        public string NewsId { get; set; } = string.Empty;

        public string CountryId { get; set; } = string.Empty;

        public string? CityId { get; set; }

        public string TextKey { get; set; } = string.Empty;
    }
}
