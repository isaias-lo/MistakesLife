using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class CareerLadderTests
    {
        private const string Desarrollo = "carrera.software.area.desarrollo";
        private const string Soporte = "carrera.software.area.soporte";
        private const string Ciberseguridad = "carrera.software.area.ciberseguridad";
        private const string Junior = "carrera.software.cargo.junior";
        private const string SemiSenior = "carrera.software.cargo.semi_senior";
        private const string Senior = "carrera.software.cargo.senior";
        private const string LiderTecnico = "carrera.software.cargo.lider_tecnico";
        private const string Instituto = "carrera.software.nivel.analista_programador";

        private static ContentDatabase Content => RepoPacks.Content;

        private static void Run(World world, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, Content, world.Player, null, readOnly: false));

        private static Value Eval(World world, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, Content, world.Player, null, readOnly: true));

        /// <summary>Un mundo cuyo jugador ya se tituló de ese nivel de la carrera de software.</summary>
        private static World Graduate(string level = Instituto, ulong seed = 1)
        {
            World world = new LifeSession(Content, seed).World;
            Run(world, $"estudiar('{level}')");
            Run(world, "jugador.titularse()");
            return world;
        }

        /// <summary>Pasa meses en el cargo y deja el desempeño en ese valor.</summary>
        private static void Work(World world, int months, int performance)
        {
            for (int i = 0; i < months; i++)
            {
                CareerLadder.AdvanceMonth(world.Player);
            }

            Run(world, $"jugador.desempeno = {performance}");
        }

        [Fact]
        public void PaquetesReales_CadaCarreraTieneAlMenosTresAreasConSuEscalera()
        {
            foreach (CareerDefinition career in Content.Careers)
            {
                Assert.True(career.Areas.Count >= ContentDatabase.MinimumAreas, $"{career.Id} tiene {career.Areas.Count} áreas.");
                Assert.All(career.Areas, a => Assert.True(a.Ladder.Count >= 4, $"{a.Id} tiene una escalera muy corta."));
                Assert.All(career.Areas, a => Assert.Contains(a.Ladder, p => p.IsTop));
            }
        }

        [Fact]
        public void TrabajarEn_SinExperiencia_EmpiezaEnElCargoInicialConElSueldoDelArea()
        {
            World world = Graduate();
            double degreeSalary = Eval(world, "sueldo_titulo()").AsNumber("prueba");

            Run(world, $"jugador.trabajar_en('{Soporte}')");

            Job job = world.Player.Job!;
            Assert.Equal(Soporte, job.Position!.AreaId);
            Assert.Equal(Junior, job.Position.PositionId);
            Assert.Equal(JobKind.Employee, job.Kind);
            Assert.Equal(Job.StartingPerformance, job.Performance);
            // El sueldo del área, por lo que paga el tamaño de la empresa a la que llegó.
            decimal areaSalary = System.Math.Round((decimal)(degreeSalary * Content.GetCareerArea(Soporte).SalaryRelative), System.MidpointRounding.AwayFromZero);
            double companyRelative = CompanyRules.SizeOf(world, Content, world.Player)!.SalaryRelative;
            Assert.Equal(System.Math.Round(areaSalary * (decimal)companyRelative, System.MidpointRounding.AwayFromZero), job.BaseSalary);
            Assert.Equal(Soporte, Eval(world, "jugador.area").AsText("prueba"));
            Assert.Equal(Junior, Eval(world, "jugador.cargo").AsText("prueba"));
        }

        [Fact]
        public void AreaEspecializada_ConTituloDeInstituto_PideCertificacion()
        {
            World world = Graduate();

            Assert.False(Eval(world, $"jugador.puede_trabajar_en('{Ciberseguridad}')").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(world, $"jugador.trabajar_en('{Ciberseguridad}')"));

            Run(world, "jugador.marcar('certificacion_ciberseguridad')");

            Assert.True(Eval(world, $"jugador.puede_trabajar_en('{Ciberseguridad}')").AsBool("prueba"));
            Run(world, $"jugador.trabajar_en('{Ciberseguridad}')");
            Assert.Equal(Ciberseguridad, world.Player.Job!.Position!.AreaId);
        }

        [Fact]
        public void Area_DeOtraCarrera_PideUnTituloDeEsaCarrera()
        {
            World world = Graduate();

            Assert.False(Eval(world, "jugador.puede_trabajar_en('carrera.contabilidad.area.general')").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(world, "jugador.trabajar_en('carrera.contabilidad.area.general')"));
        }

        [Fact]
        public void AreaEspecializada_ConIngenieriaCivil_NoPideNadaMas()
        {
            World world = Graduate("carrera.software.nivel.ingenieria_civil_computacion");

            Assert.True(Eval(world, $"jugador.puede_trabajar_en('{Ciberseguridad}')").AsBool("prueba"));
        }

        [Fact]
        public void Ascenso_PideTiempoEnElCargoYBuenDesempeno()
        {
            World world = Graduate();
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            int months = Content.GetCareerPosition(SemiSenior).MinMonths;
            int performance = Content.GetCareerPosition(SemiSenior).MinPerformance;

            Work(world, months - 1, 100);
            Assert.False(Eval(world, "jugador.puede_ascender").AsBool("prueba"));

            Work(world, 1, performance - 1);
            Assert.False(Eval(world, "jugador.puede_ascender").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(world, "jugador.ascender()"));

            Run(world, $"jugador.desempeno = {performance}");
            Assert.True(Eval(world, "jugador.puede_ascender").AsBool("prueba"));
            Assert.Equal(1, Eval(world, "jugador.caminos_ascenso").AsNumber("prueba"));
        }

        [Fact]
        public void Ascender_SubeElSueldoYEmpiezaDeCeroEnElCargoNuevo()
        {
            World world = Graduate();
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            decimal before = world.Player.Job!.BaseSalary;
            Work(world, 24, 80);

            Run(world, "jugador.ascender()");

            Job job = world.Player.Job!;
            Assert.Equal(SemiSenior, job.Position!.PositionId);
            Assert.Equal(0, job.MonthsInPosition);
            Assert.Equal(80, job.Performance);
            double ratio = Content.GetCareerPosition(SemiSenior).SalaryRelative / Content.GetCareerPosition(Junior).SalaryRelative;
            Assert.Equal((double)before * ratio, (double)job.BaseSalary, 0);
            Assert.Equal(Junior, Eval(world, "jugador.cargo_anterior").AsText("prueba"));
        }

        [Fact]
        public void Senior_ConDosCaminos_HayQueElegirUno()
        {
            // Una persona extrovertida: puede ir por lo técnico o por la jefatura.
            World world = Enumerable.Range(1, 60).Select(s => Graduate(seed: (ulong)s)).First(w => w.Player.Personality.Extraversion >= 40);
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            Work(world, 24, 90);
            Run(world, "jugador.ascender()");
            Work(world, 30, 90);
            Run(world, "jugador.ascender()");
            Assert.Equal(Senior, world.Player.Job!.Position!.PositionId);
            Work(world, 36, 90);

            Assert.Equal(2, Eval(world, "jugador.caminos_ascenso").AsNumber("prueba"));
            Assert.Throws<ScriptException>(() => Run(world, "jugador.ascender()"));

            Run(world, $"jugador.ascender('{LiderTecnico}')");
            Assert.Equal(LiderTecnico, world.Player.Job!.Position!.PositionId);
        }

        [Fact]
        public void LaCima_NoTieneAscensos()
        {
            CareerPositionDefinition cto = Content.GetCareerPosition("carrera.software.cargo.cto");

            Assert.True(cto.IsTop);
            Assert.Equal(5, cto.Depth);
        }

        [Fact]
        public void BajarCargo_VuelveAlCargoAnterior_YEnElInicialEsUnError()
        {
            World world = Graduate();
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            Assert.Throws<ScriptException>(() => Run(world, "jugador.bajar_cargo()"));
            Work(world, 24, 80);
            Run(world, "jugador.ascender()");

            Run(world, "jugador.bajar_cargo()");

            Assert.Equal(Junior, world.Player.Job!.Position!.PositionId);
            Assert.True(Eval(world, "jugador.cargo_anterior").Kind == ValueKind.Null);
        }

        [Fact]
        public void PerderElTrabajo_RecuerdaElCargo_YVuelveConEsaExperiencia()
        {
            World world = Graduate();
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            Work(world, 24, 80);
            Run(world, "jugador.ascender()");

            Run(world, "jugador.dejar_trabajo()");
            Assert.Equal(SemiSenior, world.Player.LastPosition!.PositionId);
            Assert.Equal(SemiSenior, Eval(world, "jugador.ultimo_cargo").AsText("prueba"));

            // En la misma área vuelve al mismo cargo.
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            Assert.Equal(SemiSenior, world.Player.Job!.Position!.PositionId);

            // Cambiarse de área cuesta un cargo.
            Run(world, $"jugador.trabajar_en('{Soporte}')");
            Assert.Equal(Junior, world.Player.Job!.Position!.PositionId);
            Assert.Equal(Soporte, world.Player.Job.Position.AreaId);
        }

        [Fact]
        public void TrabajoFueraDeLaCarrera_NoTieneArea_YElDesempenoNoSePuedeCambiar()
        {
            World world = Graduate();
            Run(world, "jugador.trabajar(sueldo_minimo(), 'informal')");

            Assert.True(Eval(world, "jugador.area").Kind == ValueKind.Null);
            Assert.False(Eval(world, "jugador.puede_ascender").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(world, "jugador.desempeno += 5"));
        }

        [Fact]
        public void Desempeno_SeAcercaPocoAPocoALoQueRindeSuResponsabilidad()
        {
            World world = Graduate();
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            Run(world, "jugador.desempeno = 0");
            int target = CareerLadder.BasePerformance + world.Player.Personality.Conscientiousness / 2;

            CareerLadder.AdvanceMonth(world.Player);
            Assert.Equal(1, world.Player.Job!.Performance);

            for (int i = 0; i < 120; i++)
            {
                CareerLadder.AdvanceMonth(world.Player);
            }

            Assert.Equal(target, world.Player.Job.Performance);
            Assert.Equal(121, world.Player.Job.MonthsInPosition);
        }

        [Fact]
        public void SistemaDeCarreras_SumaUnMesDeExperienciaCadaMes()
        {
            World world = Graduate();
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");

            new WeekCycle().Register(new Education.CareerSystem()).AdvanceWeeks(world, GameDate.WeeksPerYear);

            Assert.Equal(12, world.Player.Job!.MonthsInPosition);
        }

        [Fact]
        public void Guardar_YCargar_ConservaAreaCargoDesempenoYUltimoCargo()
        {
            var session = new LifeSession(Content, 1);
            World world = session.World;
            Run(world, $"estudiar('{Instituto}')");
            Run(world, "jugador.titularse()");
            Run(world, $"jugador.trabajar_en('{Desarrollo}')");
            Work(world, 20, 77);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, $"jugador.trabajar_en('{Soporte}', 'honorarios')");
            Work(world, 3, 61);

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());

            Character player = loaded.World.Player;
            Assert.Equal(Soporte, player.Job!.Position!.AreaId);
            Assert.Equal(Junior, player.Job.Position.PositionId);
            Assert.Equal(JobKind.Freelance, player.Job.Kind);
            Assert.Equal(3, player.Job.MonthsInPosition);
            Assert.Equal(61, player.Job.Performance);
            Assert.Equal(Desarrollo, player.LastPosition!.AreaId);
            Assert.Equal(session.SaveToText(), loaded.SaveToText());
        }

        private const string CareerJson = @"{
          ""id"": ""carrera.prueba"",
          ""niveles"": [ { ""id"": ""carrera.prueba.nivel.tecnico"", ""institucion"": ""cft"", ""semestres"": 4 } ],
          ""areas"": [ AREAS ],
          ""cargos"": [ CARGOS ]
        }";

        private const string ThreeAreas = @"{ ""id"": ""carrera.prueba.area.a"" }, { ""id"": ""carrera.prueba.area.b"" }, { ""id"": ""carrera.prueba.area.c"" }";

        private const string TwoPositions = @"{ ""id"": ""carrera.prueba.cargo.uno"" }, { ""id"": ""carrera.prueba.cargo.dos"", ""meses_minimos"": 12 }";

        private static ContentLoadException Rejected(string areas, string positions, string events = "[]")
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson);
            content.AddCareerJson("pack.prueba", "career.json", CareerJson.Replace("AREAS", areas).Replace("CARGOS", positions));
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return Assert.Throws<ContentLoadException>(() => content.Build());
        }

        [Fact]
        public void Carrera_ConMenosDeTresAreas_SeRechaza()
        {
            ContentLoadException ex = Rejected(@"{ ""id"": ""carrera.prueba.area.a"" }, { ""id"": ""carrera.prueba.area.b"" }", TwoPositions);

            Assert.Contains(ex.Errors, e => e.Message.Contains("al menos 3 áreas"));
        }

        [Fact]
        public void Escalera_MalArmada_SeRechazaConErroresClaros()
        {
            const string positions = @"
              { ""id"": ""carrera.prueba.cargo.uno"", ""siguientes"": [""carrera.prueba.cargo.no_existe""] },
              { ""id"": ""carrera.prueba.cargo.suelto"", ""desempeno_minimo"": 150 },
              { ""id"": ""otra.cosa"" }";

            ContentLoadException ex = Rejected(ThreeAreas, positions);

            Assert.Contains(ex.Errors, e => e.Message.Contains("no está en esta escalera"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("no se llega a este cargo"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("desempeno_minimo"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("carrera.prueba.cargo."));
        }

        [Fact]
        public void Eventos_ConAreasOCargosQueNoExisten_SeRechazan()
        {
            const string events = @"[ { ""id"": ""evento.x"", ""efectos"": [
              ""jugador.trabajar_en('carrera.prueba.area.z')"", ""jugador.ascender('carrera.prueba.cargo.z')"" ] } ]";

            ContentLoadException ex = Rejected(ThreeAreas, TwoPositions, events);

            Assert.Contains(ex.Errors, e => e.Message.Contains("esa área de carrera no existe"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("ese cargo no existe"));
        }
    }
}
