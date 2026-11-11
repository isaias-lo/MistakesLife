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

        /// <summary>Cuántas veces salió cada noticia en los últimos 12 años de cada vida (lo que el mundo recuerda).</summary>
        public SortedDictionary<string, int> NewsCount { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Semillas por tipo: plantadas, despertadas, resueltas y olvidadas.</summary>
        public SortedDictionary<string, SeedStats> Seeds { get; } = new SortedDictionary<string, SeedStats>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas terminan con cada marca (por ejemplo "trabaja_software").</summary>
        public SortedDictionary<string, int> FinalFlags { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas terminan con cada título (id de nivel de carrera).</summary>
        public SortedDictionary<string, int> Degrees { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas terminan en cada cargo de carrera (id de cargo), entre quienes trabajan en su carrera.</summary>
        public SortedDictionary<string, int> FinalPositions { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Cuántas vidas terminan trabajando en una empresa de cada tamaño (id de tamaño).</summary>
        public SortedDictionary<string, int> FinalCompanySizes { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Empresas quebradas en todas las vidas (de las que conoció cada mundo).</summary>
        public int CompaniesClosed { get; internal set; }

        /// <summary>Vidas que en algún momento llegaron a la cima de su carrera.</summary>
        public int ReachedTop { get; internal set; }

        /// <summary>Ascensos por vida, en promedio, entre quienes terminan trabajando en su carrera (altura del cargo final).</summary>
        public double AveragePromotions { get; internal set; }

        /// <summary>Cuántas vidas siguen estudiando cada nivel al final.</summary>
        public SortedDictionary<string, int> StillStudying { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public double AverageHealth { get; internal set; }

        public double AverageHappiness { get; internal set; }

        public double AverageSmarts { get; internal set; }

        public double AverageEventsPerLife { get; internal set; }

        /// <summary>Vidas que siguen vivas al final y tienen trabajo (con sueldo).</summary>
        public int Working { get; internal set; }

        /// <summary>Vidas que siguen vivas al final con un trabajo informal (sin contrato ni cotizaciones).</summary>
        public int WorkingInformal { get; internal set; }

        /// <summary>Vidas que terminan buscando trabajo: no trabajan, no estudian y no se jubilaron, pero quieren trabajar.</summary>
        public int Unemployed { get; internal set; }

        /// <summary>Cuántas veces se encontró trabajo buscándolo (aceptando una oferta), sumando todas las vidas.</summary>
        public int JobSearchHires { get; internal set; }

        /// <summary>Semanas buscando, en promedio, hasta aceptar una oferta.</summary>
        public double AverageSearchWeeks { get; internal set; }

        /// <summary>Entrevistas, en promedio, por cada trabajo encontrado.</summary>
        public double AverageInterviewsPerHire { get; internal set; }

        /// <summary>
        /// Promedio, entre las vidas que llegan a los 65, de la parte de los meses
        /// desde los 18 en que cotizó para su pensión (densidad de cotización).
        /// </summary>
        public double AverageContributionDensity { get; internal set; }

        /// <summary>
        /// Mediana del sueldo bruto de quienes trabajan al final, en dinero del inicio
        /// del mundo (sin inflación), para compararla con los sueldos reales del país.
        /// </summary>
        public double MedianRealSalary { get; internal set; }

        /// <summary>Vidas que terminan vivas con al menos un amigo.</summary>
        public int WithFriends { get; internal set; }

        /// <summary>Amigos vivos al final, en promedio por vida.</summary>
        public double AverageFriends { get; internal set; }

        /// <summary>Vidas que terminan con un rival.</summary>
        public int WithRival { get; internal set; }

        /// <summary>Vidas que tuvieron al menos una expareja.</summary>
        public int WithEx { get; internal set; }

        /// <summary>Cariño promedio que siente la madre por el jugador al final (solo si ella vive).</summary>
        public double AverageMotherAffection { get; internal set; }

        /// <summary>Rencor promedio que siente la pareja por el jugador al final (solo si tiene pareja).</summary>
        public double AveragePartnerResentment { get; internal set; }

        /// <summary>Vidas que siguen al final con el dinero en negativo (al morir, el dinero pasa a la herencia).</summary>
        public int InRed { get; internal set; }

        /// <summary>Mediana del dinero al final de quienes siguen vivos, en dinero del inicio del mundo (sin inflación).</summary>
        public double MedianRealMoney { get; internal set; }

        /// <summary>Vidas que terminan jubiladas (con pensión propia, aunque sea 0).</summary>
        public int Retired { get; internal set; }

        /// <summary>
        /// Mediana de lo que reciben al mes de pensiones (propia + del Estado) quienes
        /// terminan jubilados, en dinero del inicio del mundo.
        /// </summary>
        public double MedianRealPension { get; internal set; }

        /// <summary>Vidas que terminan con alguna deuda.</summary>
        public int WithDebt { get; internal set; }

        /// <summary>Vidas que terminan con celular.</summary>
        public int WithPhone { get; internal set; }

        /// <summary>Vidas que terminan con cuenta de correo.</summary>
        public int WithMail { get; internal set; }

        /// <summary>Correos en las bandejas al final, sumando todas las vidas con correo.</summary>
        public int MailsReceived { get; internal set; }

        /// <summary>Vidas que cayeron alguna vez en una estafa por correo.</summary>
        public int ScamVictims { get; internal set; }

        /// <summary>Vidas que siguen al final en el registro de morosos (DICOM).</summary>
        public int Delinquent { get; internal set; }

        /// <summary>Mediana de lo que deben quienes terminan con deudas, en dinero del inicio del mundo.</summary>
        public double MedianRealDebt { get; internal set; }

        /// <summary>Inflación anual promedio del país del jugador durante las vidas.</summary>
        public double AverageAnnualInflation { get; internal set; }

        public int Deaths { get; internal set; }

        /// <summary>Vidas que siguen vivas al final.</summary>
        public int Alive => Lives - Deaths;

        /// <summary>Vidas que siguen vivas al final y están cursando una carrera.</summary>
        public int Studying { get; internal set; }

        public double Percent(int count) => Lives == 0 ? 0 : 100.0 * count / Lives;

        /// <summary>
        /// Porcentaje sobre las vidas que siguen vivas: así se compara con las
        /// encuestas (CASEN, INE), que solo preguntan a quienes viven.
        /// </summary>
        public double PercentOfAlive(int count) => Alive == 0 ? 0 : 100.0 * count / Alive;

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
            sb.AppendLine($"- Siguen vivos al final: {Alive}; los porcentajes de trabajo, estudio, búsqueda y jubilación son sobre ellos");
            sb.AppendLine($"- Estudian al final: {PercentOfAlive(Studying).ToString("0.0", ci)} %");
            sb.AppendLine($"- Trabajan al final: {PercentOfAlive(Working).ToString("0.0", ci)} % (informales: {PercentOfAlive(WorkingInformal).ToString("0.0", ci)} %); sueldo bruto mediano (en dinero del inicio): {MedianRealSalary.ToString("N0", ci)}");
            sb.AppendLine($"- Buscan trabajo al final: {PercentOfAlive(Unemployed).ToString("0.0", ci)} %; desempleo (buscan / trabajan + buscan): {(Working + Unemployed == 0 ? 0 : 100.0 * Unemployed / (Working + Unemployed)).ToString("0.0", ci)} %");
            sb.AppendLine($"- Trabajos encontrados buscando: {JobSearchHires} ({(Lives == 0 ? 0 : (double)JobSearchHires / Lives).ToString("0.00", ci)} por vida); semanas buscando hasta aceptar (promedio): {AverageSearchWeeks.ToString("0.0", ci)}; entrevistas por trabajo: {AverageInterviewsPerHire.ToString("0.0", ci)}");
            if (AverageContributionDensity > 0)
            {
                sb.AppendLine($"- Meses con cotización para la pensión, de los 18 a los 65 (promedio): {(AverageContributionDensity * 100).ToString("0", ci)} %");
            }

            sb.AppendLine($"- Siguen vivos con el dinero en negativo: {Percent(InRed).ToString("0.0", ci)} %; dinero mediano de los vivos (en dinero del inicio): {MedianRealMoney.ToString("N0", ci)}");
            sb.AppendLine($"- Terminan jubilados: {PercentOfAlive(Retired).ToString("0.0", ci)} %; pensión mediana al mes, propia + del Estado (en dinero del inicio): {MedianRealPension.ToString("N0", ci)}");
            sb.AppendLine($"- Terminan con celular: {Percent(WithPhone).ToString("0.0", ci)} %; con correo: {Percent(WithMail).ToString("0.0", ci)} % (correos en la bandeja, promedio: {(WithMail == 0 ? 0 : (double)MailsReceived / WithMail).ToString("0.0", ci)}); cayeron en una estafa por correo: {Percent(ScamVictims).ToString("0.0", ci)} %");
            sb.AppendLine($"- Terminan con deudas: {Percent(WithDebt).ToString("0.0", ci)} %; deuda mediana (en dinero del inicio): {MedianRealDebt.ToString("N0", ci)}; morosos: {Percent(Delinquent).ToString("0.0", ci)} %");
            sb.AppendLine($"- Terminan con amigos: {Percent(WithFriends).ToString("0.0", ci)} % (promedio {AverageFriends.ToString("0.0", ci)}); con un rival: {Percent(WithRival).ToString("0.0", ci)} %; tuvieron una expareja: {Percent(WithEx).ToString("0.0", ci)} %");
            sb.AppendLine($"- Cariño de la madre (si vive) / rencor de la pareja (si hay), promedio al final: {AverageMotherAffection.ToString("0", ci)} / {AveragePartnerResentment.ToString("0", ci)}");
            sb.AppendLine($"- Trabajan en un cargo de su carrera: {FinalPositions.Values.Sum()} vidas; ascensos en promedio (altura del cargo final): {AveragePromotions.ToString("0.0", ci)}; llegaron a la cima: {Percent(ReachedTop).ToString("0.0", ci)} %");
            int inCompany = FinalCompanySizes.Values.Sum();
            string sizes = string.Join(", ", FinalCompanySizes.Select(s => $"{s.Key.Substring(s.Key.LastIndexOf('.') + 1)} {(inCompany == 0 ? 0 : 100.0 * s.Value / inCompany).ToString("0", ci)} %"));
            sb.AppendLine($"- Terminan en una empresa: {inCompany} vidas ({sizes}); empresas quebradas: {CompaniesClosed}");
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
            if (FinalPositions.Count > 0)
            {
                sb.AppendLine("## Cargos al final");
                sb.AppendLine();
                sb.AppendLine("| Cargo | Vidas |");
                sb.AppendLine("|---|---:|");
                foreach (KeyValuePair<string, int> position in FinalPositions)
                {
                    sb.AppendLine($"| {position.Key} | {position.Value} |");
                }

                sb.AppendLine();
            }

            sb.AppendLine("## Eventos");
            sb.AppendLine();
            sb.AppendLine("| Evento | Vidas donde aparece | % | Veces en total | Veces por vida (donde aparece) |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var ev in LivesWithEvent.OrderByDescending(e => e.Value).ThenBy(e => e.Key, StringComparer.Ordinal))
            {
                sb.AppendLine($"| {ev.Key} | {ev.Value} | {Percent(ev.Value).ToString("0.0", ci)} | {EventCount[ev.Key]} | {((double)EventCount[ev.Key] / ev.Value).ToString("0.0", ci)} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Noticias (los últimos 12 años de cada vida, lo que el mundo recuerda)");
            sb.AppendLine();
            if (NewsCount.Count == 0)
            {
                sb.AppendLine("No salió ninguna noticia.");
            }
            else
            {
                sb.AppendLine("| Noticia | Veces en total | Veces por vida |");
                sb.AppendLine("|---|---:|---:|");
                foreach (var news in NewsCount.OrderByDescending(n => n.Value).ThenBy(n => n.Key, StringComparer.Ordinal))
                {
                    sb.AppendLine($"| {news.Key} | {news.Value} | {((double)news.Value / Math.Max(1, Lives)).ToString("0.0", ci)} |");
                }
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
