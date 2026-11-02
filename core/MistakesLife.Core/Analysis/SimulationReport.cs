using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MistakesLife.Core.Analysis
{
    /// <summary>
    /// Resultados de simular muchas vidas: sirve para revisar que el contenido
    /// se parezca a la realidad y encontrar eventos que nunca aparecen.
    /// </summary>
    public sealed class SimulationReport
    {
        public int Lives { get; internal set; }

        public int FinalAge { get; internal set; }

        /// <summary>En cuántas vidas apareció cada evento (al menos una vez).</summary>
        public SortedDictionary<string, int> LivesWithEvent { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Veces totales que apareció cada evento.</summary>
        public SortedDictionary<string, int> EventCount { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Eventos del contenido que no aparecieron en ninguna vida.</summary>
        public List<string> NeverSeenEvents { get; } = new List<string>();

        /// <summary>Semillas por tipo: plantadas, despertadas, resueltas y olvidadas.</summary>
        public SortedDictionary<string, SeedStats> Seeds { get; } = new SortedDictionary<string, SeedStats>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas terminan con cada marca (por ejemplo "trabaja_software").</summary>
        public SortedDictionary<string, int> FinalFlags { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas terminan con cada título (id de nivel de carrera).</summary>
        public SortedDictionary<string, int> Degrees { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas siguen estudiando cada nivel al final.</summary>
        public SortedDictionary<string, int> StillStudying { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public double AverageHealth { get; internal set; }

        public double AverageHappiness { get; internal set; }

        public double AverageSmarts { get; internal set; }

        public double AverageEventsPerLife { get; internal set; }

        /// <summary>Vidas que terminan con trabajo (con sueldo).</summary>
        public int Working { get; internal set; }

        /// <summary>
        /// Mediana del sueldo bruto de quienes trabajan al final, en dinero del inicio
        /// del mundo (sin inflación), para compararla con los sueldos reales del país.
        /// </summary>
        public double MedianRealSalary { get; internal set; }

        /// <summary>Inflación anual promedio del país del jugador durante las vidas.</summary>
        public double AverageAnnualInflation { get; internal set; }

        public int Deaths { get; internal set; }

        public double Percent(int count) => Lives == 0 ? 0 : 100.0 * count / Lives;

        /// <summary>El informe en Markdown, para leerlo en GitHub o en la terminal.</summary>
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;
            sb.AppendLine($"# Simulación de {Lives} vidas hasta los {FinalAge} años");
            sb.AppendLine();
            sb.AppendLine($"- Eventos por vida (promedio): {AverageEventsPerLife.ToString("0.0", ci)}");
            sb.AppendLine($"- Salud / felicidad / inteligencia al final (promedio): {AverageHealth.ToString("0", ci)} / {AverageHappiness.ToString("0", ci)} / {AverageSmarts.ToString("0", ci)}");
            sb.AppendLine($"- Muertes antes de los {FinalAge}: {Deaths}");
            sb.AppendLine($"- Trabajan al final: {Percent(Working).ToString("0.0", ci)} %; sueldo bruto mediano (en dinero del inicio): {MedianRealSalary.ToString("N0", ci)}");
            sb.AppendLine($"- Inflación anual promedio: {(AverageAnnualInflation * 100).ToString("0.0", ci)} %");
            sb.AppendLine();
            sb.AppendLine("## Cómo terminan las vidas (marcas)");
            sb.AppendLine();
            sb.AppendLine("| Marca | Vidas | % |");
            sb.AppendLine("|---|---:|---:|");
            foreach (var flag in FinalFlags.OrderByDescending(f => f.Value).ThenBy(f => f.Key, StringComparer.Ordinal))
            {
                sb.AppendLine($"| {flag.Key} | {flag.Value} | {Percent(flag.Value).ToString("0.0", ci)} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Estudios superiores al final");
            sb.AppendLine();
            sb.AppendLine("| Nivel de carrera | Titulados | % | Siguen estudiando | % |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (string level in Degrees.Keys.Union(StillStudying.Keys).OrderBy(k => k, StringComparer.Ordinal))
            {
                Degrees.TryGetValue(level, out int done);
                StillStudying.TryGetValue(level, out int studying);
                sb.AppendLine($"| {level} | {done} | {Percent(done).ToString("0.0", ci)} | {studying} | {Percent(studying).ToString("0.0", ci)} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Eventos");
            sb.AppendLine();
            sb.AppendLine("| Evento | Vidas donde aparece | % | Veces en total |");
            sb.AppendLine("|---|---:|---:|---:|");
            foreach (var ev in LivesWithEvent.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.Ordinal))
            {
                sb.AppendLine($"| {ev.Key} | {ev.Value} | {Percent(ev.Value).ToString("0.0", ci)} | {EventCount[ev.Key]} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Eventos que nunca aparecieron");
            sb.AppendLine();
            if (NeverSeenEvents.Count == 0)
            {
                sb.AppendLine("Ninguno: todos los eventos aparecieron al menos una vez.");
            }
            else
            {
                foreach (string id in NeverSeenEvents)
                {
                    sb.AppendLine($"- {id}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Semillas");
            sb.AppendLine();
            sb.AppendLine("| Semilla | Plantadas | Despertaron | Resueltas | Olvidadas | Siguen abiertas |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (var seed in Seeds)
            {
                SeedStats s = seed.Value;
                sb.AppendLine($"| {seed.Key} | {s.Planted} | {s.Awakened} | {s.Resolved} | {s.Expired} | {s.Open} |");
            }

            return sb.ToString();
        }
    }
}
