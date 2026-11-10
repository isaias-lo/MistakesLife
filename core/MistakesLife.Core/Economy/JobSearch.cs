namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// La búsqueda de trabajo de un personaje: qué busca, cómo lo busca y cómo le va.
    /// La empieza el jugador con los eventos (jugador.postular_en y
    /// jugador.buscar_trabajo); <see cref="JobSearchSystem"/> la hace avanzar cada
    /// semana y <see cref="JobSearchRules"/> resuelve entrevistas y ofertas.
    /// </summary>
    public sealed class JobSearch
    {
        /// <summary>Lo que busca quien no apunta a un área de su carrera.</summary>
        public const string AnyJob = "cualquier_trabajo";

        public JobSearch(string target)
        {
            Target = target;
        }

        /// <summary>Un área de su carrera ("carrera.software.area.qa") o <see cref="AnyJob"/>.</summary>
        public string Target { get; set; }

        /// <summary>Si busca en un área de su carrera.</summary>
        public bool TargetsArea => Target != AnyJob;

        /// <summary>Con qué contrato busca en su área (por ejemplo, a honorarios). Fuera de su carrera, siempre con contrato.</summary>
        public JobKind Kind { get; set; } = JobKind.Employee;

        /// <summary>La forma de buscar del país ("busqueda.forma.portales"), o null si todavía no elige.</summary>
        public string? MethodId { get; set; }

        /// <summary>El conocido que lo recomienda (con la forma que necesita contacto), o null.</summary>
        public int? ContactId { get; set; }

        public JobSearchStage Stage { get; set; } = JobSearchStage.Searching;

        /// <summary>Semanas que lleva buscando desde que eligió cómo.</summary>
        public int Weeks { get; set; }

        /// <summary>Postulaciones que ha mandado.</summary>
        public int Applications { get; set; }

        /// <summary>Entrevistas a las que lo han llamado.</summary>
        public int Interviews { get; set; }

        /// <summary>Entrevistas en que no quedó.</summary>
        public int Rejections { get; set; }

        /// <summary>Si ya pidió más sueldo por la oferta actual.</summary>
        public bool Negotiated { get; set; }

        /// <summary>Cuánto más paga la oferta después de negociar (0.08 = 8 % más), o 0.</summary>
        public double Raise { get; set; }
    }
}
