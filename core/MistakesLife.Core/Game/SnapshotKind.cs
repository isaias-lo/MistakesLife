namespace MistakesLife.Core.Game
{
    /// <summary>Por qué se sacó una foto del mundo.</summary>
    public enum SnapshotKind
    {
        /// <summary>El jugador empezó una etapa de la vida (ver <see cref="LifeStages"/>).</summary>
        StageStart,

        /// <summary>Justo antes de una decisión de día clave, con el evento esperando respuesta.</summary>
        BeforeDecision,
    }
}
