using System;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Un personaje del mundo: el jugador o cualquier otra persona.
    /// </summary>
    public sealed class Character
    {
        public Character(int id, string firstName, string lastName, Sex sex, GameDate birthDate, Personality personality, Stats stats)
        {
            Id = id;
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Sex = sex;
            BirthDate = birthDate;
            Personality = personality ?? throw new ArgumentNullException(nameof(personality));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>Número único dentro del mundo. Nunca se reutiliza.</summary>
        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public Sex Sex { get; }

        public GameDate BirthDate { get; }

        public Personality Personality { get; }

        public Stats Stats { get; }

        public bool IsAlive { get; private set; } = true;

        public GameDate? DeathDate { get; private set; }

        /// <summary>Edad en semanas en la fecha indicada.</summary>
        public int AgeInWeeks(GameDate date) => Math.Max(0, date.WeeksSince(BirthDate));

        /// <summary>Edad en años cumplidos en la fecha indicada.</summary>
        public int AgeInYears(GameDate date) => AgeInWeeks(date) / GameDate.WeeksPerYear;

        public void Die(GameDate date)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            DeathDate = date;
        }

        public override string ToString() => $"{FirstName} {LastName} (#{Id})";
    }
}
