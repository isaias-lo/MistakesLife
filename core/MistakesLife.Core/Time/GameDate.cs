using System;

namespace MistakesLife.Core.Time
{
    /// <summary>
    /// Fecha dentro del calendario propio del juego. El tiempo avanza de a una
    /// semana; un año tiene 52 semanas. No hay años reales: el año 1 es el
    /// año en que empieza el mundo.
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

        /// <summary>
        /// Mes del año, de 1 a 12. Las 52 semanas se reparten entre los 12 meses:
        /// algunos meses tienen 4 semanas y otros 5.
        /// </summary>
        public int Month => (WeekIndexInYear * 12 / WeeksPerYear) + 1;

        /// <summary>Si esta es la primera semana de su mes (ahí se pagan sueldos y cuentas, y suben los precios).</summary>
        public bool IsFirstWeekOfMonth => WeekIndexInYear == 0 || Month != ((WeekIndexInYear - 1) * 12 / WeeksPerYear) + 1;

        private int WeekIndexInYear => TotalWeeks % WeeksPerYear;

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
