using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// El diario de noticias: lo que le pasa al país (temblores, temporales, crisis,
    /// elecciones). Una noticia no toca al jugador; lo que le pase lo deciden los
    /// eventos que la leen con noticia_reciente(), según lo que él eligió antes.
    /// </summary>
    public class NewsTests
    {
        private static string Country(string id) =>
            CountryTests.CountryJson.Replace("pais.prueba", id).Replace("ciudad.prueba", "ciudad." + id).Replace("comuna.", "comuna." + id + ".")
                .Replace("etapa.prueba.", "etapa." + id + ".");

        private const string NewsJson = @"[
          { ""id"": ""noticia.temblor"", ""tema"": ""sismo"", ""probabilidad_anual"": 1, ""enfriamiento_semanas"": 4,
            ""variantes"": [""noticia.temblor.2""] }
        ]";

        private static ContentDatabase TwoCountries(string news = NewsJson, string events = "[]")
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.aqui", "country.json", Country("pais.aqui"));
            content.AddCountryJson("pack.alla", "country.json", Country("pais.alla"));
            content.AddNewsJson("pack.aqui", "news/aqui.json", news);
            content.AddEventsJson("pack.aqui", "events/aqui.json", events);
            return content.Build();
        }

        private static World LifeIn(ContentDatabase content, string countryId)
        {
            for (ulong seed = 1; seed < 200; seed++)
            {
                World world = WorldFactory.CreateNewLife(seed, content);
                if (world.Player.Residence!.CountryId == countryId)
                {
                    return world;
                }
            }

            throw new Xunit.Sdk.XunitException("Ninguna vida nació en " + countryId);
        }

        private static void Play(ContentDatabase content, World world, int weeks)
        {
            var cycle = new WeekCycle()
                .Register(new NewsSystem(content))
                .Register(new EventSystem(content, new EventRules { WeeklyRandomEventChance = 0 }));
            cycle.AdvanceWeeks(world, weeks);
        }

        [Fact]
        public void Noticia_DelPaqueteDeUnPais_SaleSoloAhi()
        {
            ContentDatabase content = TwoCountries();
            World here = LifeIn(content, "pais.aqui");
            World there = LifeIn(content, "pais.alla");

            Play(content, here, 3);
            Play(content, there, 3);

            Assert.Single(here.News);
            Assert.Equal("pais.aqui", here.News[0].CountryId);
            Assert.Empty(there.News);
        }

        [Fact]
        public void Noticia_ConEnfriamiento_EsperaYVuelveConOtroTitular()
        {
            ContentDatabase content = TwoCountries();
            World world = LifeIn(content, "pais.aqui");

            Play(content, world, 9);

            // Semanas 0, 4 y 8: espera 4 semanas y alterna el titular.
            Assert.Equal(new[] { "noticia.temblor", "noticia.temblor.2", "noticia.temblor" }, world.News.Select(n => n.TextKey));
            Assert.Equal(new[] { 0, 4, 8 }, world.News.Select(n => n.Date.WeeksSince(world.News[0].Date)));
        }

        [Fact]
        public void EventoQueLeeLaNoticia_PasaSoloDespuesDeElla()
        {
            const string events = @"[
              { ""id"": ""evento.despues_del_temblor"", ""probabilidad_anual"": 1, ""unico"": true,
                ""condiciones"": ""noticia_reciente('noticia.temblor', 2)"", ""efectos"": [""jugador.marcar('sintio_temblor')""] }
            ]";
            const string lateNews = @"[
              { ""id"": ""noticia.temblor"", ""tema"": ""sismo"", ""probabilidad_anual"": 1, ""condiciones"": ""mundo.semanas_totales >= 999999"" }
            ]";
            ContentDatabase withNews = TwoCountries(NewsJson, events);
            ContentDatabase withoutNews = TwoCountries(lateNews, events);
            World a = LifeIn(withNews, "pais.aqui");
            World b = LifeIn(withoutNews, "pais.aqui");

            Play(withNews, a, 2);
            Play(withoutNews, b, 2);

            Assert.True(a.Player.HasFlag("sintio_temblor"));
            Assert.False(b.Player.HasFlag("sintio_temblor"));
        }

        [Fact]
        public void NoticiaReciente_SeVenceConLasSemanas()
        {
            const string events = @"[
              { ""id"": ""evento.tarde"", ""probabilidad_anual"": 1, ""unico"": true,
                ""condiciones"": ""!noticia_reciente('noticia.temblor', 3) && noticia_reciente('noticia.temblor', 30)"",
                ""efectos"": [""jugador.marcar('ya_paso_tiempo')""] }
            ]";
            const string onceNews = @"[ { ""id"": ""noticia.temblor"", ""tema"": ""sismo"", ""probabilidad_anual"": 1, ""enfriamiento_semanas"": 500 } ]";
            ContentDatabase content = TwoCountries(onceNews, events);
            World world = LifeIn(content, "pais.aqui");

            Play(content, world, 3);
            Assert.False(world.Player.HasFlag("ya_paso_tiempo"));
            Play(content, world, 2);
            Assert.True(world.Player.HasFlag("ya_paso_tiempo"));
        }

        [Fact]
        public void Noticia_QueTocaAlJugadorOEstaMalEscrita_SeRechaza()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.aqui", "country.json", Country("pais.aqui"));
            content.AddNewsJson("pack.aqui", "news/malas.json", @"[
              { ""id"": ""noticia.bien"", ""tema"": ""clima"", ""probabilidad_anual"": 0.5, ""ciudades"": [""ciudad.pais.aqui""] },
              { ""id"": ""noticia.lee_jugador"", ""tema"": ""clima"", ""probabilidad_anual"": 0.5, ""condiciones"": ""jugador.edad > 5"" },
              { ""id"": ""noticia.cambia_jugador"", ""tema"": ""clima"", ""probabilidad_anual"": 0.5, ""efectos"": [""jugador.felicidad -= 5""] },
              { ""id"": ""noticia.tema_malo"", ""tema"": ""farandula"", ""probabilidad_anual"": 0.5 },
              { ""id"": ""noticia.sin_azar"", ""tema"": ""clima"", ""probabilidad_anual"": 0 },
              { ""id"": ""noticia.ciudad_mala"", ""tema"": ""clima"", ""probabilidad_anual"": 0.5, ""ciudades"": [""ciudad.atlantida""] },
              { ""id"": ""mal.id"", ""tema"": ""clima"", ""probabilidad_anual"": 0.5 }
            ]");
            content.AddEventsJson("pack.aqui", "events/e.json", @"[
              { ""id"": ""evento.lee_noticia_falsa"", ""condiciones"": ""noticia_reciente('noticia.no_existe')"" }
            ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.DoesNotContain(ex.Errors, e => e.Source.Contains("noticia.bien"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("noticia.lee_jugador") && e.Message.Contains("jugador"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("noticia.cambia_jugador") && e.Message.Contains("solo cambia el país"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("noticia.tema_malo") && e.Message.Contains("tema"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("noticia.sin_azar") && e.Message.Contains("probabilidad_anual"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("noticia.ciudad_mala") && e.Message.Contains("ciudad.atlantida"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("mal.id") && e.Message.Contains("noticia."));
            Assert.Contains(ex.Errors, e => e.Source.Contains("evento.lee_noticia_falsa") && e.Message.Contains("esa noticia no existe"));
        }

        [Fact]
        public void PaquetesDelRepositorio_TraenNoticiasDeChileYArgentina()
        {
            ContentDatabase content = RepoPacks.Content;

            Assert.Contains(content.News, n => n.CountryId == "pais.chile" && n.Topic == "sismo");
            Assert.Contains(content.News, n => n.CountryId == "pais.argentina");
            Assert.Contains(content.News, n => n.CountryId == null && n.Id == "noticia.elecciones_presidenciales");
        }

        [Fact]
        public void VidaEnChile_TraeNoticiasDeTemporadaEnSuMes()
        {
            var session = new LifeSession(RepoPacks.Content, 11);
            int guard = 0;
            while (session.World.Date.TotalWeeks - session.World.Player.BirthDate.TotalWeeks < 10 * 52 && guard++ < 5000)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }
                else
                {
                    session.Advance(1);
                }
            }

            var news = session.World.News;
            Assert.Contains(news, n => n.NewsId == "noticia.chile.sistema_frontal");
            Assert.All(news.Where(n => n.NewsId == "noticia.chile.sistema_frontal"), n => Assert.InRange(n.Date.Month, 5, 8));
            Assert.All(news.Where(n => n.NewsId == "noticia.chile.fiestas_patrias"), n => Assert.Equal(9, n.Date.Month));
            Assert.All(news.Where(n => n.NewsId == "noticia.elecciones_presidenciales"), n => Assert.Equal(11, n.Date.Month));
            Assert.All(news.Where(n => n.NewsId == "noticia.chile.sistema_frontal"), n => Assert.Equal("ciudad.santiago", n.CityId));
        }

        [Fact]
        public void Guardar_ConservaElDiarioDeNoticias()
        {
            var session = new LifeSession(RepoPacks.Content, 11);
            for (int i = 0; i < 300; i++)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                }
                else
                {
                    session.Advance(1);
                }
            }

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.NotEmpty(session.World.News);
            Assert.Equal(
                session.World.News.Select(n => $"{n.Date.TotalWeeks}|{n.NewsId}|{n.CountryId}|{n.CityId}|{n.TextKey}"),
                loaded.World.News.Select(n => $"{n.Date.TotalWeeks}|{n.NewsId}|{n.CountryId}|{n.CityId}|{n.TextKey}"));
        }

        [Fact]
        public void Pantalla_MuestraLasNoticiasDeLaMasNuevaALaMasVieja()
        {
            var presenter = new LifePresenter(new LifeSession(RepoPacks.Content, 11));
            for (int i = 0; i < 200; i++)
            {
                if (presenter.Session.Pending != null)
                {
                    presenter.Session.Choose(presenter.Session.Pending.AvailableOptions[0]);
                }
                else
                {
                    presenter.Session.Advance(1);
                }
            }

            var news = presenter.News(5);
            var fresh = presenter.TakeNewNews();

            Assert.InRange(news.Count, 1, 5);
            Assert.True(news.Zip(news.Skip(1), (a, b) => a.WeeksAgo <= b.WeeksAgo).All(x => x));
            Assert.All(news, n => Assert.False(string.IsNullOrWhiteSpace(n.Headline)));
            Assert.All(news, n => Assert.Contains(n.Topic, NewsDefinition.Topics));
            Assert.Equal(news[0].Headline, fresh[fresh.Count - 1].Headline);
            Assert.Empty(presenter.TakeNewNews());
        }
    }
}
