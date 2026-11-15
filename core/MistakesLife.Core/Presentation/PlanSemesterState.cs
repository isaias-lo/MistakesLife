namespace MistakesLife.Core.Presentation
{
    /// <summary>Cómo va un semestre de la malla.</summary>
    public enum PlanSemesterState
    {
        /// <summary>Ya lo aprobaste.</summary>
        Approved,

        /// <summary>Lo estás cursando por primera vez.</summary>
        Current,

        /// <summary>Lo estás cursando de nuevo porque quedó un ramo rojo.</summary>
        Retaking,

        /// <summary>Todavía no llegas.</summary>
        Future,
    }
}
