using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>R4b: cada año se cierra como un capítulo del diario, con título propio.</summary>
    public class ChapterTests
    {
        [Fact]
        public void Capitulo_LoTitulaLaLineaDelDiarioDeMasPeso()
        {
            LifeSession session = AtNextYear();
            int year = session.World.Date.Year;
            session.World.WriteJournal(session.World.PlayerId, "diario.muerte_madre");
            session.World.WriteJournal(session.World.PlayerId, "diario.colegio.secreto");

            ChapterView chapter = new LifePresenter(session).Chapter(year);

            Assert.Equal("El año en que murió mi mamá", chapter.Title);
            Assert.True(chapter.IsOpen);
            Assert.Contains(chapter.Months.SelectMany(m => m.Lines), l => l.StartsWith("Alguien del curso me contó un secreto"));
        }

        [Fact]
        public void Capitulo_UnTituloNoSeRepiteEnLaVida()
        {
            LifeSession session = AtNextYear();
            int first = session.World.Date.Year;
            session.World.WriteJournal(session.World.PlayerId, "diario.colegio.secreto");
            Advance(session, first + 1);
            session.World.WriteJournal(session.World.PlayerId, "diario.colegio.secreto");

            var presenter = new LifePresenter(session);

            Assert.Equal("El año del secreto", presenter.Chapter(first).Title);
            Assert.NotEqual("El año del secreto", presenter.Chapter(first + 1).Title);
        }

        [Fact]
        public void Capitulo_SinLineasQueLoTitulen_SeLlamaPorLaEdad()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            int birth = session.World.Date.Year;
            Advance(session, birth + 3);
            var presenter = new LifePresenter(session);
            ChapterView chapter = presenter.Chapter(birth + 2);

            Assert.True(
                chapter.Title == "El año en que cumplí 2" || RepoPacks.Content.ChapterTitles.Any(t => session.Text(t.TitleKey) == chapter.Title),
                $"Título inesperado: {chapter.Title}");
            Assert.Equal("El año en que nací", presenter.Chapter(birth).Title);
        }

        [Fact]
        public void AlPasarDeAno_ElCapituloCerradoSeEntregaUnaSolaVez()
        {
            var session = new LifeSession(RepoPacks.Content, 5);
            var presenter = new LifePresenter(session);
            int year = session.World.Date.Year;
            ChapterView? closed = null;
            for (int i = 0; i < 200 && closed == null; i++)
            {
                if (session.Pending != null)
                {
                    presenter.Choose(0);
                    continue;
                }

                presenter.AdvanceUntilSomething();
                closed = presenter.TakeClosedChapter();
            }

            Assert.NotNull(closed);
            Assert.Equal(year, closed!.Year);
            Assert.False(closed.IsOpen);
            Assert.Null(presenter.TakeClosedChapter());
        }

        [Fact]
        public void TituloDeCapitulo_SinSuLineaDelDiario_EsUnError()
        {
            var content = new ContentDatabase();
            content.AddChaptersJson("pack.prueba", "chapters/c.json", @"[ { ""diario"": ""diario.no_existe"", ""titulo"": ""capitulo.x"", ""peso"": 3 } ]");
            content.AddTextsJson("pack.prueba", "lang/es.json", "es", @"{ ""capitulo.x"": ""El año de algo"" }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("diario.no_existe"));
        }

        [Fact]
        public void VidaDePrueba_LaMayoriaDeLosAnosTieneTituloPropioYNingunoSeRepite()
        {
            var session = new LifeSession(RepoPacks.Content, 1);
            var decider = new AutoDecider(RepoPacks.Content);
            GameRandom random = new GameRandom(1).Fork("decisiones");
            int target = 30 * GameDate.WeeksPerYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                PendingEvent? pending = session.Pending;
                if (pending != null)
                {
                    session.Choose(decider.Choose(session.World, pending, random));
                    continue;
                }

                session.Advance(4);
            }

            var closed = new LifePresenter(session).Chapters().Where(c => !c.IsOpen).ToList();
            var titled = closed.Where(c => !c.Title.StartsWith("El año en que cumplí")).Select(c => c.Title).ToList();

            Assert.True(titled.Count * 2 > closed.Count, $"Solo {titled.Count} de {closed.Count} años tienen título propio.");
            Assert.Equal(titled.Count, titled.Distinct().Count());
        }

        /// <summary>Una vida recién nacida llevada a la primera semana del año siguiente.</summary>
        private static LifeSession AtNextYear()
        {
            var session = new LifeSession(RepoPacks.Content, 3);
            Advance(session, session.World.Date.Year + 1);
            return session;
        }

        /// <summary>Avanza (eligiendo la primera opción en cada momento) hasta llegar a ese año.</summary>
        private static void Advance(LifeSession session, int year)
        {
            while (session.World.Date.Year < year)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(1);
            }
        }
    }
}
