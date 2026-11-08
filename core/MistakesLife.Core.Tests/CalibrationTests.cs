using System.Linq;
using MistakesLife.Core.Analysis;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Que las vidas simuladas con el contenido de Chile se parezcan a los datos
    /// reales (CASEN 2024 y ENE 2024; ver packs/countries/chile/SOURCES.md).
    /// Los rangos son amplios a propósito: cuidan que nada se salga de lo real,
    /// no el número exacto.
    /// </summary>
    public class CalibrationTests
    {
        [Fact]
        public void Simulador_ALos25_UnaParteSigueEstudiandoYTrabajanMenosQueAntes()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 200, finalAge: 25);

            // En Chile, a los 25 todavía estudia una parte importante (las carreras se alargan
            // y muchos entran tarde) y trabaja cerca del 70 %.
            Assert.InRange(report.PercentOfAlive(report.Studying), 5, 30);
            Assert.InRange(report.PercentOfAlive(report.Working), 60, 82);
        }

        [Fact]
        public void Simulador_ALos25_AlgunosDejanLaCarreraYOtrosSeCambian()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 200, finalAge: 25);

            Assert.True(report.FinalFlags.TryGetValue("deserto", out int deserted) && deserted > 0, "Nadie dejó la carrera.");
            Assert.True(report.LivesWithEvent.TryGetValue("evento.estudios.nueva_carrera", out int changed) && changed > 0, "Nadie se cambió de carrera.");
            Assert.True(report.LivesWithEvent.ContainsKey("evento.estudios.volver_a_estudiar"), "A nadie se le ocurrió volver a estudiar.");
        }

        [Fact]
        public void Simulador_ALos72_PocosSiguenTrabajandoYSeCuentaSoloALosVivos()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 120, finalAge: 72);

            // Quien murió no cuenta como trabajando ni como jubilado.
            Assert.True(report.Working + report.Unemployed <= report.Alive);
            Assert.True(report.Retired <= report.Alive);

            // En Chile trabaja el 12,4 % de quienes tienen 70 años o más, y casi todos cobran pensión.
            Assert.InRange(report.PercentOfAlive(report.Working), 3, 30);
            Assert.True(report.PercentOfAlive(report.Retired) >= 90);
            Assert.True(report.FinalFlags.ContainsKey("jubilado_trabajando"), "Nadie se jubiló y siguió trabajando.");

            // En Chile, la mayoría de los mayores que trabajan lo hacen sin contrato.
            Assert.True(report.WorkingInformal > 0, "Ningún mayor trabaja informal.");
        }

        [Fact]
        public void Simulador_ALos45_NoTodosTrabajan()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 120, finalAge: 45);

            // En Chile trabaja cerca del 75 % de quienes tienen entre 35 y 54 años: el resto
            // cuida a los hijos o a un familiar, está enfermo o no encuentra trabajo.
            Assert.InRange(report.PercentOfAlive(report.Working), 62, 82);
            Assert.True(report.FinalFlags.ContainsKey("cuida_hijos_en_casa"), "Nadie se quedó en casa cuidando a los hijos.");
        }

        [Fact]
        public void Informe_PorcentajesDeTrabajo_SonSobreLosVivos()
        {
            SimulationReport report = new LifeSimulator(RepoPacks.Content).Run(lives: 5, finalAge: 25);

            Assert.Contains("- Siguen vivos al final: " + report.Alive, report.ToMarkdown());
            Assert.Contains("- Estudian al final:", report.ToMarkdown());
            Assert.Equal(report.Lives - report.Deaths, report.Alive);
        }
    }
}
