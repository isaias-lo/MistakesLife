using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una causa del jugador tal como se ve en la carpeta del juzgado: RIT, tribunal,
    /// de qué se trata, desde cuándo y la línea del proceso con sus plazos.
    /// </summary>
    public sealed class CourtCaseView
    {
        public CourtCaseView(string caseId, string rit, string name, string court, string openedLabel, bool isOpen, string? outcome,
            IReadOnlyList<CourtStageView> stages, int weeksInStage)
        {
            CaseId = caseId;
            Rit = rit;
            Name = name;
            Court = court;
            OpenedLabel = openedLabel;
            IsOpen = isOpen;
            Outcome = outcome;
            Stages = stages;
            WeeksInStage = weeksInStage;
        }

        public string CaseId { get; }

        /// <summary>El RIT o rol de la causa, por ejemplo "C-1532-81".</summary>
        public string Rit { get; }

        /// <summary>De qué se trata, en el idioma del jugador ("Cobro de deuda").</summary>
        public string Name { get; }

        /// <summary>El tribunal ("Juzgado Civil").</summary>
        public string Court { get; }

        /// <summary>Cuándo entró la causa.</summary>
        public string OpenedLabel { get; }

        public bool IsOpen { get; }

        /// <summary>Cómo terminó, en el idioma del jugador, o null si sigue abierta.</summary>
        public string? Outcome { get; }

        public IReadOnlyList<CourtStageView> Stages { get; }

        /// <summary>Semanas que lleva en la etapa actual (0 si terminó).</summary>
        public int WeeksInStage { get; }
    }
}
