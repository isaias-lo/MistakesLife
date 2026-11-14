using System;

namespace MistakesLife.Core.Time
{
    /// <summary>
    /// Fecha dentro del calendario propio del juego. El tiempo avanza de a una
    /// semana, que siempre empieza un lunes; un año tiene 52 semanas y sus meses
    /// tienen los días de siempre (enero 31, febrero 28...). No hay años reales:
    /// el año 1 es el año en que empieza el mundo.
    /// </summary>
    public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public const int WeeksPerYear = 52;

        public GameDate(int totalWeeks)
        {
            if (totalWeeks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalWeeks), "No puede ser negativo.");
            }

            TotalWeeks = totalWeeks;
        }

        /// <summary>Semanas transcurridas desde el inicio del mundo.</summary>
        public int TotalWeeks { get; }

        /// <summary>Año del juego, desde 1.</summary>
        public int Year => (TotalWeeks / WeeksPerYear) + 1;

        /// <summary>Semana dentro del año, de 1 a 52.</summary>
        public int WeekOfYear => (TotalWeeks % WeeksPerYear) + 1;

        /// <summary>Días que dura cada mes del calendario del juego (no hay años bisiestos).</summary>
        private static readonly int[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Día del año en que empieza esta semana, desde 0 (1 de enero). Cada semana
        /// empieza un lunes; las 52 semanas cubren 364 días y el 31 de diciembre
        /// queda dentro de la última.
        /// </summary>
        public int DayOfYear => WeekIndexInYear * 7;

        /// <summary>Mes del año, de 1 a 12: el mes del lunes en que empieza la semana.</summary>
        public int Month => MonthOfDay(DayOfYear);

        /// <summary>Día del mes del lunes en que empieza la semana, de 1 a 31.</summary>
        public int DayOfMonth
        {
            get
            {
                int day = DayOfYear;
                int month = 0;
                while (day >= DaysInMonth[month])
                {
                    day -= DaysInMonth[month];
                    month++;
                }

                return day + 1;
            }
        }

        /// <summary>Si esta es la primera semana de su mes (ahí se pagan sueldos y cuentas, y suben los precios).</summary>
        public bool IsFirstWeekOfMonth => WeekIndexInYear == 0 || Month != MonthOfDay(DayOfYear - 7);

        private int WeekIndexInYear => TotalWeeks % WeeksPerYear;

        private static int MonthOfDay(int dayOfYear)
        {
            int month = 0;
            while (month < 11 && dayOfYear >= DaysInMonth[month])
            {
                dayOfYear -= DaysInMonth[month];
                month++;
            }

            return month + 1;
        }

        public static GameDate Start => new GameDate(0);

        public GameDate AddWeeks(int weeks) => new GameDate(TotalWeeks + weeks);

        /// <summary>Semanas completas entre <paramref name="earlier"/> y esta fecha.</summary>
        public int WeeksSince(GameDate earlier) => TotalWeeks - earlier.TotalWeeks;

        public bool Equals(GameDate other) => TotalWeeks == other.TotalWeeks;

        public override bool Equals(object? obj) => obj is GameDate other && Equals(other);

        public override int GetHashCode() => TotalWeeks;

        public int CompareTo(GameDate other) => TotalWeeks.CompareTo(other.TotalWeeks);

        public override string ToString() => $"Año {Year}, semana {WeekOfYear}";

        public static bool operator ==(GameDate a, GameDate b) => a.Equals(b);

        public static bool operator !=(GameDate a, GameDate b) => !a.Equals(b);

        public static bool operator <(GameDate a, GameDate b) => a.TotalWeeks < b.TotalWeeks;

        public static bool operator >(GameDate a, GameDate b) => a.TotalWeeks > b.TotalWeeks;

        public static bool operator <=(GameDate a, GameDate b) => a.TotalWeeks <= b.TotalWeeks;

        public static bool operator >=(GameDate a, GameDate b) => a.TotalWeeks >= b.TotalWeeks;
    }
}
