namespace MistakesLife.Core.Saving
{
    /// <summary>Una foto del mundo guardada: sus datos y la partida de ese momento, comprimida.</summary>
    internal sealed class SnapshotData
    {
        public int Id { get; set; }

        /// <summary>"inicio_etapa" o "antes_de_decidir".</summary>
        public string Kind { get; set; } = string.Empty;

        public int Week { get; set; }

        public int PlayerId { get; set; }

        public int PlayerAge { get; set; }

        public string StageId { get; set; } = string.Empty;

        public string? EventId { get; set; }

        /// <summary>La partida de ese momento, comprimida (en el texto va en base64).</summary>
        public byte[] Data { get; set; } = new byte[0];
    }
}
