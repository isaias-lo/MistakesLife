using MistakesLife.Core.Time;

namespace MistakesLife.Core.Courts
{
    /// <summary>
    /// Una causa judicial del jugador: su RIT, en qué etapa va desde cuándo, quién es
    /// la otra parte (si es una persona) y, al terminar, su resultado. La mueve el
    /// sistema de juzgados (los plazos) y los eventos (lo que se decide).
    /// </summary>
    public sealed class CourtCase
    {
        public CourtCase(int id, string typeId, string rit, int ownerId, int? otherPartyId, GameDate opened)
        {
            Id = id;
            TypeId = typeId;
            Rit = rit;
            OwnerId = ownerId;
            OtherPartyId = otherPartyId;
            Opened = opened;
            StageStarted = opened;
        }

        public int Id { get; }

        /// <summary>El tipo de causa del paquete, por ejemplo "causa.chile.divorcio".</summary>
        public string TypeId { get; }

        /// <summary>El RIT o rol con que la conoce el tribunal, por ejemplo "C-1532-2047".</summary>
        public string Rit { get; }

        /// <summary>De quién es la causa (el jugador).</summary>
        public int OwnerId { get; }

        /// <summary>La otra parte, si es una persona (la expareja en un divorcio), o null (un banco).</summary>
        public int? OtherPartyId { get; }

        public GameDate Opened { get; }

        /// <summary>Posición de la etapa actual en la definición.</summary>
        public int StageIndex { get; internal set; }

        /// <summary>Desde cuándo está en la etapa actual.</summary>
        public GameDate StageStarted { get; internal set; }

        /// <summary>Cuándo terminó, o null si sigue abierta.</summary>
        public GameDate? Closed { get; internal set; }

        /// <summary>Cómo terminó ("pago", "acogida", "rechazada"...), o null.</summary>
        public string? Outcome { get; internal set; }

        /// <summary>
        /// Un monto que fija la causa (lo que se debe pagar, la pensión de alimentos), en
        /// dinero del inicio del mundo: los eventos lo leen a precios de hoy, así que se
        /// reajusta solo cuando suben los precios. 0 si no hay.
        /// </summary>
        public decimal Amount { get; set; }

        public bool IsOpen => Closed == null;

        internal void MoveTo(int stageIndex, GameDate date)
        {
            StageIndex = stageIndex;
            StageStarted = date;
        }

        internal void Close(string outcome, GameDate date)
        {
            Outcome = outcome;
            Closed = date;
        }

        /// <summary>Para leer un guardado: deja la causa como estaba.</summary>
        internal void Restore(int stageIndex, GameDate stageStarted, GameDate? closed, string? outcome, decimal amount)
        {
            StageIndex = stageIndex;
            StageStarted = stageStarted;
            Closed = closed;
            Outcome = outcome;
            Amount = amount;
        }
    }
}
