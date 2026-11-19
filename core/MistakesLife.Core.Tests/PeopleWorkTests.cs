using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// En qué anda la gente que nadie juega (D4a2): trabaja, busca pega, estudia o está
    /// jubilada en la parte real de su edad, y solo la parte real de los ocupados tiene una
    /// de las pegas que existen enteras en el juego.
    /// </summary>
    public class PeopleWorkTests
    {
        private static PeopleWorkDefinition Chile => RepoPacks.Content.GetCountry("pais.chile").PeopleWork!;

        /// <summary>Una partida en Chile con el jugador de 30 años.</summary>
        private static LifeSession Session()
        {
            var session = new LifeSession(RepoPacks.Content, 7, "pais.chile");
            session.Advance(30 * GameDate.WeeksPerYear);
            return session;
        }

        /// <summary>Mira a muchas personas adultas nuevas como si tuvieran esa edad, cada una con su propio azar.</summary>
        private static List<Character> LookAtMany(LifeSession session, int count, int age)
        {
            var people = new List<Character>();
            for (int i = 0; i < count; i++)
            {
                // Una persona adulta cualquiera (la pareja que se crea para el jugador, que se suelta altiro).
                Character person = FamilyFactory.CreatePartner(session.World, session.Content, session.World.Player);
                session.World.Player.PartnerId = null;
                person.PartnerId = null;
                PeopleWorkSystem.Look(session.World, session.Content, Chile, person, age, new GameRandom((ulong)(1000 + i)));
                people.Add(person);
            }

            return people;
        }

        [Fact]
        public void ALos40_TrabajaYBuscaPega_LaParteRealDeChile()
        {
            List<Character> people = LookAtMany(Session(), 3000, 40);
            PeopleWorkAgeDefinition group = Chile.AgeGroup(40)!;

            double working = people.Count(p => p.WorkStatus == WorkStatus.Working) / 3000.0;
            double looking = people.Count(p => p.WorkStatus == WorkStatus.Unemployed) / 3000.0;

            Assert.InRange(working, group.Employed - 0.03, group.Employed + 0.03);
            Assert.InRange(looking, group.Unemployed - 0.02, group.Unemployed + 0.02);
            Assert.DoesNotContain(people, p => p.WorkStatus == WorkStatus.Retired);
        }

        [Fact]
        public void SoloLaParteRealDeLosQueTrabajan_TieneUnaPegaDelJuego_YSoloDeLasQueExisten()
        {
            List<Character> working = LookAtMany(Session(), 3000, 40).Where(p => p.WorkStatus == WorkStatus.Working).ToList();
            var allowed = new HashSet<string>(Chile.Jobs.Select(j => j.CareerId));
            double total = Chile.Jobs.Sum(j => j.Share);

            List<Character> withJob = working.Where(p => p.Job != null).ToList();
            double share = withJob.Count / (double)working.Count;

            Assert.InRange(share, total - 0.03, total + 0.03);
            Assert.All(withJob, p =>
            {
                Assert.NotNull(p.Job!.Position);
                Assert.Contains(RepoPacks.Content.GetCareerArea(p.Job.Position!.AreaId).CareerId, allowed);
            });
        }

        [Fact]
        public void QuienTrabajaEnUnaCarrera_TieneSuTitulo_YUnaEmpresaDeEsaCarrera()
        {
            LifeSession session = Session();
            List<Character> professionals = LookAtMany(session, 3000, 40)
                .Where(p => p.Job?.Position != null && !RepoPacks.Content.GetCareer(RepoPacks.Content.GetCareerArea(p.Job.Position!.AreaId).CareerId).IsOccupation)
                .ToList();

            Assert.NotEmpty(professionals);
            Assert.All(professionals, p =>
            {
                string careerId = RepoPacks.Content.GetCareerArea(p.Job!.Position!.AreaId).CareerId;
                Assert.Contains(p.Degrees, d => RepoPacks.Content.GetCareerLevel(d).CareerId == careerId);
                Assert.Equal(careerId, session.World.CompanyById(p.Job.CompanyId)!.CareerId);
            });
        }

        [Fact]
        public void ALos70_QuienNoTrabajaNiBusca_EstaJubilado()
        {
            List<Character> people = LookAtMany(Session(), 500, 70);

            Assert.Contains(people, p => p.WorkStatus == WorkStatus.Retired);
            Assert.All(people.Where(p => p.WorkStatus != WorkStatus.Working && p.WorkStatus != WorkStatus.Unemployed), p => Assert.Equal(WorkStatus.Retired, p.WorkStatus));
            Assert.DoesNotContain(people, p => p.WorkStatus == WorkStatus.Studying);
        }

        [Fact]
        public void ALos20_EstudianMuchos_YNadieEstaJubilado()
        {
            List<Character> people = LookAtMany(Session(), 2000, 20);
            double studying = people.Count(p => p.WorkStatus == WorkStatus.Studying) / 2000.0;

            Assert.InRange(studying, 0.3, Chile.HigherEducation!.Share + 0.03);
            Assert.DoesNotContain(people, p => p.WorkStatus == WorkStatus.Retired);
        }

        [Fact]
        public void QuienTuvoUnaPegaDelJuego_AlVolverATrabajar_VuelveASuOficio()
        {
            LifeSession session = Session();
            Character person = LookAtMany(session, 3000, 40).First(p => p.Job?.Position != null);
            string careerId = RepoPacks.Content.GetCareerArea(person.Job!.Position!.AreaId).CareerId;
            person.Job = null;
            person.WorkStatus = WorkStatus.Unemployed;

            for (ulong seed = 1; person.WorkStatus != WorkStatus.Working; seed++)
            {
                PeopleWorkSystem.Look(session.World, session.Content, Chile, person, 40, new GameRandom(seed));
            }

            Assert.NotNull(person.Job);
            Assert.Equal(careerId, RepoPacks.Content.GetCareerArea(person.Job!.Position!.AreaId).CareerId);
        }

        [Fact]
        public void EnUnaVida_ElJugadorYSuEquipo_NoSeMiran_YLosPapasSi()
        {
            LifeSession session = Session();
            Character player = session.World.Player;
            Character mother = session.World.GetCharacter(player.MotherId!.Value);

            Assert.Null(player.WorkStatus);
            Assert.NotNull(mother.WorkStatus);
            if (Work.WorkRules.InTeam(player))
            {
                Assert.All(Work.WorkRules.Colleagues(session.World, player), c => Assert.Null(c.WorkStatus));
            }
        }

        [Fact]
        public void LaFicha_DiceLaPegaYLaEmpresa_DeQuienTrabajaEnUna()
        {
            LifeSession session = Session();
            Character mother = session.World.GetCharacter(session.World.Player.MotherId!.Value);
            for (ulong seed = 1; mother.Job == null; seed++)
            {
                mother.WorkStatus = null;
                PeopleWorkSystem.Look(session.World, session.Content, Chile, mother, 45, new GameRandom(seed));
            }

            var presenter = new LifePresenter(session);
            string life = presenter.PersonCard(mother.Id)!.Life;
            string careerId = RepoPacks.Content.GetCareerArea(mother.Job.Position!.AreaId).CareerId;
            string g = mother.Gender == Gender.Female ? ".f" : ".m";

            Assert.StartsWith(presenter.Text(careerId + ".persona" + g), life);
            Assert.Contains(presenter.Text(session.World.CompanyById(mother.Job.CompanyId)!.NameId!), life);
        }

        [Fact]
        public void LaFicha_DeQuienBuscaPegaOEstaJubilado_DiceSoloEso()
        {
            LifeSession session = Session();
            Character mother = session.World.GetCharacter(session.World.Player.MotherId!.Value);
            var presenter = new LifePresenter(session);

            mother.Job = null;
            mother.WorkStatus = WorkStatus.Unemployed;
            Assert.Equal(presenter.Text("ui.gente.vida.busca_pega"), presenter.PersonCard(mother.Id)!.Life);

            mother.WorkStatus = WorkStatus.Retired;
            Assert.Equal(presenter.Text(mother.Gender == Gender.Female ? "ui.gente.vida.jubilado.f" : "ui.gente.vida.jubilado.m"), presenter.PersonCard(mother.Id)!.Life);

            mother.WorkStatus = WorkStatus.Working;
            Assert.Equal(presenter.Text("ui.gente.vida.trabaja"), presenter.PersonCard(mother.Id)!.Life);

            mother.WorkStatus = WorkStatus.Inactive;
            Assert.Equal(string.Empty, presenter.PersonCard(mother.Id)!.Life);
        }

        [Fact]
        public void AlGuardarYCargar_SeMantieneEnQueAndaCadaUno()
        {
            LifeSession session = Session();
            var before = session.World.Characters.ToDictionary(c => c.Id, c => c.WorkStatus);

            LifeSession loaded = LifeSession.LoadFromBytes(RepoPacks.Content, session.SaveToBytes());

            Assert.All(loaded.World.Characters, c => Assert.Equal(before[c.Id], c.WorkStatus));
        }

        [Fact]
        public void UnaPegaQueNoExisteEnElJuego_EsUnErrorDeContenido()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", CountryTests.CountryJson.TrimEnd().TrimEnd('}')
                + @", ""trabajo_de_la_gente"": { ""edades"": [ { ""desde"": 15, ""hasta"": 120, ""ocupados"": 0.5, ""desocupacion"": 0.1 } ], ""pegas"": [ { ""carrera"": ""oficio.astronauta"", ""parte"": 0.01 } ] } }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());
            Assert.Contains(ex.Errors, e => e.Source.Contains("trabajo_de_la_gente") && e.Message.Contains("oficio.astronauta"));
        }
    }
}
