using System;
using System.Collections.Generic;

namespace MistakesLife.Core.Scripting
{
    /// <summary>
    /// Una condición ya leída, lista para evaluarse muchas veces.
    /// Ejemplo: "jugador.edad >= 17 &amp;&amp; semilla('copio_paes').activa".
    /// </summary>
    public sealed class Expression
    {
        private readonly Node _root;

        private Expression(string source, Node root)
        {
            Source = source;
            _root = root;
        }

        public string Source { get; }

        /// <summary>Lee el texto. Lanza ScriptException si está mal escrito.</summary>
        public static Expression Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ScriptException("La condición está vacía.");
            }

            return new Expression(source, new Parser(source).ParseExpressionToEnd());
        }

        public Value Evaluate(IScriptScope scope) => _root.Evaluate(scope);

        /// <summary>Evalúa y exige un resultado verdadero/falso.</summary>
        public bool IsTrue(IScriptScope scope)
        {
            try
            {
                return Evaluate(scope).AsBool("condición");
            }
            catch (ScriptException ex)
            {
                throw new ScriptException($"En \"{Source}\": {ex.Message}");
            }
        }

        /// <summary>Funciones llamadas con un texto fijo, para validar ids.</summary>
        public IEnumerable<ScriptReference> References() => ReferenceFinder.Find(_root);

        /// <summary>Nombres de funciones y raíces usados, para validar que existan.</summary>
        public IEnumerable<string> Names() => ReferenceFinder.Names(_root);
    }

    internal static class ReferenceFinder
    {
        public static IEnumerable<ScriptReference> Find(Node root)
        {
            foreach (Node node in Walk(root))
            {
                if (node is FunctionNode fn && fn.Args.Count > 0 && fn.Args[0] is LiteralNode lit && lit.Value.Kind == ValueKind.Text)
                {
                    string? second = fn.Args.Count > 1 && fn.Args[1] is LiteralNode lit2 && lit2.Value.Kind == ValueKind.Text ? lit2.Value.AsText(fn.Name) : null;
                    yield return new ScriptReference(fn.Name, lit.Value.AsText(fn.Name), second);
                }
                else if (node is MethodNode method && method.Args.Count > 0 && method.Args[0] is LiteralNode methodLit && methodLit.Value.Kind == ValueKind.Text)
                {
                    // Los métodos se distinguen de las funciones con un punto: ".endeudarse".
                    yield return new ScriptReference("." + method.Name, methodLit.Value.AsText(method.Name));
                }
            }
        }

        public static IEnumerable<string> Names(Node root)
        {
            foreach (Node node in Walk(root))
            {
                if (node is FunctionNode fn)
                {
                    yield return fn.Name + "()";
                }
                else if (node is RootNode r)
                {
                    yield return r.Name;
                }
            }
        }

        private static IEnumerable<Node> Walk(Node node)
        {
            var stack = new Stack<Node>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                yield return current;
                foreach (Node child in current.Children)
                {
                    stack.Push(child);
                }
            }
        }

        internal static Exception Wrap(string source, ScriptException ex) => new ScriptException($"En \"{source}\": {ex.Message}");
    }
}
