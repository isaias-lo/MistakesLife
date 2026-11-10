using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class JobSearchTests
    {
        private const string Desarrollo = "carrera.software.area.desarrollo";
        private const string Instituto = "carrera.software.nivel.analista_programador";
        private const string Portales = "busqueda.forma.portales";
        private const string Contactos = "busqueda.forma.contactos";
        private const string Omil = "busqueda.forma.omil";

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        private static Value Eval(LifeSession session, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true));

        /// <summary>Un jugador de 25 años en Chile que está buscando cualquier trabajo de esa forma.</summary>
        private static LifeSession Searching(string method, ulong seed = 3)
        {
            var session = new LifeSession(RepoPacks.Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            AgeTo(session, 25);
            Run(session, "jugador.marcar('buscando_trabajo')");
            Run(session, $"jugador.buscar_trabajo('{method}')");
            return session;
        }

        /// <summary>Corre el tiempo sin que pase el tiempo para nadie más: solo cumple años.</summary>
        private static void AgeTo(LifeSession session, int years)
        {
            Character player = session.World.Player;
            int weeks = years * Time.GameDate.WeeksPerYear - player.AgeInWeeks(session.World.Date);
            new WeekCycle().AdvanceWeeks(session.World, weeks);
        }

        /// <summary>Hace avanzar solo la búsqueda de trabajo, semana a semana, hasta que lo llamen o pase el máximo.</summary>
        private static int WeeksUntilInterview(LifeSession session, int max = 500)
        {
            WeekCycle cycle = new WeekCycle().Register(new JobSearchSystem(session.Content));
            for (int week = 1; week <= max; week++)
            {
                cycle.AdvanceWeek(session.World);
                if (session.World.Player.JobSearch?.Stage == JobSearchStage.Interview)
                {
                    return week;
                }
            }

            return -1;
        }

        private static void GiveOffer(LifeSession session)
        {
            session.World.Player.JobSearch!.Stage = JobSearchStage.Offer;
        }

        [Fact]
        public void BuscarConPortales_PostulaCadaSemanaYTardeOTempranoLoLlamanAEntrevista()
        {
            LifeSession session = Searching(Portales);

            int weeks = WeeksUntilInterview(session);

            Assert.InRange(weeks, 1, 500);
            JobSearch search = session.World.Player.JobSearch!;
            Assert.Equal(weeks, search.Weeks);
            Assert.Equal(weeks * 10, search.Applications);
            Assert.Equal(1, search.Interviews);
            Assert.True(Eval(session, "jugador.entrevista_pendiente").AsBool("prueba"));
            Assert.Equal(weeks * 10, Eval(session, "jugador.postulaciones").AsNumber("prueba"));
        }

        [Fact]
        public void SinElegirComoBuscar_NadieLoLlama()
        {
            var session = new LifeSession(RepoPacks.Content, 3, "pais.chile");
            Run(session, "jugador.marcar('buscando_trabajo')");
            Run(session, "jugador.postular_en('cualquier_trabajo')");

            Assert.Equal(-1, WeeksUntilInterview(session, 200));
            Assert.Equal("cualquier_trabajo", Eval(session, "jugador.busca_en").AsText("prueba"));
            Assert.True(Eval(session, "jugador.busqueda == nada").AsBool("prueba"));
        }

        [Fact]
        public void AlDejarDeBuscar_LaBusquedaSeBorra()
        {
            LifeSession session = Searching(Portales);
            Run(session, "jugador.desmarcar('buscando_trabajo')");

            new WeekCycle().Register(new JobSearchSystem(session.Content)).AdvanceWeek(session.World);

            Assert.Null(session.World.Player.JobSearch);
        }

        [Fact]
        public void ProbabilidadDeEntrevista_SubeConExperienciaYTituloYBajaConEdadCesantiaLargaYCrisis()
        {
            LifeSession session = Searching(Portales);
            Character player = session.World.Player;
            JobSearch search = player.JobSearch!;
            double Chance() => JobSearchRules.InterviewChance(session.World, session.Content, player, search);
            double baseChance = Chance();

            player.ContributedMonths = 60;
            double withExperience = Chance();
            Assert.True(withExperience > baseChance);

            Run(session, $"estudiar('{Instituto}')");
            Run(session, "jugador.titularse()");
            Assert.True(Chance() > withExperience);
            double qualified = Chance();

            search.Weeks = 60;
            Assert.True(Chance() < qualified);
            search.Weeks = 0;

            Run(session, "cambiar_ciclo('economia.ciclo.crisis')");
            Assert.True(Chance() < qualified);
        }

        [Fact]
        public void ProbabilidadDeEntrevista_DesdeLos55EsMenor()
        {
            LifeSession young = Searching(Portales);
            LifeSession older = Searching(Portales);
            AgeTo(older, 56);

            Assert.True(JobSearchRules.InterviewChance(older.World, older.Content, older.World.Player, older.World.Player.JobSearch!)
                < JobSearchRules.InterviewChance(young.World, young.Content, young.World.Player, young.World.Player.JobSearch!));
        }

        [Fact]
        public void Entrevista_PreparadaYConRecomendacion_TieneMasProbabilidadDeQuedar()
        {
            LifeSession session = Searching(Portales);
            JobSearchDefinition rules = JobSearchRules.Definition(session.Content, session.World.Player)!;
            Character player = session.World.Player;

            double improvising = JobSearchRules.SuccessChance(rules, rules.Method(Portales), player, 0);
            double prepared = JobSearchRules.SuccessChance(rules, rules.Method(Portales), player, 12);
            double recommended = JobSearchRules.SuccessChance(rules, rules.Method(Contactos), player, 12);

            Assert.True(prepared > improvising);
            Assert.True(recommended > prepared);
        }

        [Fact]
        public void Entrevista_SiNoQueda_SigueBuscandoYLoAnotaEnElDiario()
        {
            for (ulong seed = 1; seed < 60; seed++)
            {
                LifeSession session = Searching(Portales, seed);
                session.World.Player.JobSearch!.Stage = JobSearchStage.Interview;
                Run(session, "jugador.dar_entrevista(-100)");
                JobSearch search = session.World.Player.JobSearch!;
                if (search.Stage == JobSearchStage.Searching)
                {
                    Assert.Equal(1, search.Rejections);
                    Assert.Contains(session.World.Journal, j => j.TextKey == JobSearchRules.RejectionJournal);
                    return;
                }
            }

            Assert.Fail("Con -100 puntos casi nunca se queda; alguna partida debió no quedar.");
        }

        [Fact]
        public void AceptarOferta_EnSuArea_TrabajaAhiConLaMarcaDeSuCarreraYDejaDeBuscar()
        {
            var session = new LifeSession(RepoPacks.Content, 5, "pais.chile");
            Run(session, $"estudiar('{Instituto}')");
            Run(session, "jugador.titularse()");
            Run(session, "jugador.marcar('buscando_trabajo')");
            Run(session, $"jugador.postular_en('{Desarrollo}')");
            Run(session, $"jugador.buscar_trabajo('{Portales}')");
            GiveOffer(session);
            Assert.Equal(Desarrollo, Eval(session, "jugador.oferta_area").AsText("prueba"));

            Run(session, "jugador.aceptar_oferta()");

            Character player = session.World.Player;
            Assert.Equal(Desarrollo, player.Job?.Position?.AreaId);
            Assert.NotNull(player.Job!.CompanyId);
            Assert.Null(player.JobSearch);
            Assert.True(player.HasFlag("trabaja_software"));
            Assert.True(player.HasFlag(JobSearchRules.WorkingFlag));
            Assert.False(player.HasFlag(JobSearchRules.SearchingFlag));
            Assert.Contains(session.World.Journal, j => j.TextKey == JobSearchRules.HiredJournal);
        }

        [Fact]
        public void AceptarOferta_SinTituloNiExperiencia_GanaElMinimo_YConExperienciaLoTipico()
        {
            LifeSession first = Searching(Portales);
            GiveOffer(first);
            Run(first, "jugador.aceptar_oferta()");

            LifeSession experienced = Searching(Portales);
            experienced.World.Player.ContributedMonths = 36;
            GiveOffer(experienced);
            Run(experienced, "jugador.aceptar_oferta()");

            Assert.Equal(539000m, first.World.Player.Job!.BaseSalary);
            Assert.Equal(Math.Round(760000m * 0.85m), experienced.World.Player.Job!.BaseSalary);
            Assert.Equal(JobKind.Employee, first.World.Player.Job.Kind);
        }

        [Fact]
        public void NegociarOferta_PuedeSubirElSueldo_MantenerloORetirarLaOferta()
        {
            bool raised = false, same = false, withdrawn = false;
            for (ulong seed = 1; seed < 200 && !(raised && same && withdrawn); seed++)
            {
                LifeSession session = Searching(Portales, seed);
                GiveOffer(session);
                Run(session, "jugador.negociar_oferta()");
                JobSearch search = session.World.Player.JobSearch!;
                if (search.Stage == JobSearchStage.Searching)
                {
                    withdrawn = true;
                    Assert.Contains(session.World.Journal, j => j.TextKey == JobSearchRules.WithdrawnJournal);
                    continue;
                }

                Assert.True(Eval(session, "jugador.oferta_negociada").AsBool("prueba"));
                if (Eval(session, "jugador.oferta_mejorada").AsBool("prueba"))
                {
                    raised = true;
                    Run(session, "jugador.aceptar_oferta()");
                    Assert.Equal(Math.Round(539000m * 1.08m), session.World.Player.Job!.BaseSalary);
                }
                else
                {
                    same = true;
                }
            }

            Assert.True(raised && same && withdrawn);
        }

        [Fact]
        public void RechazarOferta_SigueBuscando()
        {
            LifeSession session = Searching(Portales);
            GiveOffer(session);

            Run(session, "jugador.rechazar_oferta()");

            Assert.Equal(JobSearchStage.Searching, session.World.Player.JobSearch!.Stage);
            Assert.Null(session.World.Player.Job);
        }

        [Fact]
        public void BuscarConContactos_SinConocidos_NoSePuede_ConUnAmigo_QuedaComoContacto()
        {
            LifeSession session = Searching(Portales);
            Assert.False(Eval(session, $"jugador.puede_buscar_con('{Contactos}')").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(session, $"jugador.buscar_trabajo('{Contactos}')"));

            Run(session, "jugador.conocer('amistad')");
            Assert.True(Eval(session, $"jugador.puede_buscar_con('{Contactos}')").AsBool("prueba"));
            Run(session, $"jugador.buscar_trabajo('{Contactos}')");

            Character friend = session.World.GetCharacter(session.World.Player.JobSearch!.ContactId!.Value);
            Assert.Equal(RelationshipBond.Friend, session.World.Player.RelationshipWith(friend.Id)!.Bond);
            Assert.True(Eval(session, "jugador.contacto != nada && jugador.contacto.carino >= 50").AsBool("prueba"));
        }

        [Fact]
        public void LaOficinaMunicipal_SoloExisteEnChile()
        {
            LifeSession chile = Searching(Portales);
            var argentina = new LifeSession(RepoPacks.Content, 3, "pais.argentina");

            Assert.True(Eval(chile, $"jugador.puede_buscar_con('{Omil}')").AsBool("prueba"));
            Assert.False(Eval(argentina, $"jugador.puede_buscar_con('{Omil}')").AsBool("prueba"));
            Assert.Throws<ScriptException>(() => Run(argentina, $"jugador.buscar_trabajo('{Omil}')"));
        }

        [Fact]
        public void Guardar_UnaBusquedaEnCurso_SeConservaIgual()
        {
            LifeSession session = Searching(Portales);
            WeeksUntilInterview(session);
            JobSearch before = session.World.Player.JobSearch!;

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            JobSearch after = loaded.World.Player.JobSearch!;
            Assert.Equal(before.Target, after.Target);
            Assert.Equal(before.MethodId, after.MethodId);
            Assert.Equal(before.Stage, after.Stage);
            Assert.Equal(before.Weeks, after.Weeks);
            Assert.Equal(before.Applications, after.Applications);
            Assert.Equal(before.Interviews, after.Interviews);
            Assert.Equal(before.Kind, after.Kind);
        }

        [Fact]
        public void UnTrabajoDeCarrera_YaNoLlegaSolo_HayQueBuscarlo()
        {
            // Los eventos que entregaban trabajo sin buscarlo ya no existen.
            Assert.False(RepoPacks.Content.TryGetEvent("evento.trabajo_adulto", out _));
            Assert.False(RepoPacks.Content.TryGetEvent("evento.trabajo_sin_titulo", out _));
            EventDefinition primer = RepoPacks.Content.GetEvent("evento.software.primer_trabajo");
            Assert.DoesNotContain(primer.Options.SelectMany(o => o.Effects), e => e.Contains("trabajar_en"));
        }
    }
}
