using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Events;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Game
{
    /// <summary>
    /// Volver al pasado: guarda fotos del mundo (al empezar cada etapa de la vida
    /// y antes de cada decisión de día clave) y lleva la cuenta de las cargas. Vive
    /// en <see cref="LifeSession"/>, fuera del mundo, para que no se borre al volver.
    /// Reglas: cada vuelta gasta una carga, y cada etapa de la vida se puede volver
    /// a vivir una sola vez por generación, para que las decisiones sigan pesando.
    /// </summary>
    public sealed class TimeMachine
    {
        /// <summary>Cargas con que empieza una partida nueva. Las demás se compran (<see cref="AddCharges"/>).</summary>
        public const int StartingCharges = 1;

        /// <summary>Cuántas fotos de antes de una decisión se guardan (las más recientes).</summary>
        public const int MaxDecisionSnapshots = 3;

        private readonly List<WorldSnapshot> _snapshots = new List<WorldSnapshot>();
        private readonly HashSet<string> _usedStages = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Intuition> _intuitions = new List<Intuition>();

        internal TimeMachine(int charges)
        {
            Charges = charges;
        }

        /// <summary>
        /// Si está apagada no saca fotos (el simulador de vidas la apaga para ir
        /// rápido). Las fotos que ya tiene se pueden seguir usando.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Cuántas veces más se puede volver al pasado.</summary>
        public int Charges { get; private set; }

        /// <summary>Las fotos de la generación actual, de la más antigua a la más nueva.</summary>
        public IReadOnlyList<WorldSnapshot> Snapshots => _snapshots;

        /// <summary>Etapas de la vida que ya se volvieron a vivir en esta generación.</summary>
        public IReadOnlyCollection<string> UsedStages => _usedStages;

        internal int NextSnapshotId { get; set; } = 1;

        internal IReadOnlyList<Intuition> Intuitions => _intuitions;

        /// <summary>Suma cargas (por ejemplo, al comprarlas).</summary>
        public void AddCharges(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Hay que sumar al menos una carga.");
            }

            Charges += count;
        }

        /// <summary>Si ahora se puede volver a esa foto: quedan cargas y su etapa no se volvió a vivir.</summary>
        public bool CanGoBackTo(WorldSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return Charges > 0 && _snapshots.Contains(snapshot) && !_usedStages.Contains(snapshot.StageId);
        }

        /// <summary>
        /// La intuición: si el jugador ya vivió este mismo evento antes de volver
        /// al pasado, la opción que eligió esa vez (para que la pantalla la marque).
        /// Null si es la primera vez.
        /// </summary>
        public int? PreviousChoice(PendingEvent pending)
        {
            if (pending == null)
            {
                throw new ArgumentNullException(nameof(pending));
            }

            Intuition? intuition = _intuitions.LastOrDefault(i =>
                i.EventId == pending.EventId && i.CharacterId == pending.CharacterId && i.Date == pending.Date);
            return intuition?.Option;
        }

        internal bool HasStageSnapshot(int playerId, string stageId) =>
            _snapshots.Any(s => s.Kind == SnapshotKind.StageStart && s.PlayerId == playerId && s.StageId == stageId);

        /// <summary>Si ya hay foto de este evento esperando respuesta, sacada en la fecha actual del mundo.</summary>
        internal bool HasDecisionSnapshot(PendingEvent pending, GameDate now) =>
            _snapshots.Any(s => s.Kind == SnapshotKind.BeforeDecision && s.PlayerId == pending.CharacterId
                && s.EventId == pending.EventId && s.Date == now);

        internal void Add(WorldSnapshot snapshot)
        {
            _snapshots.Add(snapshot);
            NextSnapshotId = Math.Max(NextSnapshotId, snapshot.Id + 1);
            var decisions = _snapshots.Where(s => s.Kind == SnapshotKind.BeforeDecision).ToList();
            foreach (WorldSnapshot old in decisions.Take(Math.Max(0, decisions.Count - MaxDecisionSnapshots)))
            {
                _snapshots.Remove(old);
            }
        }

        /// <summary>
        /// Gasta una carga para volver a la foto: marca su etapa como vivida de
        /// nuevo y borra las fotos posteriores (ese futuro ya no pasó).
        /// </summary>
        internal void Spend(WorldSnapshot snapshot)
        {
            if (!CanGoBackTo(snapshot))
            {
                throw new InvalidOperationException(Charges <= 0
                    ? "No quedan cargas para volver al pasado."
                    : "Esa etapa de la vida ya se volvió a vivir, o la foto ya no existe.");
            }

            Charges--;
            _usedStages.Add(snapshot.StageId);
            _snapshots.RemoveAll(s => s.Date > snapshot.Date);
        }

        internal void RememberChoice(string eventId, int characterId, GameDate date, int option) =>
            _intuitions.Add(new Intuition(eventId, characterId, date, option));

        internal void SetCharges(int charges) => Charges = charges;

        internal void MarkStageUsed(string stageId) => _usedStages.Add(stageId);

        /// <summary>Legado: la nueva generación empieza sin fotos ni etapas usadas. Las cargas se conservan.</summary>
        internal void StartNewGeneration()
        {
            _snapshots.Clear();
            _usedStages.Clear();
            _intuitions.Clear();
        }

        /// <summary>Lo que el jugador eligió en un evento antes de volver al pasado.</summary>
        internal sealed class Intuition
        {
            public Intuition(string eventId, int characterId, GameDate date, int option)
            {
                EventId = eventId;
                CharacterId = characterId;
                Date = date;
                Option = option;
            }

            public string EventId { get; }

            public int CharacterId { get; }

            public GameDate Date { get; }

            public int Option { get; }
        }
    }
}
