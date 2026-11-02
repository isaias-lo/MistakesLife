using System;
using System.Globalization;

namespace MistakesLife.Core.Scripting
{
    /// <summary>Un valor del lenguaje de condiciones: número, sí/no, texto, objeto o nada.</summary>
    public readonly struct Value : IEquatable<Value>
    {
        private readonly double _number;
        private readonly object? _ref;

        private Value(ValueKind kind, double number, object? reference)
        {
            Kind = kind;
            _number = number;
            _ref = reference;
        }

        public static Value Null => default;

        public static Value True => FromBool(true);

        public static Value False => FromBool(false);

        public ValueKind Kind { get; }

        public static Value FromNumber(double number) => new Value(ValueKind.Number, number, null);

        public static Value FromBool(bool value) => new Value(ValueKind.Bool, value ? 1 : 0, null);

        public static Value FromText(string text) => new Value(ValueKind.Text, 0, text ?? throw new ArgumentNullException(nameof(text)));

        public static Value FromObject(IScriptObject obj) => new Value(ValueKind.Object, 0, obj ?? throw new ArgumentNullException(nameof(obj)));

        public double AsNumber(string context)
        {
            Expect(ValueKind.Number, context);
            return _number;
        }

        public bool AsBool(string context)
        {
            Expect(ValueKind.Bool, context);
            return _number != 0;
        }

        public string AsText(string context)
        {
            Expect(ValueKind.Text, context);
            return (string)_ref!;
        }

        public IScriptObject AsObject(string context)
        {
            Expect(ValueKind.Object, context);
            return (IScriptObject)_ref!;
        }

        public bool Equals(Value other)
        {
            if (Kind != other.Kind)
            {
                return false;
            }

            switch (Kind)
            {
                case ValueKind.Null:
                    return true;
                case ValueKind.Number:
                case ValueKind.Bool:
                    return _number.Equals(other._number);
                case ValueKind.Text:
                    return string.Equals((string)_ref!, (string)other._ref!, StringComparison.Ordinal);
                default:
                    return ReferenceEquals(_ref, other._ref);
            }
        }

        public override bool Equals(object? obj) => obj is Value other && Equals(other);

        public override int GetHashCode() => Kind == ValueKind.Number || Kind == ValueKind.Bool ? _number.GetHashCode() : (_ref?.GetHashCode() ?? 0);

        public override string ToString()
        {
            switch (Kind)
            {
                case ValueKind.Null:
                    return "nada";
                case ValueKind.Number:
                    return _number.ToString(CultureInfo.InvariantCulture);
                case ValueKind.Bool:
                    return _number != 0 ? "true" : "false";
                case ValueKind.Text:
                    return "'" + (string)_ref! + "'";
                default:
                    return _ref!.ToString() ?? "objeto";
            }
        }

        private void Expect(ValueKind kind, string context)
        {
            if (Kind != kind)
            {
                throw new ScriptException($"{context}: se esperaba {Describe(kind)} y llegó {Describe(Kind)} ({this}).");
            }
        }

        private static string Describe(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Number:
                    return "un número";
                case ValueKind.Bool:
                    return "verdadero/falso";
                case ValueKind.Text:
                    return "un texto";
                case ValueKind.Object:
                    return "un objeto";
                default:
                    return "nada";
            }
        }
    }
}
