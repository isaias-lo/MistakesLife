using System;
using System.Collections.Generic;

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

        public string Source { get; }

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
