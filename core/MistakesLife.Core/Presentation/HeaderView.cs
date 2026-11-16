namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// La cabecera de la pantalla principal: el mes en grande, el día, y en qué estás y dónde.
    /// Siempre a la vista, para que el jugador nunca se pierda en el tiempo (docs/diseno-juego.md).
    /// </summary>
    public sealed class HeaderView
    {
        public HeaderView(string month, string day, string role, string commune)
        {
            Month = month;
            Day = day;
            Role = role;
            Commune = commune;
        }

        /// <summary>El mes, con mayúscula ("Marzo").</summary>
        public string Month { get; }

        /// <summary>El día de esta semana ("lunes 14").</summary>
        public string Day { get; }

        /// <summary>Tu curso ("8º básico B"), tu carrera o tu pega; vacío si no estás en nada.</summary>
        public string Role { get; }

        /// <summary>La comuna donde vives ("Puente Alto"); vacío si aún no vives en ninguna.</summary>
        public string Commune { get; }
    }
}
