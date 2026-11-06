using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class CompanyTests
    {
        private const string Desarrollo = "carrera.software.area.desarrollo";
        private const string Senior = "carrera.software.cargo.senior";
        private const string LiderTecnico = "carrera.software.cargo.lider_tecnico";
        private const string Micro = "empresa.tamano.micro";
        private const string Pequena = "empresa.tamano.pequena";
        private const string Grande = "empresa.tamano.grande";
        private const string Instituto = "carrera.software.nivel.analista_programador";
        private const string ChileCountry = "chile/country.json";

        private static void Run(LifeSession session, string effect) =>
            Effect.Parse(effect).Apply(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false));

        private static Value Eval(LifeSession session, string expression) =>
            Expression.Parse(expression).Evaluate(new EventScope(session.World, session.Content, session.World.Player, null, readOnly: true));

        /// <summary>Una partida cuyo jugador se tituló de software y entró a desarrollo.</summary>
        private static LifeSession Hired(ContentDatabase content, ulong seed)
        {
            var session = new LifeSession(content, seed);
            session.TimeMachine.Enabled = false;
            Run(session, $"estudiar('{Instituto}')");
            Run(session, "jugador.titularse()");
            Run(session, "jugador.marcar('trabajando')");
            Run(session, $"jugador.trabajar_en('{Desarrollo}')");
            return session;
        }

        /// <summary>La primera partida (desde la semilla 1) cuyo jugador llegó a una empresa de ese tamaño.</summary>
        private static LifeSession HiredIn(ContentDatabase content, string sizeId, Func<LifeSession, bool>? before = null)
        {
            for (ulong seed = 1; seed < 300; seed++)
            {
                var session = new LifeSession(content, seed);
                session.TimeMachine.Enabled = false;
                Run(session, $"estudiar('{Instituto}')");
                Run(session, "jugador.titularse()");
                Run(session, "jugador.marcar('trabajando')");
                if (before != null && !before(session))
                {
                    continue;
                }

                Run(session, $"jugador.trabajar_en('{Desarrollo}')");
                if (Company(session)?.SizeId == sizeId)
                {
                    return session;
                }
            }

            throw new InvalidOperationException("Ninguna partida llegó a una empresa de ese tamaño.");
        }

        private static Company? Company(LifeSession session) => session.World.CompanyById(session.World.Player.Job?.CompanyId);

        /// <summary>Los paquetes reales, con un cambio en el country.json de Chile.</summary>
        private static ContentDatabase PatchedChile(Func<string, string> patch)
        {
            PackBundle real = PackBundle.FromDirectory(RepoPacks.Directory);
            var files = real.Files.ToDictionary(f => f.Key, f => f.Key.EndsWith(ChileCountry, StringComparison.Ordinal) ? patch(f.Value) : f.Value);
            return PackLoader.LoadBundle(new PackBundle(files));
        }

        private static string Set(string json, string field, string value) =>
            Regex.Replace(json, "\"" + field + "\": [0-9.]+", "\"" + field + "\": " + value);

        /// <summary>Avanza hasta pasar la primera semana del año siguiente (cuando viven las empresas), sin responder eventos.</summary>
        private static void AdvanceToNextYear(LifeSession session)
        {
            int weeks = GameDate.WeeksPerYear - session.World.Date.WeekOfYear + 1;
            session.World.Player.Stats.Health = 100;
            session.Advance(weeks);
        }

        [Fact]
        public void TrabajarEnSuCarrera_LlegaAUnaEmpresaDeEsaCarreraConTamanoYNombre()
        {
            LifeSession session = Hired(RepoPacks.Content, 1);

            Company company = Assert.IsType<Company>(Company(session));
            CareerDefinition software = RepoPacks.Content.GetCareer("carrera.software");
            Assert.Equal(software.Id, company.CareerId);
            Assert.Contains(company.NameId, software.CompanyNames);
            Assert.True(company.IsOpen);
            Assert.Equal(company.SizeId, Eval(session, "jugador.empresa_tamano").AsText("prueba"));
            Assert.NotEqual(company.SizeId, company.NameId);
        }

        [Fact]
        public void LosTamanos_SeRepartenComoDiceElPais()
        {
            var counts = new Dictionary<string, int>();
            const int lives = 400;
            for (ulong seed = 1; seed <= lives; seed++)
            {
                string size = Company(Hired(RepoPacks.Content, seed))!.SizeId;
                counts[size] = counts.TryGetValue(size, out int n) ? n + 1 : 1;
            }

            CompaniesDefinition chile = RepoPacks.Content.GetCountry("pais.chile").Economy!.Companies!;
            double total = chile.Sizes.Sum(s => s.Weight);
            foreach (CompanySizeDefinition size in chile.Sizes)
            {
                double share = counts.TryGetValue(size.Id, out int n) ? (double)n / lives : 0;
                Assert.InRange(share, (size.Weight / total) - 0.08, (size.Weight / total) + 0.08);
            }
        }

        [Fact]
        public void EmpresaGrande_PagaMasQueUnaMicroPorElMismoCargo()
        {
            decimal micro = HiredIn(RepoPacks.Content, Micro).World.Player.Job!.BaseSalary;
            decimal grande = HiredIn(RepoPacks.Content, Grande).World.Player.Job!.BaseSalary;
            Assert.True(grande > micro * 1.3m, $"Grande {grande}, micro {micro}.");
        }

        [Fact]
        public void CambiarDeContratoEnLaMismaArea_SigueEnLaMismaEmpresa()
        {
            LifeSession session = Hired(RepoPacks.Content, 3);
            int company = session.World.Player.Job!.CompanyId!.Value;

            Run(session, "jugador.trabajar_en(jugador.area, 'honorarios')");

            Assert.Equal(company, session.World.Player.Job!.CompanyId);
            Assert.Equal(JobKind.Freelance, session.World.Player.Job.Kind);
        }

        [Fact]
        public void EnUnaMicro_NoExistenLosCargosAltos_YSePuedeCambiarAUnaMasGrande()
        {
            LifeSession session = HiredIn(RepoPacks.Content, Micro);
            for (int step = 0; step < 2; step++)
            {
                for (int month = 0; month < 60; month++)
                {
                    CareerLadder.AdvanceMonth(session.World.Player);
                }

                Run(session, "jugador.desempeno = 95");
                Run(session, "jugador.ascender()");
            }

            for (int month = 0; month < 60; month++)
            {
                CareerLadder.AdvanceMonth(session.World.Player);
            }

            Run(session, "jugador.desempeno = 95");
            Assert.Equal(Senior, session.World.Player.Job!.Position!.PositionId);
            Assert.False(Eval(session, "jugador.puede_ascender").AsBool("prueba"));
            Assert.True(Eval(session, "jugador.tope_de_empresa").AsBool("prueba"));
            Assert.Equal(Pequena, Eval(session, "jugador.empresa_mayor").AsText("prueba"));
            decimal before = session.World.Player.Job.BaseSalary;
            int oldCompany = session.World.Player.Job.CompanyId!.Value;

            Run(session, "jugador.cambiar_de_empresa()");

            Assert.Equal(Pequena, Company(session)!.SizeId);
            Assert.NotEqual(oldCompany, session.World.Player.Job!.CompanyId);
            Assert.Equal(Senior, session.World.Player.Job.Position!.PositionId);
            Assert.True(session.World.Player.Job.BaseSalary > before);
            Assert.True(Eval(session, "jugador.puede_ascender").AsBool("prueba"));
        }

        [Fact]
        public void ConMuchaExperiencia_EnUnaMicro_EntraEnElCargoMasAltoQueExiste()
        {
            // Llega con experiencia de líder técnico (de su trabajo anterior) a una microempresa.
            LifeSession session = HiredIn(RepoPacks.Content, Micro, s =>
            {
                Run(s, $"jugador.trabajar_en('{Desarrollo}')");
                for (int i = 0; i < 3; i++)
                {
                    for (int month = 0; month < 60; month++)
                    {
                        CareerLadder.AdvanceMonth(s.World.Player);
                    }

                    Run(s, "jugador.desempeno = 95");
                    if (!Eval(s, "jugador.puede_ascender").AsBool("prueba"))
                    {
                        return false;
                    }

                    Run(s, Eval(s, "jugador.caminos_ascenso").AsNumber("prueba") > 1 ? $"jugador.ascender('{LiderTecnico}')" : "jugador.ascender()");
                }

                if (s.World.Player.Job!.Position!.PositionId != LiderTecnico)
                {
                    return false;
                }

                // Deja ese trabajo: vuelve a buscar con la experiencia de líder técnico.
                Run(s, "jugador.dejar_trabajo()");
                return true;
            });

            Assert.Equal(Senior, session.World.Player.Job!.Position!.PositionId);
        }

        [Fact]
        public void SiQuiebraLaEmpresaDelJugador_SeDetieneElTiempo_YElEventoLoDejaSinTrabajo()
        {
            ContentDatabase content = PatchedChile(json => Set(json, "quiebra_anual", "1"));
            LifeSession session = Hired(content, 2);
            Company company = Company(session)!;

            AdvanceToNextYear(session);
            while (session.Pending != null && session.Pending.EventId != "evento.empresa.quiebra")
            {
                session.Choose(session.Pending.AvailableOptions[0]);
                session.Advance(1);
            }

            Assert.False(company.IsOpen);
            Assert.Contains(session.World.Journal, j => j.TextKey == CompanySystem.BankruptcyJournal && j.CharacterId == session.World.PlayerId);
            Assert.Equal("evento.empresa.quiebra", session.Pending!.EventId);
            Assert.True(Eval(session, "jugador.empresa_quebro").AsBool("prueba"));

            session.Choose(session.Pending.AvailableOptions[0]);

            Assert.Null(session.World.Player.Job);
            Assert.True(session.World.Player.HasFlag("buscando_trabajo"));
            Assert.Equal(Desarrollo, session.World.Player.LastPosition!.AreaId);
        }

        [Fact]
        public void LasEmpresasCrecen_YLoCuentaElDiario()
        {
            ContentDatabase content = PatchedChile(json => Set(Set(json, "quiebra_anual", "0"), "crece_anual", "1"));
            LifeSession session = HiredIn(content, Micro);

            AdvanceToNextYear(session);

            Assert.Equal(Pequena, Company(session)!.SizeId);
            Assert.Contains(session.World.Journal, j => j.TextKey == CompanySystem.GrowthJournal);
        }

        [Fact]
        public void EnRecesionYCrisis_QuiebranMasEmpresas()
        {
            EconomicCycleDefinition cycle = RepoPacks.Content.GetCountry("pais.chile").Economy!.Cycle!;
            Assert.Equal(1, cycle.Phase("economia.ciclo.normal")!.BankruptcyFactor);
            Assert.True(cycle.Phase("economia.ciclo.recesion")!.BankruptcyFactor > 1);
            Assert.True(cycle.Phase("economia.ciclo.crisis")!.BankruptcyFactor > cycle.Phase("economia.ciclo.recesion")!.BankruptcyFactor);
        }

        [Fact]
        public void Guardar_ConservaLasEmpresasYDondeTrabajaCadaUno()
        {
            LifeSession session = Hired(RepoPacks.Content, 4);
            session.Advance(GameDate.WeeksPerYear);
            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.Equal(session.World.Companies.Select(c => (c.Id, c.SizeId, c.NameId, c.IsOpen)), loaded.World.Companies.Select(c => (c.Id, c.SizeId, c.NameId, c.IsOpen)));
            Assert.Equal(session.World.Player.Job?.CompanyId, loaded.World.Player.Job?.CompanyId);
            Assert.Equal(session.SaveToText(), loaded.SaveToText());
        }

        [Fact]
        public void Cargar_UnGuardadoDeLaVersion5_TrabajosSinEmpresaSiguenIgual()
        {
            LifeSession session = Hired(RepoPacks.Content, 4);
            var json = Newtonsoft.Json.Linq.JObject.Parse(session.SaveToText());
            json["version"] = 5;
            var world = (Newtonsoft.Json.Linq.JObject)json["world"]!;
            world.Remove("companies");
            world.Remove("nextCompanyId");
            foreach (var job in json.SelectTokens("world.characters[*].job").OfType<Newtonsoft.Json.Linq.JObject>())
            {
                job.Remove("companyId");
            }

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, json.ToString());

            Assert.Empty(loaded.World.Companies);
            Assert.Null(loaded.World.Player.Job!.CompanyId);
            Assert.True(Eval(loaded, "jugador.empresa_tamano").Kind == ValueKind.Null);
            Assert.Equal(session.World.Player.Job!.BaseSalary, loaded.World.Player.Job.BaseSalary);
            loaded.Advance(GameDate.WeeksPerYear);
        }

        [Fact]
        public void UnPaisSinEmpresas_DaTrabajosSinEmpresa()
        {
            ContentDatabase content = PatchedChile(json => Regex.Replace(json, ",\\s*\"empresas\": \\{[^{}]*\"tamanos\": \\[[^\\]]*\\]\\s*\\}", string.Empty));
            Assert.Null(content.GetCountry("pais.chile").Economy!.Companies);
            LifeSession session = Hired(content, 1);
            Assert.Null(session.World.Player.Job!.CompanyId);
            Assert.True(Eval(session, "jugador.puede_ascender").Kind == ValueKind.Bool);
        }

        [Fact]
        public void TamanoMalEscrito_EsUnErrorDelPaquete()
        {
            Assert.Throws<ContentLoadException>(() => PatchedChile(json => json.Replace("\"empresa.tamano.micro\"", "\"tamano.micro\"")));
            Assert.Throws<ContentLoadException>(() => PatchedChile(json => json.Replace("\"cargo_maximo\": 2", "\"cargo_maximo\": -1")));
        }
    }
}
