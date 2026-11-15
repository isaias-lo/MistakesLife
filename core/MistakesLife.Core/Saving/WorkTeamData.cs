using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>El equipo del trabajo, guardado (desde la versión 29 del formato).</summary>
    internal sealed class WorkTeamData
    {
        public int? BossId { get; set; }

        public List<int> ColleagueIds { get; set; } = new List<int>();

        public List<int> LeftIds { get; set; } = new List<int>();

        public int MonthsInJob { get; set; }
    }
}
