using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class ScriptingTests
    {
        /// <summary>Un objeto de prueba con datos que se pueden leer y cambiar.</summary>
        private sealed class Bag : IScriptObject
        {
            public Dictionary<string, Value> Data { get; } = new Dictionary<string, Value>();

            public Value Get(string member, IScriptScope scope) => Data.TryGetValue(member, out Value v) ? v : throw new ScriptException($"{member} no existe");

            public void Set(string member, Value value, IScriptScope scope) => Data[member] = value;

            public Value Call(string method, Value[] args, IScriptScope scope) =>
                method == "doble" ? Value.FromNumber(args[0].AsNumber("doble") * 2) : throw new ScriptException("no existe");
        }

        private sealed class TestScope : IScriptScope
        {
            public Bag Player { get; } = new Bag();

            public List<string> Calls { get; } = new List<string>();

            public bool ReadOnly { get; set; }

            public Value GetRoot(string name) => name == "jugador" ? Value.FromObject(Player) : throw new ScriptException($"'{name}' no existe");

            public Value CallFunction(string name, Value[] args)
            {
                Calls.Add(name + ":" + args[0].AsText(name));
                return Value.True;
            }
        }

        private static TestScope Scope(int edad = 17, int salud = 80)
        {
            var scope = new TestScope();
            scope.Player.Data["edad"] = Value.FromNumber(edad);
            scope.Player.Data["salud"] = Value.FromNumber(salud);
            scope.Player.Data["postulando"] = Value.True;
            return scope;
        }

        [Theory]
        [InlineData("jugador.edad >= 17", true)]
        [InlineData("jugador.edad < 17", false)]
        [InlineData("jugador.edad == 17 && jugador.postulando", true)]
        [InlineData("jugador.edad > 30 || jugador.postulando", true)]
        [InlineData("!jugador.postulando", false)]
        [InlineData("(jugador.edad + 3) * 2 == 40", true)]
        [InlineData("jugador.salud / 4 - 1 == 19", true)]
        [InlineData("-jugador.edad < 0", true)]
        [InlineData("jugador.doble(4) == 8", true)]
        [InlineData("'chile' == \"chile\"", true)]
        [InlineData("verdadero && !falso", true)]
        [InlineData("1 + 2 * 3 == 7", true)]
        public void Condiciones_SeEvaluanCorrectamente(string source, bool expected)
        {
            Assert.Equal(expected, Expression.Parse(source).IsTrue(Scope()));
        }

        [Fact]
        public void AndCorta_NoEvaluaLaSegundaParteSiLaPrimeraEsFalsa()
        {
            var scope = Scope();
            Assert.False(Expression.Parse("jugador.edad > 50 && semilla('x')").IsTrue(scope));
            Assert.Empty(scope.Calls);
        }

        [Theory]
        [InlineData("jugador.edad >=")]
        [InlineData("jugador.edad >= 17)")]
        [InlineData("semilla('sin cerrar)")]
        [InlineData("jugador. >= 3")]
        [InlineData("jugador.edad # 3")]
        [InlineData("")]
        public void TextoMalEscrito_DaUnErrorClaro(string source)
        {
            Assert.Throws<ScriptException>(() => Expression.Parse(source));
        }

        [Fact]
        public void TiposDistintos_DanError()
        {
            var ex = Assert.Throws<ScriptException>(() => Expression.Parse("jugador.edad && true").IsTrue(Scope()));
            Assert.Contains("jugador.edad && true", ex.Message);
        }

        [Fact]
        public void CondicionQueNoDaVerdaderoFalso_DaError()
        {
            Assert.Throws<ScriptException>(() => Expression.Parse("jugador.edad + 1").IsTrue(Scope()));
        }

        [Theory]
        [InlineData("jugador.salud -= 10", 70)]
        [InlineData("jugador.salud += 5", 85)]
        [InlineData("jugador.salud = 42", 42)]
        public void Efectos_CambianLosDatos(string source, double expected)
        {
            var scope = Scope();
            Effect.Parse(source).Apply(scope);
            Assert.Equal(expected, scope.Player.Data["salud"].AsNumber("salud"));
        }

        [Fact]
        public void Efecto_SinAsignacion_LlamaLaFuncion()
        {
            var scope = Scope();
            Effect.Parse("plantar('mentira_entrevista')").Apply(scope);
            Assert.Equal(new[] { "plantar:mentira_entrevista" }, scope.Calls);
        }

        [Fact]
        public void Efecto_EnModoSoloLectura_Falla()
        {
            var scope = Scope();
            scope.ReadOnly = true;
            Assert.Throws<ScriptException>(() => Effect.Parse("jugador.salud -= 1").Apply(scope));
        }

        [Fact]
        public void AsignarAAlgoQueNoEsUnDato_Falla()
        {
            Assert.Throws<ScriptException>(() => Effect.Parse("3 = 4"));
        }

        [Fact]
        public void Referencias_EncuentraLosIdsEscritosEntreComillas()
        {
            var refs = new List<ScriptReference>(Expression.Parse("semilla('a').activa && semilla('b').fuerza > 3").References());
            Assert.Contains(refs, r => r.Function == "semilla" && r.Argument == "a");
            Assert.Contains(refs, r => r.Function == "semilla" && r.Argument == "b");
        }
    }
}
