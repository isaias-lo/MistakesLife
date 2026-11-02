using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MistakesLife.Core.Scripting
{
    // Piezas internas del lenguaje: tokens, lector (lexer) y árbol de la expresión.
    // No son públicas: el resto del juego usa Expression y Effect.

    internal enum TokenKind
    {
        Number,
        Text,
        Identifier,
        Dot,
        Comma,
        LeftParen,
        RightParen,
        And,
        Or,
        Not,
        Equal,
        NotEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        Plus,
        Minus,
        Star,
        Slash,
        Assign,
        PlusAssign,
        MinusAssign,
        End,
    }

    internal readonly struct Token
    {
        public Token(TokenKind kind, string text, int position)
        {
            Kind = kind;
            Text = text;
            Position = position;
        }

        public TokenKind Kind { get; }

        public string Text { get; }

        public int Position { get; }
    }

    internal static class Lexer
    {
        public static List<Token> Tokenize(string source)
        {
            var tokens = new List<Token>();
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                int start = i;
                if (char.IsDigit(c))
                {
                    while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.'))
                    {
                        i++;
                    }

                    tokens.Add(new Token(TokenKind.Number, source.Substring(start, i - start), start));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                    {
                        i++;
                    }

                    tokens.Add(new Token(TokenKind.Identifier, source.Substring(start, i - start), start));
                    continue;
                }

                if (c == '\'' || c == '"')
                {
                    char quote = c;
                    i++;
                    var text = new StringBuilder();
                    while (i < source.Length && source[i] != quote)
                    {
                        text.Append(source[i]);
                        i++;
                    }

                    if (i >= source.Length)
                    {
                        throw new ScriptException($"Texto sin cerrar en la posición {start}.");
                    }

                    i++;
                    tokens.Add(new Token(TokenKind.Text, text.ToString(), start));
                    continue;
                }

                string two = i + 1 < source.Length ? source.Substring(i, 2) : string.Empty;
                TokenKind? twoKind = two switch
                {
                    "&&" => TokenKind.And,
                    "||" => TokenKind.Or,
                    "==" => TokenKind.Equal,
                    "!=" => TokenKind.NotEqual,
                    "<=" => TokenKind.LessEqual,
                    ">=" => TokenKind.GreaterEqual,
                    "+=" => TokenKind.PlusAssign,
                    "-=" => TokenKind.MinusAssign,
                    _ => null,
                };
                if (twoKind.HasValue)
                {
                    tokens.Add(new Token(twoKind.Value, two, start));
                    i += 2;
                    continue;
                }

                TokenKind? oneKind = c switch
                {
                    '.' => TokenKind.Dot,
                    ',' => TokenKind.Comma,
                    '(' => TokenKind.LeftParen,
                    ')' => TokenKind.RightParen,
                    '!' => TokenKind.Not,
                    '<' => TokenKind.Less,
                    '>' => TokenKind.Greater,
                    '+' => TokenKind.Plus,
                    '-' => TokenKind.Minus,
                    '*' => TokenKind.Star,
                    '/' => TokenKind.Slash,
                    '=' => TokenKind.Assign,
                    _ => null,
                };
                if (!oneKind.HasValue)
                {
                    throw new ScriptException($"Carácter no válido '{c}' en la posición {start}.");
                }

                tokens.Add(new Token(oneKind.Value, c.ToString(), start));
                i++;
            }

            tokens.Add(new Token(TokenKind.End, string.Empty, source.Length));
            return tokens;
        }
    }

    internal abstract class Node
    {
        public abstract Value Evaluate(IScriptScope scope);

        /// <summary>Recorre el árbol (para la validación de paquetes).</summary>
        public abstract IEnumerable<Node> Children { get; }
    }

    internal sealed class LiteralNode : Node
    {
        public LiteralNode(Value value) => Value = value;

        public Value Value { get; }

        public override IEnumerable<Node> Children => Array.Empty<Node>();

        public override Value Evaluate(IScriptScope scope) => Value;
    }

    internal sealed class RootNode : Node
    {
        public RootNode(string name) => Name = name;

        public string Name { get; }

        public override IEnumerable<Node> Children => Array.Empty<Node>();

        public override Value Evaluate(IScriptScope scope) => scope.GetRoot(Name);
    }

    internal sealed class FunctionNode : Node
    {
        public FunctionNode(string name, List<Node> args)
        {
            Name = name;
            Args = args;
        }

        public string Name { get; }

        public List<Node> Args { get; }

        public override IEnumerable<Node> Children => Args;

        public override Value Evaluate(IScriptScope scope) => scope.CallFunction(Name, EvaluateAll(Args, scope));

        internal static Value[] EvaluateAll(List<Node> args, IScriptScope scope)
        {
            var values = new Value[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                values[i] = args[i].Evaluate(scope);
            }

            return values;
        }
    }

    internal sealed class MemberNode : Node
    {
        public MemberNode(Node target, string name)
        {
            Target = target;
            Name = name;
        }

        public Node Target { get; }

        public string Name { get; }

        public override IEnumerable<Node> Children => new[] { Target };

        public override Value Evaluate(IScriptScope scope) => Target.Evaluate(scope).AsObject("." + Name).Get(Name, scope);
    }

    internal sealed class MethodNode : Node
    {
        public MethodNode(Node target, string name, List<Node> args)
        {
            Target = target;
            Name = name;
            Args = args;
        }

        public Node Target { get; }

        public string Name { get; }

        public List<Node> Args { get; }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Target;
                foreach (Node arg in Args)
                {
                    yield return arg;
                }
            }
        }

        public override Value Evaluate(IScriptScope scope)
        {
            IScriptObject target = Target.Evaluate(scope).AsObject("." + Name + "()");
            return target.Call(Name, FunctionNode.EvaluateAll(Args, scope), scope);
        }
    }

    internal sealed class UnaryNode : Node
    {
        public UnaryNode(TokenKind op, Node operand)
        {
            Op = op;
            Operand = operand;
        }

        public TokenKind Op { get; }

        public Node Operand { get; }

        public override IEnumerable<Node> Children => new[] { Operand };

        public override Value Evaluate(IScriptScope scope)
        {
            Value value = Operand.Evaluate(scope);
            return Op == TokenKind.Not
                ? Value.FromBool(!value.AsBool("!"))
                : Value.FromNumber(-value.AsNumber("-"));
        }
    }

    internal sealed class BinaryNode : Node
    {
        public BinaryNode(TokenKind op, Node left, Node right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public TokenKind Op { get; }

        public Node Left { get; }

        public Node Right { get; }

        public override IEnumerable<Node> Children => new[] { Left, Right };

        public override Value Evaluate(IScriptScope scope)
        {
            // && y || cortan apenas saben el resultado.
            if (Op == TokenKind.And)
            {
                return Value.FromBool(Left.Evaluate(scope).AsBool("&&") && Right.Evaluate(scope).AsBool("&&"));
            }

            if (Op == TokenKind.Or)
            {
                return Value.FromBool(Left.Evaluate(scope).AsBool("||") || Right.Evaluate(scope).AsBool("||"));
            }

            Value left = Left.Evaluate(scope);
            Value right = Right.Evaluate(scope);
            switch (Op)
            {
                case TokenKind.Equal:
                    return Value.FromBool(left.Equals(right));
                case TokenKind.NotEqual:
                    return Value.FromBool(!left.Equals(right));
                case TokenKind.Less:
                    return Value.FromBool(left.AsNumber("<") < right.AsNumber("<"));
                case TokenKind.LessEqual:
                    return Value.FromBool(left.AsNumber("<=") <= right.AsNumber("<="));
                case TokenKind.Greater:
                    return Value.FromBool(left.AsNumber(">") > right.AsNumber(">"));
                case TokenKind.GreaterEqual:
                    return Value.FromBool(left.AsNumber(">=") >= right.AsNumber(">="));
                case TokenKind.Plus:
                    return Value.FromNumber(left.AsNumber("+") + right.AsNumber("+"));
                case TokenKind.Minus:
                    return Value.FromNumber(left.AsNumber("-") - right.AsNumber("-"));
                case TokenKind.Star:
                    return Value.FromNumber(left.AsNumber("*") * right.AsNumber("*"));
                case TokenKind.Slash:
                    double divisor = right.AsNumber("/");
                    if (divisor == 0)
                    {
                        throw new ScriptException("División por cero.");
                    }

                    return Value.FromNumber(left.AsNumber("/") / divisor);
                default:
                    throw new ScriptException($"Operador no soportado: {Op}.");
            }
        }
    }

    /// <summary>Lee los tokens y arma el árbol, respetando la precedencia de operadores.</summary>
    internal sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public Parser(string source)
        {
            _tokens = Lexer.Tokenize(source);
        }

        private Token Current => _tokens[_pos];

        public Node ParseExpressionToEnd()
        {
            Node node = ParseExpression();
            Expect(TokenKind.End, "el final");
            return node;
        }

        /// <summary>Efecto: expresión, opcionalmente seguida de =, += o -= y otra expresión.</summary>
        public (Node Target, TokenKind? Op, Node? Value) ParseStatementToEnd()
        {
            Node target = ParseExpression();
            if (Current.Kind == TokenKind.Assign || Current.Kind == TokenKind.PlusAssign || Current.Kind == TokenKind.MinusAssign)
            {
                TokenKind op = Advance().Kind;
                if (!(target is MemberNode))
                {
                    throw new ScriptException("Solo se puede asignar a un dato, por ejemplo jugador.salud += 5.");
                }

                Node value = ParseExpression();
                Expect(TokenKind.End, "el final");
                return (target, op, value);
            }

            Expect(TokenKind.End, "el final");
            return (target, null, null);
        }

        private Node ParseExpression() => ParseOr();

        private Node ParseOr()
        {
            Node left = ParseAnd();
            while (Current.Kind == TokenKind.Or)
            {
                Advance();
                left = new BinaryNode(TokenKind.Or, left, ParseAnd());
            }

            return left;
        }

        private Node ParseAnd()
        {
            Node left = ParseEquality();
            while (Current.Kind == TokenKind.And)
            {
                Advance();
                left = new BinaryNode(TokenKind.And, left, ParseEquality());
            }

            return left;
        }

        private Node ParseEquality()
        {
            Node left = ParseComparison();
            while (Current.Kind == TokenKind.Equal || Current.Kind == TokenKind.NotEqual)
            {
                TokenKind op = Advance().Kind;
                left = new BinaryNode(op, left, ParseComparison());
            }

            return left;
        }

        private Node ParseComparison()
        {
            Node left = ParseAdditive();
            while (Current.Kind == TokenKind.Less || Current.Kind == TokenKind.LessEqual || Current.Kind == TokenKind.Greater || Current.Kind == TokenKind.GreaterEqual)
            {
                TokenKind op = Advance().Kind;
                left = new BinaryNode(op, left, ParseAdditive());
            }

            return left;
        }

        private Node ParseAdditive()
        {
            Node left = ParseMultiplicative();
            while (Current.Kind == TokenKind.Plus || Current.Kind == TokenKind.Minus)
            {
                TokenKind op = Advance().Kind;
                left = new BinaryNode(op, left, ParseMultiplicative());
            }

            return left;
        }

        private Node ParseMultiplicative()
        {
            Node left = ParseUnary();
            while (Current.Kind == TokenKind.Star || Current.Kind == TokenKind.Slash)
            {
                TokenKind op = Advance().Kind;
                left = new BinaryNode(op, left, ParseUnary());
            }

            return left;
        }

        private Node ParseUnary()
        {
            if (Current.Kind == TokenKind.Not || Current.Kind == TokenKind.Minus)
            {
                TokenKind op = Advance().Kind;
                return new UnaryNode(op, ParseUnary());
            }

            return ParsePostfix();
        }

        private Node ParsePostfix()
        {
            Node node = ParsePrimary();
            while (Current.Kind == TokenKind.Dot)
            {
                Advance();
                string name = Expect(TokenKind.Identifier, "un nombre después del punto").Text;
                node = Current.Kind == TokenKind.LeftParen
                    ? new MethodNode(node, name, ParseArguments())
                    : (Node)new MemberNode(node, name);
            }

            return node;
        }

        private Node ParsePrimary()
        {
            Token token = Advance();
            switch (token.Kind)
            {
                case TokenKind.Number:
                    if (!double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    {
                        throw new ScriptException($"Número no válido '{token.Text}' en la posición {token.Position}.");
                    }

                    return new LiteralNode(Value.FromNumber(number));
                case TokenKind.Text:
                    return new LiteralNode(Value.FromText(token.Text));
                case TokenKind.Identifier:
                    if (token.Text == "true" || token.Text == "verdadero")
                    {
                        return new LiteralNode(Value.True);
                    }

                    if (token.Text == "false" || token.Text == "falso")
                    {
                        return new LiteralNode(Value.False);
                    }

                    return Current.Kind == TokenKind.LeftParen
                        ? new FunctionNode(token.Text, ParseArguments())
                        : (Node)new RootNode(token.Text);
                case TokenKind.LeftParen:
                    Node inner = ParseExpression();
                    Expect(TokenKind.RightParen, "')'");
                    return inner;
                default:
                    throw new ScriptException($"No se esperaba '{token.Text}' en la posición {token.Position}.");
            }
        }

        private List<Node> ParseArguments()
        {
            Expect(TokenKind.LeftParen, "'('");
            var args = new List<Node>();
            if (Current.Kind != TokenKind.RightParen)
            {
                args.Add(ParseExpression());
                while (Current.Kind == TokenKind.Comma)
                {
                    Advance();
                    args.Add(ParseExpression());
                }
            }

            Expect(TokenKind.RightParen, "')'");
            return args;
        }

        private Token Advance() => _tokens[_pos++];

        private Token Expect(TokenKind kind, string what)
        {
            if (Current.Kind != kind)
            {
                string found = Current.Kind == TokenKind.End ? "el final" : $"'{Current.Text}'";
                throw new ScriptException($"Se esperaba {what} en la posición {Current.Position}, pero vino {found}.");
            }

            return Advance();
        }
    }
}
