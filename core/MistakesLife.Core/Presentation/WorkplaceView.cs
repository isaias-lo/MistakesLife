using System.Globalization;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// El lugar donde trabaja el jugador, listo para dibujar. Sale de su título:
    /// la carrera dice qué tipo de lugar es (paquete de carrera, <c>lugar_de_trabajo</c>)
    /// y el sueldo relativo del título dice qué tan bueno es.
    /// </summary>
    public sealed class WorkplaceView
    {
        public WorkplaceView(string? careerId, string? kind, double salaryRelative, bool freelance, HomeMemberView player)
        {
            CareerId = careerId;
            Kind = kind;
            SalaryRelative = salaryRelative;
            Freelance = freelance;
            Player = player;
        }

        /// <summary>La carrera del título con que trabaja, o null si trabaja sin título.</summary>
        public string? CareerId { get; }

        /// <summary>
        /// Tipo de lugar (uno de <see cref="Content.CareerDefinition.WorkplaceKinds"/>), o null:
        /// sin título es un local de atención; con título y sin tipo, una oficina común.
        /// </summary>
        public string? Kind { get; }

        /// <summary>Sueldo del título comparado con el típico del país (1 sin título).</summary>
        public double SalaryRelative { get; }

        /// <summary>True si trabaja a honorarios.</summary>
        public bool Freelance { get; }

        /// <summary>El jugador, para dibujar su figura.</summary>
        public HomeMemberView Player { get; }

        /// <summary>Resumen corto para saber si hay que volver a dibujar el lugar.</summary>
        public string Signature =>
            string.Join("|", CareerId ?? "-", Kind ?? "-", SalaryRelative.ToString("0.0", CultureInfo.InvariantCulture), Freelance ? "h" : "c", Player.Stage.ToString());
    }
}
