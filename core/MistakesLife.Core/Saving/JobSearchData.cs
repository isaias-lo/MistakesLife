namespace MistakesLife.Core.Saving
{
    /// <summary>Una búsqueda de trabajo guardada (desde la versión 9).</summary>
    internal sealed class JobSearchData
    {
        public string Target { get; set; } = string.Empty;

        public string Kind { get; set; } = "Employee";

        public string? MethodId { get; set; }

        public int? ContactId { get; set; }

        public string Stage { get; set; } = string.Empty;

        public int Weeks { get; set; }

        public int Applications { get; set; }

        public int Interviews { get; set; }

        public int Rejections { get; set; }

        public bool Negotiated { get; set; }

        public double Raise { get; set; }
    }
}
