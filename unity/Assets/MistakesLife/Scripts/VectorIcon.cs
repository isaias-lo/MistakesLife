#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Un ícono dibujado con trazos, como un SVG de 24 × 24: se arma con los mismos caminos
    /// ("M3 9.5 12 5l9 4.5-9 4.5z") que tienen los bocetos aprobados, así un ícono se copia
    /// del boceto tal cual y se cambia sin tocar nada más. <see cref="Rasterize"/> lo pinta en
    /// una grilla de transparencias (blanco con bordes suaves) que <see cref="Icons"/> convierte
    /// en imagen. No usa nada de Unity, para poder revisarlo fuera del editor.
    /// Entiende los comandos M, L, H, V, C, S, Q, A y Z (con sus versiones en minúscula).
    /// </summary>
    public sealed class VectorIcon
    {
        /// <summary>El tamaño del dibujo en unidades (como el viewBox "0 0 24 24" de los bocetos).</summary>
        public const float Units = 24f;

        /// <summary>Cuántos tramos rectos se usan para cada curva (más, más suave).</summary>
        private const int CurveSteps = 16;

        private readonly List<Layer> _layers = new List<Layer>();

        /// <summary>Agrega un camino con borde (línea fina). <paramref name="width"/> en unidades del dibujo.</summary>
        public VectorIcon Stroke(string path, float width = 1.8f)
        {
            _layers.Add(new Layer(Parse(path), false, width));
            return this;
        }

        /// <summary>Agrega un camino relleno.</summary>
        public VectorIcon Fill(string path)
        {
            _layers.Add(new Layer(Parse(path), true, 0f));
            return this;
        }

        /// <summary>Un círculo con borde.</summary>
        public VectorIcon StrokeCircle(float cx, float cy, float r, float width = 1.8f) => Stroke(CirclePath(cx, cy, r), width);

        /// <summary>Un círculo relleno.</summary>
        public VectorIcon FillCircle(float cx, float cy, float r) => Fill(CirclePath(cx, cy, r));

        /// <summary>Un rectángulo con esquinas redondeadas, con borde.</summary>
        public VectorIcon StrokeRect(float x, float y, float w, float h, float rx, float width = 1.8f) => Stroke(RectPath(x, y, w, h, rx), width);

        /// <summary>
        /// Pinta el ícono en una grilla de <paramref name="size"/> × <paramref name="size"/>.
        /// Devuelve la transparencia de cada punto (0 a 1), fila por fila, empezando por la de
        /// abajo (como las texturas de Unity).
        /// </summary>
        public float[] Rasterize(int size)
        {
            float scale = size / Units;
            var alpha = new float[size * size];
            foreach (Layer layer in _layers)
            {
                var polylines = new List<List<Point>>();
                foreach (List<Point> line in layer.Lines)
                {
                    var scaled = new List<Point>(line.Count);
                    foreach (Point p in line)
                    {
                        scaled.Add(new Point(p.X * scale, p.Y * scale));
                    }

                    polylines.Add(scaled);
                }

                float half = layer.Width * scale / 2f;
                for (int row = 0; row < size; row++)
                {
                    // La fila 0 de la textura es la de abajo; en el dibujo, y crece hacia abajo.
                    float y = size - row - 0.5f;
                    for (int col = 0; col < size; col++)
                    {
                        float x = col + 0.5f;
                        float distance = Distance(polylines, x, y);
                        float value;
                        if (layer.Filled)
                        {
                            bool inside = Inside(polylines, x, y);
                            value = inside ? Clamp01(distance + 0.5f) : Clamp01(0.5f - distance);
                        }
                        else
                        {
                            value = Clamp01(half - distance + 0.5f);
                        }

                        int index = (row * size) + col;
                        if (value > alpha[index])
                        {
                            alpha[index] = value;
                        }
                    }
                }
            }

            return alpha;
        }

        /// <summary>Cuántos tramos tiene el ícono (para las pruebas: un camino mal escrito queda vacío).</summary>
        public int SegmentCount
        {
            get
            {
                int count = 0;
                foreach (Layer layer in _layers)
                {
                    foreach (List<Point> line in layer.Lines)
                    {
                        count += Math.Max(0, line.Count - 1);
                    }
                }

                return count;
            }
        }

        private static string CirclePath(float cx, float cy, float r) =>
            string.Format(CultureInfo.InvariantCulture, "M{0} {1}a{2} {2} 0 1 0 {3} 0a{2} {2} 0 1 0 {4} 0z", cx - r, cy, r, 2f * r, -2f * r);

        private static string RectPath(float x, float y, float w, float h, float r) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "M{0} {1}H{2}A{4} {4} 0 0 1 {3} {5}V{6}A{4} {4} 0 0 1 {2} {7}H{0}A{4} {4} 0 0 1 {8} {6}V{5}A{4} {4} 0 0 1 {0} {1}z",
                x + r, y, x + w - r, x + w, r, y + r, y + h - r, y + h, x);

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        /// <summary>La distancia más corta de un punto a cualquier tramo.</summary>
        private static float Distance(List<List<Point>> lines, float x, float y)
        {
            float best = float.MaxValue;
            foreach (List<Point> line in lines)
            {
                if (line.Count == 1)
                {
                    best = Math.Min(best, Length(x - line[0].X, y - line[0].Y));
                }

                for (int i = 1; i < line.Count; i++)
                {
                    best = Math.Min(best, SegmentDistance(line[i - 1], line[i], x, y));
                }
            }

            return best;
        }

        private static float SegmentDistance(Point a, Point b, float x, float y)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float lengthSquared = (dx * dx) + (dy * dy);
            float t = lengthSquared <= 0f ? 0f : Clamp01((((x - a.X) * dx) + ((y - a.Y) * dy)) / lengthSquared);
            return Length(x - (a.X + (t * dx)), y - (a.Y + (t * dy)));
        }

        private static float Length(float x, float y) => (float)Math.Sqrt((x * x) + (y * y));

        /// <summary>Si el punto queda adentro del relleno (regla del número de vueltas, como en SVG).</summary>
        private static bool Inside(List<List<Point>> lines, float x, float y)
        {
            int winding = 0;
            foreach (List<Point> line in lines)
            {
                for (int i = 0; i < line.Count; i++)
                {
                    // Cada camino relleno se cierra solo, como en SVG.
                    Point a = line[i];
                    Point b = line[(i + 1) % line.Count];
                    if (a.Y <= y)
                    {
                        if (b.Y > y && Cross(a, b, x, y) > 0f)
                        {
                            winding++;
                        }
                    }
                    else if (b.Y <= y && Cross(a, b, x, y) < 0f)
                    {
                        winding--;
                    }
                }
            }

            return winding != 0;
        }

        private static float Cross(Point a, Point b, float x, float y) => ((b.X - a.X) * (y - a.Y)) - ((x - a.X) * (b.Y - a.Y));

        /// <summary>Convierte el texto del camino en tramos rectos (las curvas se cortan en <see cref="CurveSteps"/> pedazos).</summary>
        private static List<List<Point>> Parse(string path)
        {
            var lines = new List<List<Point>>();
            List<Point>? current = null;
            var reader = new PathReader(path);
            var position = new Point(0f, 0f);
            var start = position;
            var lastControl = position;
            char command = 'M';
            char previous = ' ';
            while (reader.HasMore)
            {
                if (reader.NextIsCommand)
                {
                    command = reader.Command();
                }
                else if (command == 'M')
                {
                    // Números después de un "M": son líneas, como dice SVG.
                    command = 'L';
                }
                else if (command == 'm')
                {
                    command = 'l';
                }

                bool relative = char.IsLower(command);
                float ox = relative ? position.X : 0f;
                float oy = relative ? position.Y : 0f;
                switch (char.ToUpperInvariant(command))
                {
                    case 'M':
                        position = new Point(ox + reader.Number(), oy + reader.Number());
                        start = position;
                        current = new List<Point> { position };
                        lines.Add(current);
                        break;
                    case 'L':
                        position = new Point(ox + reader.Number(), oy + reader.Number());
                        Add(ref current, lines, position);
                        break;
                    case 'H':
                        position = new Point(ox + reader.Number(), position.Y);
                        Add(ref current, lines, position);
                        break;
                    case 'V':
                        position = new Point(position.X, oy + reader.Number());
                        Add(ref current, lines, position);
                        break;
                    case 'C':
                    {
                        var c1 = new Point(ox + reader.Number(), oy + reader.Number());
                        var c2 = new Point(ox + reader.Number(), oy + reader.Number());
                        var end = new Point(ox + reader.Number(), oy + reader.Number());
                        Cubic(ref current, lines, position, c1, c2, end);
                        lastControl = c2;
                        position = end;
                        break;
                    }
                    case 'S':
                    {
                        char up = char.ToUpperInvariant(previous);
                        Point c1 = up == 'C' || up == 'S' ? new Point((2f * position.X) - lastControl.X, (2f * position.Y) - lastControl.Y) : position;
                        var c2 = new Point(ox + reader.Number(), oy + reader.Number());
                        var end = new Point(ox + reader.Number(), oy + reader.Number());
                        Cubic(ref current, lines, position, c1, c2, end);
                        lastControl = c2;
                        position = end;
                        break;
                    }
                    case 'Q':
                    {
                        var control = new Point(ox + reader.Number(), oy + reader.Number());
                        var end = new Point(ox + reader.Number(), oy + reader.Number());
                        for (int i = 1; i <= CurveSteps; i++)
                        {
                            float t = i / (float)CurveSteps;
                            float u = 1f - t;
                            Add(ref current, lines, new Point(
                                (u * u * position.X) + (2f * u * t * control.X) + (t * t * end.X),
                                (u * u * position.Y) + (2f * u * t * control.Y) + (t * t * end.Y)));
                        }

                        position = end;
                        break;
                    }
                    case 'A':
                    {
                        float rx = reader.Number();
                        float ry = reader.Number();
                        float rotation = reader.Number();
                        bool large = reader.Flag();
                        bool sweep = reader.Flag();
                        var end = new Point(ox + reader.Number(), oy + reader.Number());
                        Arc(ref current, lines, position, rx, ry, rotation, large, sweep, end);
                        position = end;
                        break;
                    }
                    case 'Z':
                        Add(ref current, lines, start);
                        position = start;
                        current = null;
                        break;
                    default:
                        throw new FormatException($"Comando de camino desconocido: {command}");
                }

                previous = command;
            }

            return lines;
        }

        private static void Add(ref List<Point>? current, List<List<Point>> lines, Point point)
        {
            if (current == null)
            {
                current = new List<Point>();
                lines.Add(current);
            }

            current.Add(point);
        }

        private static void Cubic(ref List<Point>? current, List<List<Point>> lines, Point p0, Point p1, Point p2, Point p3)
        {
            for (int i = 1; i <= CurveSteps; i++)
            {
                float t = i / (float)CurveSteps;
                float u = 1f - t;
                float a = u * u * u;
                float b = 3f * u * u * t;
                float c = 3f * u * t * t;
                float d = t * t * t;
                Add(ref current, lines, new Point(
                    (a * p0.X) + (b * p1.X) + (c * p2.X) + (d * p3.X),
                    (a * p0.Y) + (b * p1.Y) + (c * p2.Y) + (d * p3.Y)));
            }
        }

        /// <summary>Un arco de elipse como lo escribe SVG (punto final, radios y banderas), pasado a tramos.</summary>
        private static void Arc(ref List<Point>? current, List<List<Point>> lines, Point from, float rx, float ry, float rotationDegrees, bool large, bool sweep, Point to)
        {
            if (rx == 0f || ry == 0f || (from.X == to.X && from.Y == to.Y))
            {
                Add(ref current, lines, to);
                return;
            }

            double phi = rotationDegrees * Math.PI / 180.0;
            double cos = Math.Cos(phi);
            double sin = Math.Sin(phi);
            double dx = (from.X - to.X) / 2.0;
            double dy = (from.Y - to.Y) / 2.0;
            double x1 = (cos * dx) + (sin * dy);
            double y1 = (-sin * dx) + (cos * dy);
            double rxa = Math.Abs(rx);
            double rya = Math.Abs(ry);

            // Si los radios no alcanzan para unir los puntos, se agrandan (como hace SVG).
            double fit = ((x1 * x1) / (rxa * rxa)) + ((y1 * y1) / (rya * rya));
            if (fit > 1.0)
            {
                rxa *= Math.Sqrt(fit);
                rya *= Math.Sqrt(fit);
            }

            double numerator = (rxa * rxa * rya * rya) - (rxa * rxa * y1 * y1) - (rya * rya * x1 * x1);
            double denominator = (rxa * rxa * y1 * y1) + (rya * rya * x1 * x1);
            double factor = Math.Sqrt(Math.Max(0.0, numerator / denominator)) * (large == sweep ? -1.0 : 1.0);
            double cx1 = factor * rxa * y1 / rya;
            double cy1 = -factor * rya * x1 / rxa;
            double cx = (cos * cx1) - (sin * cy1) + ((from.X + to.X) / 2.0);
            double cy = (sin * cx1) + (cos * cy1) + ((from.Y + to.Y) / 2.0);

            double startAngle = Angle(1.0, 0.0, (x1 - cx1) / rxa, (y1 - cy1) / rya);
            double delta = Angle((x1 - cx1) / rxa, (y1 - cy1) / rya, (-x1 - cx1) / rxa, (-y1 - cy1) / rya);
            if (!sweep && delta > 0)
            {
                delta -= 2.0 * Math.PI;
            }
            else if (sweep && delta < 0)
            {
                delta += 2.0 * Math.PI;
            }

            int steps = Math.Max(4, (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2.0) * CurveSteps / 2.0));
            for (int i = 1; i <= steps; i++)
            {
                double angle = startAngle + (delta * i / steps);
                double ex = rxa * Math.Cos(angle);
                double ey = rya * Math.Sin(angle);
                Add(ref current, lines, new Point((float)((cos * ex) - (sin * ey) + cx), (float)((sin * ex) + (cos * ey) + cy)));
            }
        }

        private static double Angle(double ux, double uy, double vx, double vy)
        {
            double angle = Math.Atan2((ux * vy) - (uy * vx), (ux * vx) + (uy * vy));
            return angle;
        }

        private readonly struct Point
        {
            public Point(float x, float y)
            {
                X = x;
                Y = y;
            }

            public float X { get; }

            public float Y { get; }
        }

        private sealed class Layer
        {
            public Layer(List<List<Point>> lines, bool filled, float width)
            {
                Lines = lines;
                Filled = filled;
                Width = width;
            }

            public List<List<Point>> Lines { get; }

            public bool Filled { get; }

            public float Width { get; }
        }

        /// <summary>Lee el texto de un camino: letras de comando, números (con o sin espacios: "-.2-.3") y banderas de arco.</summary>
        private sealed class PathReader
        {
            private readonly string _text;
            private int _at;

            public PathReader(string text)
            {
                _text = text;
            }

            public bool HasMore
            {
                get
                {
                    SkipSeparators();
                    return _at < _text.Length;
                }
            }

            public bool NextIsCommand
            {
                get
                {
                    SkipSeparators();
                    return _at < _text.Length && char.IsLetter(_text[_at]) && _text[_at] != 'e' && _text[_at] != 'E';
                }
            }

            public char Command()
            {
                SkipSeparators();
                return _text[_at++];
            }

            /// <summary>Una bandera de arco: un solo dígito 0 o 1, que puede ir pegado al número siguiente ("0 0 1 6 0" o "011").</summary>
            public bool Flag()
            {
                SkipSeparators();
                char c = _text[_at++];
                if (c != '0' && c != '1')
                {
                    throw new FormatException($"Bandera de arco inválida en \"{_text}\"");
                }

                return c == '1';
            }

            public float Number()
            {
                SkipSeparators();
                int begin = _at;
                if (_at < _text.Length && (_text[_at] == '-' || _text[_at] == '+'))
                {
                    _at++;
                }

                bool dot = false;
                while (_at < _text.Length)
                {
                    char c = _text[_at];
                    if (char.IsDigit(c))
                    {
                        _at++;
                    }
                    else if (c == '.' && !dot)
                    {
                        dot = true;
                        _at++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (_at == begin)
                {
                    throw new FormatException($"Se esperaba un número en \"{_text}\" (posición {begin})");
                }

                return float.Parse(_text.Substring(begin, _at - begin), CultureInfo.InvariantCulture);
            }

            private void SkipSeparators()
            {
                while (_at < _text.Length && (char.IsWhiteSpace(_text[_at]) || _text[_at] == ','))
                {
                    _at++;
                }
            }
        }
    }
}
