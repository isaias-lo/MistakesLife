using System;
using System.IO;
using System.Linq;
using MistakesLife.ConsoleGame;
using MistakesLife.Core.Game;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class ConsoleGameTests
    {
        private static string Play(ulong seed, string input)
        {
            var output = new StringWriter();
            new ConsoleGame.ConsoleGame(new LifeSession(RepoPacks.Content, seed), new StringReader(input), output).Run();
            return output.ToString();
        }

        /// <summary>Enter para avanzar y "1" para elegir, muchas veces.</summary>
        private static string AlwaysFirstOption() => string.Concat(Enumerable.Repeat("\n1\n", 3000));

        [Fact]
        public void Consola_JuegaUnaVidaHastaLaMuerte()
        {
            string output = Play(7, AlwaysFirstOption());

            Assert.Contains("Naciste:", output);
            Assert.Contains("Puente Alto, Santiago, Chile", output);
            Assert.Contains("Tu mamá:", output);
            Assert.Contains("Primer día de primero básico", output);
            Assert.Contains("Tu vida terminó a los", output);
        }

        [Fact]
        public void Consola_MismaPartidaYMismasDecisiones_MismaHistoria()
        {
            Assert.Equal(Play(42, AlwaysFirstOption()), Play(42, AlwaysFirstOption()));
        }

        [Fact]
        public void Consola_OpcionInvalida_VuelveAPreguntar()
        {
            string output = Play(3, "\n9\nhola\n1\nq\n");
            Assert.Contains("Escribe un número del 1 al", output);
        }

        [Fact]
        public void Consola_Salir_TerminaSinErrores()
        {
            string output = Play(3, "e\nd\nq\n");
            Assert.Contains("Hasta pronto.", output);
            Assert.Contains("Salud", output);
        }

        [Fact]
        public void Sesion_NoDejaAvanzarConUnEventoPendiente()
        {
            var session = new LifeSession(RepoPacks.Content, 1);
            while (session.Pending == null)
            {
                session.Advance(52);
            }

            Assert.Throws<InvalidOperationException>(() => session.Advance(1));
        }

        [Fact]
        public void Sesion_EntregaCadaEntradaDelDiarioUnaSolaVez()
        {
            var session = new LifeSession(RepoPacks.Content, 1);
            Assert.Contains(session.TakeNewJournalEntries(), e => e.TextKey == "diario.nacimiento");
            Assert.Empty(session.TakeNewJournalEntries());
        }

        [Fact]
        public void BuscarPacks_SubeHastaEncontrarlos()
        {
            Assert.NotNull(ConsoleGame.ConsoleGame.FindPacks(AppContext.BaseDirectory));
        }
    }
}
