using System;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Game
{
    /// <summary>
    /// Una foto del mundo a la que se puede volver con
    /// <see cref="LifeSession.GoBack(int)"/>. Guarda la partida completa de ese
    /// momento (comprimida), así que volver es como cargar esa partida.
    /// </summary>
    public sealed class WorldSnapshot
    {
        internal WorldSnapshot(int id, SnapshotKind kind, GameDate date, int playerId, int playerAgeInYears, string stageId, string? eventId, byte[] data)
        {
            Id = id;
            Kind = kind;
            Date = date;
            PlayerId = playerId;
            PlayerAgeInYears = playerAgeInYears;
            StageId = stageId;
            EventId = eventId;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>Número de la foto, único dentro de la partida.</summary>
        public int Id { get; }

        public SnapshotKind Kind { get; }

        /// <summary>Fecha del mundo en la foto.</summary>
        public GameDate Date { get; }

        /// <summary>El personaje que se jugaba cuando se sacó.</summary>
        public int PlayerId { get; }

        public int PlayerAgeInYears { get; }

        /// <summary>Etapa de la vida del jugador en la foto (id de <see cref="LifeStages"/>, también clave de texto).</summary>
        public string StageId { get; }

        /// <summary>El evento de día clave que esperaba respuesta, si la foto es de antes de una decisión.</summary>
        public string? EventId { get; }

        /// <summary>La partida de ese momento, comprimida (sin la máquina del tiempo).</summary>
        internal byte[] Data { get; }
    }
}
