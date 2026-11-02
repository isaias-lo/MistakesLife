using System;
using System.Collections.Generic;
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

        /// <summary>Id de la madre, si existe en el mundo.</summary>
        public int? MotherId { get; set; }

        /// <summary>Id del padre, si existe en el mundo.</summary>
        public int? FatherId { get; set; }

        /// <summary>Dónde vive: país, ciudad y comuna (ids de los paquetes).</summary>
        public Residence? Residence { get; set; }

        /// <summary>
        /// Nivel socioeconómico del hogar donde creció, de 0 a 100.
        /// Influye en colegio, oportunidades y eventos.
        /// </summary>
        public int HouseholdLevel { get; set; } = 50;

        /// <summary>Etapa educativa actual (id del paquete del país), o null si no estudia.</summary>
        public string? EducationStageId { get; set; }

        private readonly SortedSet<string> _flags = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Marcas simples sobre el personaje, como "postulando" o "fumador".
        /// Las ponen y quitan los efectos de los eventos.
        /// </summary>
        public IReadOnlyCollection<string> Flags => _flags;

        public bool HasFlag(string flag) => _flags.Contains(flag);

        public void SetFlag(string flag) => _flags.Add(flag);

        public void ClearFlag(string flag) => _flags.Remove(flag);

        /// <summary>La carrera que estudia ahora, o null si no estudia.</summary>
        public CareerStudies? Studies { get; set; }

        private readonly List<string> _degrees = new List<string>();

        /// <summary>Niveles de carrera en que se tituló (ids de nivel), en orden.</summary>
        public IReadOnlyList<string> Degrees => _degrees;

        /// <summary>Termina los estudios actuales con su título.</summary>
        public void Graduate()
        {
            if (Studies == null)
            {
                throw new InvalidOperationException($"{this} no está estudiando.");
            }

            _degrees.Add(Studies.LevelId);
            Studies = null;
        }

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
