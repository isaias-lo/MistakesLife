using System;
using System.Collections.Generic;
using System.Linq;

namespace MistakesLife.Core.Scripting
{
    /// <summary>
    /// Un efecto ya leído: lo que cambia una opción de un evento.
    /// Ejemplos: "jugador.salud -= 10", "semilla('copio_paes').fuerza += 20",
    /// "plantar('mentira_entrevista')", "semilla('copio_paes').cerrar".
    /// </summary>
    public sealed class Effect
    {
        private readonly Node _target;
        private readonly TokenKind? _op;
        private readonly Node? _value;

        private Effect(string source, Node target, TokenKind? op, Node? value)
        {
            Source = source;
            _target = target;
            _op = op;
            _value = value;
        }

        /// <summary>Nombre de la función que planta una semilla: <c>plantar('id')</c> o <c>plantar('id', persona)</c>.</summary>
        public const string PlantFunction = "plantar";

        public string Source { get; }

        /// <summary>Si este efecto planta una semilla: la elección se va a recordar años después.</summary>
        public bool PlantsSeed => Plantings().Any();

        /// <summary>
        /// Los testigos que este efecto le pondría a sus semillas (el segundo dato de
        /// <c>plantar</c>), calculados sin aplicar nada. Si un testigo no se puede calcular
        /// (por ejemplo, la persona no existe), se salta.
        /// </summary>
        public IReadOnlyList<Value> Witnesses(IScriptScope scope)
        {
            var witnesses = new List<Value>();
            foreach (FunctionNode plant in Plantings())
            {
                if (plant.Args.Count != 2)
                {
                    continue;
                }

                try
                {
                    witnesses.Add(plant.Args[1].Evaluate(scope));
                }
                catch (ScriptException)
                {
                    // Sin testigo que mostrar: al aplicarse, el efecto dará su propio error.
                }
            }

            return witnesses;
        }

        private IEnumerable<FunctionNode> Plantings()
        {
            IEnumerable<Node> nodes = _value == null ? Walk(_target) : Walk(_target).Concat(Walk(_value));
            return nodes.OfType<FunctionNode>().Where(f => f.Name == PlantFunction);
        }

        private static IEnumerable<Node> Walk(Node node)
        {
            yield return node;
            foreach (Node child in node.Children)
            {
                foreach (Node inner in Walk(child))
                {
                    yield return inner;
                }
            }
        }

        public static Effect Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ScriptException("El efecto está vacío.");
            }

            var (target, op, value) = new Parser(source).ParseStatementToEnd();
            return new Effect(source, target, op, value);
        }

        public void Apply(IScriptScope scope)
        {
            if (scope.ReadOnly)
            {
                throw new ScriptException($"El efecto \"{Source}\" no puede aplicarse en modo solo lectura.");
            }

            try
            {
                if (_op == null)
                {
                    _target.Evaluate(scope);
                    return;
                }

                var member = (MemberNode)_target;
                IScriptObject obj = member.Target.Evaluate(scope).AsObject("." + member.Name);
                Value newValue = _value!.Evaluate(scope);
                if (_op == TokenKind.PlusAssign || _op == TokenKind.MinusAssign)
                {
                    double current = obj.Get(member.Name, scope).AsNumber(member.Name);
                    double delta = newValue.AsNumber(member.Name);
                    newValue = Value.FromNumber(_op == TokenKind.PlusAssign ? current + delta : current - delta);
                }

                obj.Set(member.Name, newValue, scope);
            }
            catch (ScriptException ex)
            {
                throw ReferenceFinder.Wrap(Source, ex);
            }
        }

        public IEnumerable<ScriptReference> References()
        {
            foreach (ScriptReference r in ReferenceFinder.Find(_target))
            {
                yield return r;
            }

            if (_value != null)
            {
                foreach (ScriptReference r in ReferenceFinder.Find(_value))
                {
                    yield return r;
                }
            }
        }

        public IEnumerable<string> Names()
        {
            var names = new List<string>(ReferenceFinder.Names(_target));
            if (_value != null)
            {
                names.AddRange(ReferenceFinder.Names(_value));
            }

            return names;
        }
    }
}
