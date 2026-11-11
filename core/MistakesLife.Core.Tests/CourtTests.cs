using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Courts;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Los juzgados y la vida civil: causas que duran meses (con RIT, etapas y plazos)
    /// y uniones ante la ley (casarse, firmar unión civil, separarse, divorciarse).
    /// Una causa nunca empieza sola: la abre un evento por algo que hizo el jugador.
    /// </summary>
    public class CourtTests
    {
        private const string CasesJson = @"[
          { ""id"": ""causa.prueba.cobro"", ""tribunal"": ""tribunal.prueba.civil"", ""prefijo_rit"": ""C"",
            ""etapas"": [
              { ""id"": ""demanda"", ""semanas"": 4 },
              { ""id"": ""audiencia"", ""semanas"": 2, ""espera"": true },
              { ""id"": ""remate"", ""semanas"": 3 }
            ],
            ""resultados"": [""pago"", ""terminada""] }
        ]";

        private static string CountryWithUnions =>
            CountryTests.CountryJson.Replace(@"""moneda"": ""CLP"",", @"""moneda"": ""CLP"", ""uniones_civiles"": [""union.prueba.matrimonio""],");

        private static ContentDatabase Content(string cases = CasesJson, string events = "[]")
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryWithUnions);
            content.AddCourtCasesJson("pack.prueba", "courts/prueba.json", cases);
            content.AddEventsJson("pack.prueba", "events/prueba.json", events);
            return content.Build();
        }

        private static void Run(World world, ContentDatabase content, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(world, content, world.Player, null, readOnly: false));

        private static Value Eval(World world, ContentDatabase content, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(world, content, world.Player, null, readOnly: true));

        private static void Play(ContentDatabase content, World world, int weeks) =>
            new WeekCycle().Register(new CourtSystem(content)).AdvanceWeeks(world, weeks);

        [Fact]
        public void AbrirCausa_EmpiezaEnLaPrimeraEtapa_ConUnRit()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);

            Run(world, content, "abrir_causa('causa.prueba.cobro')");

            CourtCase courtCase = Assert.Single(world.CourtCases);
            Assert.True(courtCase.IsOpen);
            Assert.Equal("demanda", Eval(world, content, "causa('causa.prueba.cobro').etapa").AsText("etapa"));
            Assert.Matches(@"^C-\d{4}-\d+$", courtCase.Rit);
        }

        [Fact]
        public void AbrirCausa_SiYaHayUnaAbierta_NoAbreOtra()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);

            Run(world, content, "abrir_causa('causa.prueba.cobro')");
            Run(world, content, "abrir_causa('causa.prueba.cobro')");

            Assert.Single(world.CourtCases);
        }

        [Fact]
        public void EtapaQueNoEspera_PasaSolaALaSiguiente_AlCumplirSuPlazo()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            Run(world, content, "abrir_causa('causa.prueba.cobro')");

            // El sistema corre al empezar cada semana: con 4 semanas de plazo, pasa en la quinta.
            Play(content, world, 4);
            Assert.Equal("demanda", Eval(world, content, "causa('causa.prueba.cobro').etapa").AsText("etapa"));

            Play(content, world, 1);
            Assert.Equal("audiencia", Eval(world, content, "causa('causa.prueba.cobro').etapa").AsText("etapa"));
        }

        [Fact]
        public void EtapaQueEspera_NoAvanzaSola_YCausaEnSoloSeCumpleConElPlazo()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            Run(world, content, "abrir_causa('causa.prueba.cobro')");
            Play(content, world, 4);

            Assert.False(Eval(world, content, "causa_en('causa.prueba.cobro', 'audiencia')").AsBool("causa_en"));
            Play(content, world, 2);
            Assert.True(Eval(world, content, "causa_en('causa.prueba.cobro', 'audiencia')").AsBool("causa_en"));

            Play(content, world, 20);
            Assert.Equal("audiencia", Eval(world, content, "causa('causa.prueba.cobro').etapa").AsText("etapa"));
        }

        [Fact]
        public void UltimaEtapaQueNoEspera_TerminaLaCausa_ComoTerminada()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            Run(world, content, "abrir_causa('causa.prueba.cobro')");
            Play(content, world, 6);

            Run(world, content, "causa('causa.prueba.cobro').avanzar()");
            Play(content, world, 4);

            Assert.False(Eval(world, content, "causa('causa.prueba.cobro').abierta").AsBool("abierta"));
            Assert.Equal(CourtSystem.FinishedOutcome, Eval(world, content, "causa('causa.prueba.cobro').resultado").AsText("resultado"));
        }

        [Fact]
        public void Cerrar_ConUnResultado_YDespuesSePuedeAbrirOtra()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            Run(world, content, "abrir_causa('causa.prueba.cobro')");

            Run(world, content, "causa('causa.prueba.cobro').cerrar('pago')");
            Assert.Equal("pago", Eval(world, content, "causa('causa.prueba.cobro').resultado").AsText("resultado"));

            Run(world, content, "abrir_causa('causa.prueba.cobro')");
            Assert.Equal(2, world.CourtCases.Count);
            Assert.True(Eval(world, content, "causa('causa.prueba.cobro').abierta").AsBool("abierta"));
        }

        [Fact]
        public void CausaQueTerminaConLaMuerteDeLaOtraParte_SeCierraSola()
        {
            ContentDatabase content = Content(@"[
              { ""id"": ""causa.prueba.divorcio"", ""tribunal"": ""tribunal.prueba.familia"", ""prefijo_rit"": ""C"", ""termina_si_muere_la_otra_parte"": true,
                ""etapas"": [ { ""id"": ""audiencia"", ""semanas"": 2, ""espera"": true } ], ""resultados"": [""divorcio"", ""fallecimiento""] }
            ]");
            World world = WorldFactory.CreateNewLife(1, content);
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            Run(world, content, "jugador.formar_pareja()");
            Character spouse = world.GetCharacter(world.Player.PartnerId!.Value);
            Run(world, content, "abrir_causa('causa.prueba.divorcio', jugador.pareja)");

            spouse.Die(world.Date, "muerte.prueba");
            Play(content, world, 1);

            Assert.Equal(CourtSystem.DeathOutcome, Eval(world, content, "causa('causa.prueba.divorcio').resultado").AsText("resultado"));
        }

        [Fact]
        public void CerrarConUnResultadoQueNoExiste_EsUnError()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            Run(world, content, "abrir_causa('causa.prueba.cobro')");

            Assert.Throws<ScriptException>(() => Run(world, content, "causa('causa.prueba.cobro').cerrar('ganada')"));
        }

        [Fact]
        public void CausasMalEscritas_SonErroresDelPaquete()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryWithUnions);
            content.AddCourtCasesJson("pack.prueba", "courts/prueba.json", CasesJson);
            content.AddCourtCasesJson("pack.base", "courts/base.json", @"[
              { ""id"": ""causa.sin_pais"", ""tribunal"": ""tribunal.x"", ""prefijo_rit"": ""C"", ""etapas"": [ { ""id"": ""a"", ""semanas"": 1 } ], ""resultados"": [""pago""] }
            ]");
            content.AddEventsJson("pack.prueba", "events/prueba.json", @"[
              { ""id"": ""evento.etapa_mala"", ""condiciones"": ""causa_en('causa.prueba.cobro', 'apelacion')"" },
              { ""id"": ""evento.causa_mala"", ""efectos"": [""abrir_causa('causa.prueba.no_existe')""] },
              { ""id"": ""evento.resultado_malo"", ""efectos"": [""causa('causa.prueba.cobro').cerrar('ganada')""] },
              { ""id"": ""evento.union_mala"", ""efectos"": [""jugador.unirse('union.prueba.no_existe')""] }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Source.Contains("causa.sin_pais") && e.Message.Contains("país"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("causa.sin_pais") && e.Message.Contains("terminada"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.etapa_mala") && e.Message.Contains("etapa"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.causa_mala") && e.Message.Contains("no existe"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.resultado_malo") && e.Message.Contains("resultado"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.union_mala") && e.Message.Contains("unión"));
        }

        [Fact]
        public void Unirse_QuedanUnidosLosDos_YAlSepararseSiguenUnidosAnteLaLey()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            Run(world, content, "jugador.formar_pareja()");
            Character partner = world.GetCharacter(world.Player.PartnerId!.Value);

            Run(world, content, "jugador.unirse('union.prueba.matrimonio')");
            Assert.Equal("union.prueba.matrimonio", Eval(world, content, "jugador.union").AsText("union"));
            Assert.Equal(world.PlayerId, partner.SpouseId);
            Assert.False(Eval(world, content, "jugador.separado").AsBool("separado"));

            Run(world, content, "jugador.terminar_pareja()");
            Assert.True(Eval(world, content, "jugador.separado").AsBool("separado"));
            Assert.Equal("union.prueba.matrimonio", Eval(world, content, "jugador.union").AsText("union"));

            Run(world, content, "jugador.terminar_union()");
            Assert.Equal(ValueKind.Null, Eval(world, content, "jugador.union").Kind);
            Assert.Null(partner.SpouseId);
        }

        [Fact]
        public void Unirse_ConAlguienYaUnidoConOtraPersona_EsUnError()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(1, content);
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            Run(world, content, "jugador.formar_pareja()");
            Run(world, content, "jugador.unirse('union.prueba.matrimonio')");
            Run(world, content, "jugador.terminar_pareja()");
            Run(world, content, "jugador.formar_pareja()");

            Assert.Throws<ScriptException>(() => Run(world, content, "jugador.unirse('union.prueba.matrimonio')"));
        }

        [Fact]
        public void Guardado_ConservaLasCausasYLaUnion()
        {
            ContentDatabase content = RepoPacks.Content;
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 25 * GameDate.WeeksPerYear);
            Run(world, content, "jugador.formar_pareja()");
            Run(world, content, "jugador.unirse('union.chile.matrimonio')");
            Run(world, content, "abrir_causa('causa.chile.divorcio', jugador.pareja)");
            Run(world, content, "causa('causa.chile.divorcio').monto = 120000");

            LifeSession loaded = LifeSession.Load(content, session.SaveToText());

            CourtCase courtCase = Assert.Single(loaded.World.CourtCases);
            Assert.Equal(world.CourtCases[0].Rit, courtCase.Rit);
            Assert.Equal(world.Player.PartnerId, courtCase.OtherPartyId);
            Assert.Equal(120000m, courtCase.Amount);
            Assert.Equal("union.chile.matrimonio", loaded.World.Player.UnionTypeId);
            Assert.Equal(session.SaveToText(), loaded.SaveToText());
        }

        [Fact]
        public void CarpetaDelJuzgado_MuestraElRitYLaLineaDelProceso()
        {
            LifeSession session = SessionInChile();
            Run(session.World, RepoPacks.Content, "abrir_causa('causa.chile.cobro_deuda')");

            CourtCaseView view = Assert.Single(new LifePresenter(session).CourtCases());

            Assert.Equal(session.World.CourtCases[0].Rit, view.Rit);
            Assert.Equal("Juzgado Civil", view.Court);
            Assert.True(view.IsOpen);
            Assert.True(view.Stages[0].Current);
            Assert.Equal(5, view.Stages.Count);
        }

        [Fact]
        public void SeparadosDeUnMatrimonio_QuedaLaSemillaConElConyugeDeTestigo()
        {
            ContentDatabase content = RepoPacks.Content;
            LifeSession session = SessionInChile();
            World world = session.World;
            new WeekCycle().AdvanceWeeks(world, 30 * GameDate.WeeksPerYear);
            Run(world, content, "jugador.formar_pareja()");
            int spouseId = world.Player.PartnerId!.Value;
            Run(world, content, "jugador.unirse('union.chile.matrimonio')");
            Run(world, content, "jugador.terminar_pareja()");

            EventDefinition separation = content.GetEvent("evento.chile.separacion_de_hecho");
            var scope = new EventScope(world, content, world.Player, null, readOnly: false);
            Assert.True(separation.CompiledConditions!.IsTrue(scope));
            foreach (Effect effect in separation.CompiledEffects)
            {
                effect.Apply(scope);
            }

            Assert.Contains(world.Seeds.OpenFor(world.PlayerId), s => s.TypeId == "semilla.chile.separados" && s.HasWitness(spouseId));
        }

        private static LifeSession SessionInChile()
        {
            for (ulong seed = 1; seed < 100; seed++)
            {
                var session = new LifeSession(RepoPacks.Content, seed);
                if (session.World.Player.Residence!.CountryId == "pais.chile")
                {
                    return session;
                }
            }

            throw new Xunit.Sdk.XunitException("Ninguna vida nació en Chile.");
        }
    }
}
