#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Los íconos propios del juego (docs/diseno-juego.md, "Íconos de los grupos" y "Barras de la
    /// vida"), copiados de los bocetos aprobados: línea fina (estilo A) para los grupos y un
    /// dibujo blanco sobre un círculo de color para cada barra. Cada ícono es un dibujo de
    /// trazos (<see cref="VectorIcon"/>) que se pinta una sola vez en blanco y se tiñe con el
    /// color de la imagen. Para cambiar un ícono se cambia solo su línea en <see cref="Draw"/>.
    /// </summary>
    public static class Icons
    {
        /// <summary>Lado de la imagen en la que se pinta cada ícono (nítido hasta unos 96 puntos).</summary>
        private const int Pixels = 128;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Tu gente: dos personas.</summary>
        public const string People = "tu_gente";

        /// <summary>Estudios y trabajo: el birrete.</summary>
        public const string Studies = "estudios";

        /// <summary>Dinero: la tarjeta.</summary>
        public const string Money = "dinero";

        /// <summary>Dónde vives: la casa.</summary>
        public const string Home = "donde_vives";

        /// <summary>Compras: la bolsa.</summary>
        public const string Shopping = "compras";

        /// <summary>Salud: el corazón con pulso, en línea.</summary>
        public const string Health = "salud";

        /// <summary>Celular.</summary>
        public const string Phone = "celular";

        /// <summary>Trámites y leyes: la balanza.</summary>
        public const string Law = "tramites";

        /// <summary>Tu historia: el libro abierto.</summary>
        public const string History = "historia";

        /// <summary>Más: tres puntos.</summary>
        public const string More = "mas";

        /// <summary>Volver al pasado: el reloj con la flecha hacia atrás.</summary>
        public const string GoBack = "volver";

        /// <summary>Menú del juego: tres rayas.</summary>
        public const string Menu = "menu";

        /// <summary>Tu plata en una cuenta (D6-1a): un billete.</summary>
        public const string Bill = "billete";

        /// <summary>Tu plata de niño, en efectivo (D6-1a): una moneda.</summary>
        public const string Coin = "moneda";

        /// <summary>Un círculo lleno, para el fondo de los íconos de las barras.</summary>
        public const string Disc = "disco";

        /// <summary>
        /// El dibujo de un ícono, por su nombre. Los de las barras son "barra." más el id de la
        /// barra ("barra.felicidad", "barra.salud", "barra.inteligencia", "barra.apariencia",
        /// "barra.energia"); "barra.salud.pulso" es la línea del pulso que va encima del corazón.
        /// Null si no existe.
        /// </summary>
        public static VectorIcon? Draw(string name)
        {
            switch (name)
            {
                // Grupos, línea fina (boceto "Logos A · Línea fina").
                case People:
                    return new VectorIcon().StrokeCircle(9f, 8f, 3.2f).StrokeCircle(16.5f, 9.5f, 2.5f)
                        .Stroke("M3.5 19c0-3.2 2.5-5.2 5.5-5.2s5.5 2 5.5 5.2").Stroke("M14.5 14.4c.6-.2 1.3-.3 2-.3 2.4 0 4 1.6 4 4.4");
                case Studies:
                    return new VectorIcon().Stroke("M3 9.5 12 5l9 4.5-9 4.5z").Stroke("M7 11.5V16c0 1.4 2.2 2.6 5 2.6s5-1.2 5-2.6v-4.5").Stroke("M21 9.5V15");
                case Money:
                    return new VectorIcon().StrokeRect(3f, 6.5f, 18f, 12f, 2.5f).Stroke("M16 12.5h2.5").Stroke("M3 10h18");
                case Home:
                    return new VectorIcon().Stroke("M4 11 12 4.5l8 6.5").Stroke("M6 9.8V19h12V9.8").Stroke("M10 19v-5h4v5");
                case Shopping:
                    return new VectorIcon().Stroke("M6 8h12l-1 11H7z").Stroke("M9 8V6.5a3 3 0 0 1 6 0V8");
                case Health:
                    return new VectorIcon().Stroke("M12 19s-7-4.3-7-9.2A3.8 3.8 0 0 1 12 7.6a3.8 3.8 0 0 1 7 2.2C19 14.7 12 19 12 19z").Stroke("M8.5 12h2l1-2 1.5 4 1-2h1.5");
                case Phone:
                    return new VectorIcon().StrokeRect(7f, 3.5f, 10f, 17f, 2.2f).Stroke("M11 17.5h2");
                case Law:
                    return new VectorIcon().Stroke("M12 4v15").Stroke("M8 19h8").Stroke("M5 7h14").Stroke("M5 7l-2.5 6a2.5 2.5 0 0 0 5 0z").Stroke("M19 7l-2.5 6a2.5 2.5 0 0 0 5 0z");
                case History:
                    return new VectorIcon().Stroke("M5 5.5h6a2 2 0 0 1 2 2V19a1.8 1.8 0 0 0-1.8-1.8H5z").Stroke("M19 5.5h-6").Stroke("M19 5.5v11.7h-5.2A1.8 1.8 0 0 0 12 19");
                case More:
                    return new VectorIcon().FillCircle(6f, 12f, 1.6f).FillCircle(12f, 12f, 1.6f).FillCircle(18f, 12f, 1.6f);
                case GoBack:
                    return new VectorIcon().Stroke("M4.5 12a7.5 7.5 0 1 0 2.2-5.3", 2f).Stroke("M4 4.5v3.2h3.2", 2f).Stroke("M12 8v4.3l2.8 1.8", 2f);
                case Menu:
                    return new VectorIcon().Stroke("M5 7h14").Stroke("M5 12h14").Stroke("M5 17h14");
                case Disc:
                    return new VectorIcon().FillCircle(12f, 12f, 12f);
                case Bill:
                    return new VectorIcon().StrokeRect(2f, 6f, 20f, 12f, 1.8f).StrokeCircle(12f, 12f, 3f)
                        .FillCircle(5.5f, 9.5f, 1f).FillCircle(18.5f, 14.5f, 1f);
                case Coin:
                    return new VectorIcon().StrokeCircle(12f, 12f, 8.5f).StrokeCircle(12f, 12f, 5.5f);

                // Barras de la vida: dibujo blanco que va sobre el círculo de color (boceto S1Filas).
                case "barra.felicidad":
                    return new VectorIcon().StrokeCircle(12f, 12f, 7.5f).FillCircle(9.3f, 10.3f, 1.1f).FillCircle(14.7f, 10.3f, 1.1f)
                        .Stroke("M8.6 13.6c.9 1.5 2 2.2 3.4 2.2s2.5-.7 3.4-2.2");
                case "barra.salud":
                    return new VectorIcon().Fill("M12 18.2s-6-3.7-6-8a3.3 3.3 0 0 1 6-1.9 3.3 3.3 0 0 1 6 1.9c0 4.3-6 8-6 8z");
                case "barra.salud.pulso":
                    return new VectorIcon().Stroke("M8.7 11.6h1.8l.9-1.6 1.3 3.2.9-1.6h1.7", 1.4f);
                case "barra.inteligencia":
                    return new VectorIcon().Fill("M12 5.5a4.6 4.6 0 0 0-2.8 8.3c.5.4.8 1 .8 1.6v.6h4v-.6c0-.6.3-1.2.8-1.6A4.6 4.6 0 0 0 12 5.5z").Stroke("M10.2 18.2h3.6");
                case "barra.apariencia":
                    return new VectorIcon().Fill("M12 5.2l1.9 4 4.3.5-3.2 2.9.9 4.3L12 14.7l-3.9 2.2.9-4.3-3.2-2.9 4.3-.5z");
                case "barra.energia":
                    return new VectorIcon().Fill("M13.4 4.8 7.6 13h4l-1.2 6.2L16.4 11h-4z");
                default:
                    return null;
            }
        }

        /// <summary>El color del círculo de cada barra (boceto S1Filas).</summary>
        public static Color BarColor(string bar)
        {
            switch (bar)
            {
                case "felicidad":
                    return UiFactory.Hex(0xF2A93B);
                case "salud":
                    return UiFactory.Hex(0xE0544B);
                case "inteligencia":
                    return UiFactory.Hex(0x4C7FD6);
                case "apariencia":
                    return UiFactory.Hex(0xB565C9);
                case "energia":
                    return UiFactory.Hex(0x3BAE7A);
                default:
                    return UiFactory.Option;
            }
        }

        /// <summary>El ícono pintado como imagen blanca (se tiñe con <see cref="Image.color"/>). Null si no existe.</summary>
        public static Sprite? Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }

            VectorIcon? icon = Draw(name);
            if (icon == null)
            {
                return null;
            }

            float[] alpha = icon.Rasterize(Pixels);
            var pixels = new Color32[alpha.Length];
            for (int i = 0; i < alpha.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha[i] * 255f));
            }

            var texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, Pixels, Pixels), new Vector2(0.5f, 0.5f), 100f);
            Cache[name] = sprite;
            return sprite;
        }

        /// <summary>Pone un ícono de <paramref name="size"/> puntos dentro de <paramref name="parent"/>, con su color.</summary>
        public static Image Place(Transform parent, string name, float size, Color color)
        {
            RectTransform rect = UiFactory.Child(parent, "Icono " + name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(name);
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = size;
            layout.minHeight = layout.preferredHeight = size;
            rect.sizeDelta = new Vector2(size, size);
            return image;
        }

        /// <summary>
        /// El ícono de una barra: su círculo de color con el dibujo blanco encima (y, en Salud,
        /// la línea del pulso del color del círculo).
        /// </summary>
        public static RectTransform BarIcon(Transform parent, string bar, float size)
        {
            Color color = BarColor(bar);
            Image disc = Place(parent, Disc, size, color);
            RectTransform holder = disc.rectTransform;
            Layer(holder, "barra." + bar, Color.white);
            if (bar == "salud")
            {
                Layer(holder, "barra.salud.pulso", color);
            }

            return holder;
        }

        /// <summary>Un dibujo encima de otro, del mismo tamaño.</summary>
        private static void Layer(RectTransform holder, string name, Color color)
        {
            RectTransform rect = UiFactory.Child(holder, "Capa " + name);
            UiFactory.Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Get(name);
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
    }
}
