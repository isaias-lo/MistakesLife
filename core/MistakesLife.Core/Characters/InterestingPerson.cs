using MistakesLife.Core.Time;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Alguien interesante de tu horario (D4b2): una persona con algo real que darte (un cargo
    /// más alto en tu empresa, o que trabaja en lo que estudias) que el juego te muestra en
    /// "Gente que podrías conocer". Mientras no conversan no hay relación de tu lado; ver
    /// <see cref="Life.InterestingPeopleRules"/>.
    /// </summary>
    public sealed class InterestingPerson
    {
        public InterestingPerson(int personId, string source, GameDate since)
        {
            PersonId = personId;
            Source = source;
            Since = since;
        }

        /// <summary>Id del personaje.</summary>
        public int PersonId { get; }

        /// <summary>
        /// Dónde lo ves: <see cref="Life.InterestingPeopleRules.WorkSource"/> (tu pega) o
        /// <see cref="Life.InterestingPeopleRules.StudiesSource"/> (tu carrera).
        /// </summary>
        public string Source { get; }

        /// <summary>Desde cuándo aparece en tu horario.</summary>
        public GameDate Since { get; }

        /// <summary>Cuántas veces te acercaste y no te pescó; a las <see cref="Life.InterestingPeopleRules.MaxDeclines"/> se cierra la puerta.</summary>
        public int Declines { get; set; }
    }
}
