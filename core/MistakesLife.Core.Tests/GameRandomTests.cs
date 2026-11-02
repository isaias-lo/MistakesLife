using System;
using System.Linq;
using MistakesLife.Core.Randomness;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class GameRandomTests
    {
        [Fact]
        public void MismaSemilla_DaLaMismaSecuencia()
        {
            var a = new GameRandom(42);
            var b = new GameRandom(42);
            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(a.NextULong(), b.NextULong());
            }
        }

        [Fact]
        public void SemillasDistintas_DanSecuenciasDistintas()
        {
            var a = new GameRandom(1);
            var b = new GameRandom(2);
            var seqA = Enumerable.Range(0, 10).Select(_ => a.NextULong()).ToArray();
            var seqB = Enumerable.Range(0, 10).Select(_ => b.NextULong()).ToArray();
            Assert.NotEqual(seqA, seqB);
        }

        [Fact]
        public void PrimerValor_EsFijoEntreVersiones()
        {
            // Si esta prueba falla, cambió el algoritmo y las partidas guardadas
            // dejarían de repetirse igual. No cambiar sin una migración.
            // 0xE220A8397B1DCDAF es el valor de referencia de SplitMix64 con semilla 0.
            Assert.Equal(0xE220A8397B1DCDAFUL, new GameRandom(0).NextULong());
        }

        [Fact]
        public void EstadoGuardado_ContinuaLaMismaSecuencia()
        {
            var original = new GameRandom(7);
            original.NextULong();
            var restored = GameRandom.FromState(original.State);
            Assert.Equal(original.NextULong(), restored.NextULong());
        }

        [Fact]
        public void NextInt_QuedaDentroDelRango()
        {
            var random = new GameRandom(3);
            for (int i = 0; i < 10000; i++)
            {
                int value = random.NextInt(5, 10);
                Assert.InRange(value, 5, 9);
            }
        }

        [Fact]
        public void NextInt_ConMaximoInvalido_Falla()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameRandom(1).NextInt(0));
        }

        [Fact]
        public void NextDouble_QuedaEntreCeroYUno()
        {
            var random = new GameRandom(9);
            for (int i = 0; i < 10000; i++)
            {
                double value = random.NextDouble();
                Assert.True(value >= 0.0 && value < 1.0);
            }
        }

        [Fact]
        public void Chance_RespetaAproximadamenteLaProbabilidad()
        {
            var random = new GameRandom(11);
            int hits = Enumerable.Range(0, 100000).Count(_ => random.Chance(0.3));
            Assert.InRange(hits, 29000, 31000);
        }

        [Fact]
        public void Fork_NoAvanzaElGeneradorOriginal()
        {
            var a = new GameRandom(5);
            var b = new GameRandom(5);
            a.Fork("salud").NextULong();
            Assert.Equal(b.NextULong(), a.NextULong());
        }

        [Fact]
        public void Fork_ConNombresDistintos_DaFlujosDistintos()
        {
            var random = new GameRandom(5);
            Assert.NotEqual(random.Fork("salud").NextULong(), random.Fork("dinero").NextULong());
        }

        [Fact]
        public void Fork_ConElMismoNombre_EsReproducible()
        {
            Assert.Equal(new GameRandom(5).Fork("salud").NextULong(), new GameRandom(5).Fork("salud").NextULong());
        }
    }
}
