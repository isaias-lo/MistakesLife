namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// La cabecera de la pantalla principal (D6-1a, acordada con Isaias el 22-11-2026): a la
    /// izquierda tu cara, tu nombre y tu edad; en la esquina derecha tu plata y lo que ganas al mes;
    /// abajo una tira con la fecha corta y el año del mundo. El curso, la pega y la comuna van en tu
    /// ficha (<see cref="ProfileView"/>).
    /// </summary>
    public sealed class HeaderView
    {
        public HeaderView(string month, string day, string role, string commune)
            : this(month, day, role, commune, string.Empty, string.Empty, string.Empty, false, false, string.Empty)
        {
        }

        public HeaderView(
            string month,
            string day,
            string role,
            string commune,
            string date,
            string year,
            string money,
            bool belowZero,
            bool cash,
            string income)
        {
            Month = month;
            Day = day;
            Role = role;
            Commune = commune;
            Date = date;
            Year = year;
            Money = money;
            BelowZero = belowZero;
            Cash = cash;
            Income = income;
        }

        /// <summary>El mes, con mayúscula ("Marzo").</summary>
        public string Month { get; }

        /// <summary>El día de esta semana ("lunes 14").</summary>
        public string Day { get; }

        /// <summary>Tu curso ("8º básico B"), tu carrera o tu pega; vacío si no estás en nada.</summary>
        public string Role { get; }

        /// <summary>La comuna donde vives ("Puente Alto"); vacío si aún no vives en ninguna.</summary>
        public string Commune { get; }

        /// <summary>La fecha corta de la tira, sin el día de la semana ("18 de marzo").</summary>
        public string Date { get; }

        /// <summary>El año del mundo ("Año 27"); sigue creciendo con las generaciones.</summary>
        public string Year { get; }

        /// <summary>Tu plata, con signo si estás bajo cero ("$1.248.300", "-$86.400").</summary>
        public string Money { get; }

        /// <summary>Si tu plata está bajo cero (se pinta en rojo).</summary>
        public bool BelowZero { get; }

        /// <summary>Si es efectivo (de niño, con monedas) y no plata en una cuenta.</summary>
        public bool Cash { get; }

        /// <summary>Lo que te entra al mes ("+$742.000"); vacío si no te entra plata.</summary>
        public string Income { get; }
    }
}
