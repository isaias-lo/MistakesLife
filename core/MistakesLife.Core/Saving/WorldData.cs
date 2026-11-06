using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>El mundo guardado: fecha, azar, personajes, semillas, economía, leyes, diario y eventos.</summary>
    internal sealed class WorldData
    {
        public ulong Seed { get; set; }

        /// <summary>Estado del generador de azar de la partida.</summary>
        public ulong RandomState { get; set; }

        /// <summary>Fecha en semanas desde el inicio del mundo.</summary>
        public int Week { get; set; }

        public int PlayerId { get; set; }

        public int NextCharacterId { get; set; }

        public List<int> FormerPlayers { get; set; } = new List<int>();

        /// <summary>Personajes en el orden en que se crearon (el orden importa para repetir la vida exacta).</summary>
        public List<CharacterData> Characters { get; set; } = new List<CharacterData>();

        public int NextSeedId { get; set; }

        /// <summary>Semanas en que despertó alguna semilla (para el límite por mes).</summary>
        public List<int> SeedAwakenings { get; set; } = new List<int>();

        public List<SeedData> Seeds { get; set; } = new List<SeedData>();

        public List<EconomyData> Economies { get; set; } = new List<EconomyData>();

        public List<LawChangeData> LawChanges { get; set; } = new List<LawChangeData>();

        public List<JournalData> Journal { get; set; } = new List<JournalData>();

        public List<PendingEventData> PendingEvents { get; set; } = new List<PendingEventData>();

        public List<EventRecordData> EventHistory { get; set; } = new List<EventRecordData>();

        /// <summary>Las empresas que conoce el mundo (desde la versión 6).</summary>
        public List<CompanyData> Companies { get; set; } = new List<CompanyData>();

        public int NextCompanyId { get; set; } = 1;
    }
}
