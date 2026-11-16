using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Time;
using MistakesLife.Core.Work;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R6c: lo que decides tú en la pega (pedir aumento, vacaciones legales, renunciar con aviso).</summary>
    public class WorkInitiativeTests
    {
        private const string Vestuario = "oficio.vendedor.area.vestuario";

        private static ContentDatabase Content => RepoPacks.Content;

        private static VacationsDefinition VacationsOf(string country) => Content.GetCountry(country).Economy!.Vacations!;

        /// <summary>Una vida a los 25 que entra con contrato a vender ropa (un trabajo con jefe y equipo). A los 25 ya no corre
        /// el fin de contrato al azar de los jóvenes (evento.fin_contrato_joven), que dejaría la prueba a merced de la semilla.</summary>
        private static LifeSession WorkingIn(string country, ulong seed = 5)
        {
            var session = new LifeSession(Content, seed, country);
            session.TimeMachine.Enabled = false;
            World world = session.World;
            new Simulation.WeekCycle().AdvanceWeeks(world, (25 * GameDate.WeeksPerYear) - world.Player.AgeInWeeks(world.Date), out _);
            Run(world, "jugador.dejar_trabajo()");
            Run(world, "jugador.desmarcar('buscando_trabajo')");
            Run(world, "jugador.marcar('trabajando')");
            Run(world, $"jugador.trabajar_en('{Vestuario}')");
            return session;
        }

        private static void Run(World world, string effect) =>
            Scripting.Effect.Parse(effect).Apply(Scope(world));

        private static Scripting.Value Eval(World world, string expression) =>
            Scripting.Expression.Parse(expression).Evaluate(Scope(world));

        private static EventScope Scope(World world) => new EventScope(world, Content, world.Player, null, readOnly: false);

        /// <summary>Avanza semanas; en cada momento elige la opción con ese texto si está, o la primera.</summary>
        private static void Weeks(LifeSession session, int weeks, string? prefer = null)
        {
            for (int i = 0; i < weeks && session.World.Player.IsAlive; i++)
            {
                while (session.Pending != null)
                {
                    int? preferred = OptionIndex(session, prefer);
                    session.Choose(preferred ?? session.Pending.AvailableOptions[0]);
                }

                session.Advance(1);
            }

            while (session.Pending != null)
            {
                session.Choose(OptionIndex(session, prefer) ?? session.Pending.AvailableOptions[0]);
            }
        }

        /// <summary>La opción con ese texto en el momento pendiente, si está disponible.</summary>
        private static int? OptionIndex(LifeSession session, string? textKey)
        {
            PendingEvent pending = session.Pending!;
            EventDefinition ev = Content.Events.Single(e => e.Id == pending.EventId);
            foreach (int index in pending.AvailableOptions)
            {
                if (ev.Options[index].TextKey == textKey)
                {
                    return index;
                }
            }

            return null;
        }

        private static void ChooseRequested(LifeSession session, string eventId, string optionKey)
        {
            Assert.True(session.Request(eventId));
            PendingEvent pending = session.Pending!;
            Assert.Equal(eventId, pending.EventId);
            session.Choose(OptionIndex(session, optionKey) ?? throw new Xunit.Sdk.XunitException("no está la opción " + optionKey));
        }

        [Fact]
        public void Vacaciones_Chile_QuinceDiasHabilesAlAnoYDosAnosComoMaximo()
        {
            VacationsDefinition rules = VacationsOf("pais.chile");
            Character character = WorkingIn("pais.chile").World.Player;
            character.ContributedMonths = 0;
            var job = new Job(JobKind.Employee, 500000, 1);
            Assert.Equal(15, VacationRules.DaysPerYear(rules, character, job));

            for (int month = 0; month < 12; month++)
            {
                VacationRules.Accrue(rules, character, job);
            }

            Assert.Equal(15, job.VacationDays, 6);
            for (int month = 0; month < 36; month++)
            {
                VacationRules.Accrue(rules, character, job);
            }

            Assert.Equal(30, job.VacationDays, 6); // se pueden juntar dos períodos (art. 70)
        }

        [Fact]
        public void Vacaciones_Chile_FeriadoProgresivoConDiezAnosUnDiaCadaTresNuevos()
        {
            VacationsDefinition rules = VacationsOf("pais.chile");

            // 15 años cotizados antes y 3 con este empleador: cuentan 10 de antes + 3 nuevos = 1 día más.
            Assert.Equal(16, VacationRules.DaysPerYear(rules, 36, 18 * 12));
            Assert.Equal(17, VacationRules.DaysPerYear(rules, 72, 21 * 12));

            // Con 9 años en total todavía no hay progresivo.
            Assert.Equal(15, VacationRules.DaysPerYear(rules, 36, 9 * 12));
        }

        [Fact]
        public void Vacaciones_Argentina_SubenConLaAntiguedadComoEnLaLey()
        {
            VacationsDefinition rules = VacationsOf("pais.argentina");
            Assert.Equal(14, VacationRules.DaysPerYear(rules, 0, 0));
            Assert.Equal(14, VacationRules.DaysPerYear(rules, 59, 59));
            Assert.Equal(21, VacationRules.DaysPerYear(rules, 60, 60));
            Assert.Equal(28, VacationRules.DaysPerYear(rules, 120, 120));
            Assert.Equal(35, VacationRules.DaysPerYear(rules, 240, 240));
        }

        [Fact]
        public void Vacaciones_SinContrato_NoSeGanan()
        {
            var job = new Job(JobKind.Freelance, 500000, 1);
            VacationRules.Accrue(VacationsOf("pais.chile"), WorkingIn("pais.chile").World.Player, job);
            Assert.Equal(0, job.VacationDays);
        }

        [Fact]
        public void Vacaciones_AlSalir_ChilePagaLosHabilesComoCorridos()
        {
            var job = new Job(JobKind.Employee, 900000, 1);
            Character character = WorkingIn("pais.chile").World.Player;
            character.ContributedMonths = 0;
            for (int month = 0; month < 12; month++)
            {
                VacationRules.Accrue(VacationsOf("pais.chile"), character, job);
            }

            // 15 días hábiles × 1,4 = 21 días corridos de sueldo diario (900.000 / 30).
            Assert.Equal(630000m, VacationRules.PayFor(VacationsOf("pais.chile"), job));
        }

        [Fact]
        public void PedirAumento_ConDesempenoYRespetoNormales_SigueLaEncuesta()
        {
            (double full, double some) = RaiseRules.Chances(Job.StartingPerformance, RaiseRules.NeutralRespect, 1, 10);
            Assert.Equal(0.39, full, 6);
            Assert.Equal(0.70, some, 6);

            (double good, _) = RaiseRules.Chances(80, 80, 1, 10);
            (double bad, _) = RaiseRules.Chances(20, 20, 1, 10);
            (double greedy, _) = RaiseRules.Chances(Job.StartingPerformance, RaiseRules.NeutralRespect, 1, 30);
            Assert.True(good > full && bad < full && greedy < full);
        }

        [Fact]
        public void Botones_RecienEntrado_NoPuedePedirAumentoPeroSiRenunciar()
        {
            LifeSession session = WorkingIn("pais.chile");
            Weeks(session, 2);
            var ids = session.Requestable().Select(e => e.Id).ToList();
            Assert.DoesNotContain("evento.pega.pedir_aumento", ids);
            Assert.DoesNotContain("evento.pega.vacaciones", ids);
            Assert.Contains("evento.pega.renunciar", ids);
        }

        [Fact]
        public void PedirAumento_ConSeisMeses_PasaConElJefeYDejaUnaLineaEnElDiario()
        {
            LifeSession session = WorkingIn("pais.chile");
            Weeks(session, 27, "opcion.pega.vacaciones.guardar");
            Assert.Contains(session.Requestable(), e => e.Id == "evento.pega.pedir_aumento");

            decimal before = session.World.Player.Job!.BaseSalary;
            Assert.True(session.Request("evento.pega.pedir_aumento"));
            PendingEvent pending = session.Pending!;
            Assert.Equal("evento.pega.pedir_aumento", pending.EventId);
            Assert.NotNull(pending.PersonId);
            session.Choose(OptionIndex(session, "opcion.pega.pedir_aumento.diez")!.Value);

            decimal after = session.World.Player.Job!.BaseSalary;
            Assert.Contains(after, new[] { before, before * 1.05m, before * 1.10m }.Select(v => System.Math.Round(v, 2)));
            Assert.Contains(session.World.Journal, j => RaiseRules.Journals.Contains(j.TextKey));

            // Después de pedir, hay que esperar medio año para volver a pedir.
            Assert.DoesNotContain(session.Requestable(), e => e.Id == "evento.pega.pedir_aumento");
        }

        [Fact]
        public void Vacaciones_ConUnAnoTrabajado_SePidenYSeDescuentanLosDias()
        {
            LifeSession session = WorkingIn("pais.chile");
            Weeks(session, 53, "opcion.pega.vacaciones.guardar");
            double days = session.World.Player.Job!.VacationDays;
            Assert.True(days >= 10, $"tenía {days} días");
            Assert.Contains(session.Requestable(), e => e.Id == "evento.pega.vacaciones");

            ChooseRequested(session, "evento.pega.vacaciones", "opcion.pega.vacaciones.todas");
            Assert.True(session.World.Player.Job!.VacationDays < 1);
        }

        [Fact]
        public void Renunciar_ConAvisoEnChile_SeVaEnCuatroSemanasYLePaganLasVacaciones()
        {
            LifeSession session = WorkingIn("pais.chile");
            Weeks(session, 40, "opcion.pega.vacaciones.guardar");
            ChooseRequested(session, "evento.pega.renunciar", "opcion.pega.renunciar.con_aviso");

            World world = session.World;
            Assert.True(Eval(world, "jugador.renuncio").AsBool("prueba"));
            Assert.Equal(4, Eval(world, "jugador.semanas_para_irse").AsNumber("prueba"));
            Assert.DoesNotContain(session.Requestable(), e => e.Id == "evento.pega.renunciar");

            Job job = world.Player.Job!;
            Weeks(session, 3);
            Assert.Same(job, world.Player.Job);
            Weeks(session, 6);
            Assert.NotSame(job, world.Player.Job);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.pega.ultimo_dia");
            Assert.Equal(0, job.VacationDays);
        }

        [Fact]
        public void Renunciar_HoyEnArgentina_DescuentaElPreaviso()
        {
            LifeSession session = WorkingIn("pais.argentina");
            Weeks(session, 4);
            World world = session.World;
            decimal salary = world.Player.Job!.Salary;
            decimal vacationPay = VacationRules.PayFor(VacationsOf("pais.argentina"), world.Player.Job);
            decimal money = world.Player.Money;

            ChooseRequested(session, "evento.pega.renunciar", "opcion.pega.renunciar.hoy");
            Assert.Null(world.Player.Job);
            // El dinero se redondea a pesos al aplicar cada efecto: se acepta un peso de diferencia.
            Assert.InRange(world.Player.Money, money + vacationPay - (salary / 2) - 1, money + vacationPay - (salary / 2) + 1);
        }

        [Fact]
        public void Guardado_VacacionesYRenunciaConAviso_SeMantienen()
        {
            LifeSession session = WorkingIn("pais.chile");
            Weeks(session, 20, "opcion.pega.vacaciones.guardar");
            ChooseRequested(session, "evento.pega.renunciar", "opcion.pega.renunciar.con_aviso");

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());
            Assert.Equal(session.World.Player.Job!.VacationDays, loaded.World.Player.Job!.VacationDays);
            Assert.Equal(session.World.Player.Job.LeavingWeek, loaded.World.Player.Job.LeavingWeek);
        }

        [Fact]
        public void APedido_ConProbabilidadAnual_SeDetecta()
        {
            var content = new ContentDatabase();
            content.AddSeedsJson("pack.prueba", "seeds.json", "[]");
            content.AddEventsJson("pack.prueba", "events.json", @"[{ ""id"": ""evento.a"", ""lugar"": ""lugar.trabajo"", ""probabilidad_anual"": 1,
              ""a_pedido"": { ""texto"": ""boton.a"", ""por_ano"": ""1"" } }]");
            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Message.Contains("a_pedido"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("accion."));
        }
    }
}
