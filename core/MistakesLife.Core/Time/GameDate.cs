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
