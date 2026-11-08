using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Objetos de la escena 3D que aparecen según la vida del jugador (paquetes, carpeta scene/).</summary>
    public class SceneObjectTests
    {
        private static LifePresenter NewGame(ulong seed) => new LifePresenter(new LifeSession(RepoPacks.Content, seed));

        [Fact]
        public void PaqueteBase_TraeObjetosDeLaCasa()
        {
            var ids = RepoPacks.Content.SceneObjects.Select(o => o.Id).ToList();

            Assert.Contains("escena.casa.libros", ids);
            Assert.Contains("escena.casa.cuentas", ids);
            Assert.All(RepoPacks.Content.SceneObjects, o => Assert.NotNull(o.CompiledConditions));
        }

        [Fact]
        public void Objeto_ConLugarMaloIdMaloOCondicionMala_SeRechaza()
        {
            var content = new ContentDatabase();
            content.AddSceneJson("pack.prueba", "scene/prueba.json", @"[
              { ""id"": ""escena.casa.bien"", ""lugar"": ""casa"", ""condiciones"": ""jugador.edad > 5"" },
              { ""id"": ""escena.luna.cohete"", ""lugar"": ""luna"", ""condiciones"": ""jugador.edad > 5"" },
              { ""id"": ""escena.barrio.mal"", ""lugar"": ""casa"", ""condiciones"": ""jugador.edad > 5"" },
              { ""id"": ""escena.casa.rota"", ""lugar"": ""casa"", ""condiciones"": ""jugador.edad >"" },
              { ""id"": ""escena.casa.vacia"", ""lugar"": ""casa"", ""condiciones"": "" "" } ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.DoesNotContain(ex.Errors, e => e.Source.Contains("escena.casa.bien"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("escena.luna.cohete") && e.Message.Contains("lugar"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("escena.barrio.mal") && e.Message.Contains("escena.casa."));
            Assert.Contains(ex.Errors, e => e.Source.Contains("escena.casa.rota"));
            Assert.Contains(ex.Errors, e => e.Source.Contains("escena.casa.vacia") && e.Message.Contains("condición"));
        }

        [Fact]
        public void Objeto_Repetido_SeRechaza()
        {
            var content = new ContentDatabase();
            content.AddSceneJson("pack.prueba", "scene/a.json", @"[ { ""id"": ""escena.casa.x"", ""lugar"": ""casa"", ""condiciones"": ""true"" } ]");
            content.AddSceneJson("pack.prueba", "scene/b.json", @"[ { ""id"": ""escena.casa.x"", ""lugar"": ""casa"", ""condiciones"": ""true"" } ]");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("repetido"));
        }

        [Fact]
        public void Casa_CambiaConLaSaludYLaFelicidad()
        {
            LifePresenter presenter = NewGame(7);
            Character player = presenter.Session.World.Player;

            player.Stats.Health = 20;
            player.Stats.Happiness = 10;
            var sad = presenter.Snapshot().Home.SceneObjects;
            player.Stats.Health = 90;
            player.Stats.Happiness = 95;
            var happy = presenter.Snapshot().Home.SceneObjects;

            Assert.Contains("escena.casa.remedios", sad);
            Assert.Contains("escena.casa.desorden", sad);
            Assert.DoesNotContain("escena.casa.flores", sad);
            Assert.Contains("escena.casa.flores", happy);
            Assert.DoesNotContain("escena.casa.remedios", happy);
        }

        [Fact]
        public void Casa_ConObjetosDistintos_TieneOtraFirma()
        {
            LifePresenter presenter = NewGame(7);
            Character player = presenter.Session.World.Player;

            player.Stats.Happiness = 50;
            string before = presenter.Snapshot().Home.Signature;
            player.Stats.Happiness = 95;
            string after = presenter.Snapshot().Home.Signature;

            Assert.NotEqual(before, after);
        }

        [Fact]
        public void Vida_AlAvanzar_LaCasaVaCambiando()
        {
            LifePresenter presenter = NewGame(42);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int step = 0; step < 200 && presenter.Phase != LifePhase.Over && presenter.Phase != LifePhase.ChoosingHeir; step++)
            {
                seen.UnionWith(presenter.Snapshot().Home.SceneObjects);
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceUntilSomething();
                }
            }

            Assert.Contains("escena.casa.mochila", seen);
            Assert.True(seen.Count >= 4, "en una vida larga la casa debería mostrar varios objetos distintos: " + string.Join(", ", seen));
        }

        [Fact]
        public void MirarLaCasa_NoCambiaLaPartida()
        {
            LifePresenter looked = NewGame(11);
            LifePresenter notLooked = NewGame(11);
            for (int i = 0; i < 30; i++)
            {
                looked.Snapshot();
                Step(looked);
                Step(notLooked);
            }

            Assert.Equal(notLooked.Session.SaveToText(), looked.Session.SaveToText());
        }

        private static void Step(LifePresenter presenter)
        {
            if (presenter.Phase == LifePhase.Choosing)
            {
                presenter.Choose(0);
            }
            else if (presenter.Phase == LifePhase.Living)
            {
                presenter.AdvanceUntilSomething();
            }
        }
    }
}
