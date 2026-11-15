using System.Collections.Generic;

namespace MistakesLife.Core.Work
{
    /// <summary>
    /// El equipo del trabajo del jugador: su jefe o jefa y sus colegas con nombre. Se arma al
    /// entrar a un trabajo (con contrato o informal; a honorarios se trabaja solo) y se deshace
    /// al dejarlo. Quienes fueron del equipo quedan como colegas para siempre (pueden
    /// recomendarte después).
    /// </summary>
    public sealed class WorkTeam
    {
        /// <summary>El jefe o la jefa (id de personaje), si hay.</summary>
        public int? BossId { get; internal set; }

        /// <summary>Los colegas del equipo, por id, en el orden en que llegaron.</summary>
        public List<int> ColleagueIds { get; } = new List<int>();

        /// <summary>Quienes dejaron el equipo en el último cambio del año (para verlos en gris, con "Se fue").</summary>
        public List<int> LeftIds { get; } = new List<int>();

        /// <summary>
        /// Los meses que llevaba en el trabajo la última vez que se miró: si el trabajo de ahora
        /// lleva menos, es un trabajo nuevo y el equipo se vuelve a armar.
        /// </summary>
        public int MonthsInJob { get; internal set; }
    }
}
