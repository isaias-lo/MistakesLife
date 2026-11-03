using System;
using System.IO;
using MistakesLife.Core.Content;
using MistakesLife.UnityExport;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class UnityExportTests
    {
        [Fact]
        public void Exportar_CopiaElNucleoYElArchivoDePaquetes()
        {
            string unity = Path.Combine(Path.GetTempPath(), "mistakeslife-unity-" + Guid.NewGuid().ToString("N"));
            try
            {
                UnityExporter.Export(RepoPacks.Directory, unity);

                Assert.True(File.Exists(Path.Combine(unity, UnityExporter.CoreDllPath)));
                string bundle = File.ReadAllText(Path.Combine(unity, UnityExporter.BundlePath));
                Assert.Equal(PackBundle.FromDirectory(RepoPacks.Directory).ToJson(), bundle);
                Assert.NotEmpty(PackLoader.LoadBundle(PackBundle.FromJson(bundle)).Events);
            }
            finally
            {
                if (Directory.Exists(unity))
                {
                    Directory.Delete(unity, recursive: true);
                }
            }
        }
    }
}
