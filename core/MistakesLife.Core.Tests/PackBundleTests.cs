using System.IO;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class PackBundleTests
    {
        [Fact]
        public void Paquete_DesdeCarpeta_TieneTodosLosJsonConRutasRelativas()
        {
            PackBundle bundle = PackBundle.FromDirectory(RepoPacks.Directory);

            int jsonFiles = Directory.GetFiles(RepoPacks.Directory, "*.json", SearchOption.AllDirectories).Length;
            Assert.Equal(jsonFiles, bundle.Files.Count);
            Assert.Contains("base/pack.json", bundle.Files.Keys);
            Assert.Contains("countries/chile/country.json", bundle.Files.Keys);
            Assert.All(bundle.Files.Keys, k => Assert.DoesNotContain("\\", k));
        }

        [Fact]
        public void Paquete_GuardadoYLeido_QuedaIgual()
        {
            PackBundle bundle = PackBundle.FromDirectory(RepoPacks.Directory);

            PackBundle again = PackBundle.FromJson(bundle.ToJson());

            Assert.Equal(bundle.Files, again.Files);
            Assert.Equal(bundle.ToJson(), again.ToJson());
        }

        [Fact]
        public void Paquete_JsonRoto_DaUnErrorClaro()
        {
            Assert.Throws<InvalidDataException>(() => PackBundle.FromJson("{ esto no es json"));
        }

        [Fact]
        public void Paquete_CargaElMismoContenidoQueLaCarpeta()
        {
            ContentDatabase fromBundle = PackLoader.LoadBundle(PackBundle.FromJson(PackBundle.FromDirectory(RepoPacks.Directory).ToJson()));

            Assert.Equal(RepoPacks.Content.Events.Select(e => e.Id), fromBundle.Events.Select(e => e.Id));
            Assert.Equal(RepoPacks.Content.Text("diario.nacimiento"), fromBundle.Text("diario.nacimiento"));
        }

        [Fact]
        public void Paquete_MismaPartidaDesdeCarpetaYDesdePaquete_MismaVida()
        {
            ContentDatabase fromBundle = PackLoader.LoadBundle(PackBundle.FromDirectory(RepoPacks.Directory));

            Assert.Equal(Live(RepoPacks.Content, 42), Live(fromBundle, 42));
        }

        /// <summary>Vive 30 años eligiendo siempre la primera opción y devuelve el diario.</summary>
        private static string Live(ContentDatabase content, ulong seed)
        {
            var presenter = new LifePresenter(new LifeSession(content, seed));
            for (int step = 0; step < 300 && presenter.Snapshot().AgeInYears < 30; step++)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else if (presenter.Phase == LifePhase.Living)
                {
                    presenter.AdvanceUntilSomething();
                }
                else
                {
                    break;
                }
            }

            return string.Join("\n", presenter.FullJournal().Select(l => l.AgeLabel + ": " + l.Text));
        }
    }
}
