namespace MistakesLife.Core.Presentation
{
    /// <summary>En qué momento está la partida: le dice a la pantalla qué mostrar.</summary>
    public enum LifePhase
    {
        /// <summary>El tiempo puede avanzar: se muestra el botón para seguir viviendo.</summary>
        Living,

        /// <summary>Hay un evento esperando una decisión: se muestran sus opciones.</summary>
        Choosing,

        /// <summary>El jugador murió y tiene hijos vivos: se elige con quién seguir.</summary>
        ChoosingHeir,

        /// <summary>El jugador murió sin hijos vivos: la partida terminó.</summary>
        Over,
    }
}
