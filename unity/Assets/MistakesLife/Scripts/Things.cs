#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// "Tus cosas" como objetos de la vida (docs/diseno-app.md, "La idea"): en vez de un botón
    /// con un nombre, cada tema se toca como lo que es. La billetera verde abre Tu dinero, el
    /// celular abre el celular (con su pantalla encendida y el punto rojo si hay algo nuevo), las
    /// fotos abren tu gente y el diario, y el reloj abre volver al pasado.
    /// Se dibujan con rectángulos redondeados, sin archivos de imagen. Para cambiar cómo se ve
    /// una cosa se cambia solo su método aquí; el resto de la pantalla no se entera.
    /// </summary>
    public static class Things
    {
        /// <summary>Alto de cada cosa (el dibujo y su nombre chico abajo).</summary>
        public const float TileHeight = 230f;

        /// <summary>El material de Tu dinero: verde billetera.</summary>
        public static readonly Color Wallet = UiFactory.Hex(0x1D4A3C);

        private static readonly Color WalletFlap = UiFactory.Hex(0x163A2F);
        private static readonly Color WalletStitch = UiFactory.Hex(0x3C7A62);
        private static readonly Color Bill = UiFactory.Hex(0xB9D3B0);
        private static readonly Color ScreenOff = UiFactory.Hex(0x222C45);
        private static readonly Color ScreenOn = UiFactory.Hex(0x5E7FC4);
        private static readonly Color PhotoSky = UiFactory.Hex(0x7E9DBA);
        private static readonly Color PhotoWarm = UiFactory.Hex(0xC9906C);
        private static readonly Color Skin = UiFactory.Hex(0xE2B694);
        private static readonly Color ClockRim = UiFactory.Hex(0x8A6A4A);
        private static readonly Color FolderTab = UiFactory.Hex(0xB48D4C);

        /// <summary>
        /// La billetera verde con un billete asomado; abajo, lo que tienes, en letra de monto.
        /// Si estás en rojo o debes, el billete no asoma y el monto va en rojo.
        /// </summary>
        public static Button WalletThing(Transform parent, string amount, bool inTrouble, UnityAction onClick)
        {
            RectTransform drawing = Tile(parent, "Billetera", amount, inTrouble ? UiFactory.Bad : UiFactory.Ink, Face.Mono, onClick, out Button button);
            if (!inTrouble)
            {
                Piece(drawing, "Billete", Bill, new Vector2(130f, 60f), new Vector2(-6f, 34f), 6f);
            }

            Image body = Piece(drawing, "Cuero", Wallet, new Vector2(176f, 112f), new Vector2(0f, -8f), 0f);
            button.targetGraphic = body;
            Piece(drawing, "Costura", WalletStitch, new Vector2(156f, 4f), new Vector2(0f, -54f), 0f, 4f);
            Piece(drawing, "Solapa", WalletFlap, new Vector2(176f, 52f), new Vector2(0f, 22f), 0f);
            Piece(drawing, "Broche", UiFactory.Paper, new Vector2(22f, 22f), new Vector2(64f, 0f), 0f, 2.6f);
            return button;
        }

        /// <summary>El celular, un poco chueco como dejado en la mesa: con correos sin leer, la pantalla se enciende y aparece el número.</summary>
        public static Button PhoneThing(Transform parent, string caption, int unread, UnityAction onClick)
        {
            RectTransform drawing = Tile(parent, "Celular", caption, UiFactory.MutedInk, Face.Body, onClick, out Button button);
            Image body = Piece(drawing, "Cuerpo", PhoneScreen.PhoneBody, new Vector2(82f, 146f), Vector2.zero, -8f, 1.6f);
            button.targetGraphic = body;
            Image screen = Piece((RectTransform)body.transform, "Pantalla", unread > 0 ? ScreenOn : ScreenOff, new Vector2(66f, 116f), new Vector2(0f, -2f), 0f, 2.4f);
            Piece((RectTransform)body.transform, "Parlante", ScreenOff, new Vector2(22f, 4f), new Vector2(0f, 64f), 0f, 6f);
            if (unread > 0)
            {
                // Dos líneas claras en la pantalla, como un aviso que llegó.
                Piece((RectTransform)screen.transform, "Aviso", UiFactory.Paper, new Vector2(50f, 10f), new Vector2(0f, 26f), 0f, 3f);
                Piece((RectTransform)screen.transform, "Aviso 2", UiFactory.Paper, new Vector2(34f, 10f), new Vector2(-8f, 10f), 0f, 3f);
                Image dot = Piece(drawing, "Punto rojo", UiFactory.Bad, new Vector2(52f, 52f), new Vector2(40f, 62f), 0f, 1.1f);
                TextMeshProUGUI count = UiFactory.Label(dot.transform, unread > 9 ? "9+" : unread.ToString(), UiFactory.MinFontSize, UiFactory.Paper, FontStyles.Bold);
                UiFactory.Stretch(count.rectTransform);
                count.alignment = TextAlignmentOptions.Center;
            }

            return button;
        }

        /// <summary>Dos fotos de papel, una sobre otra: tu gente y el diario de tu vida.</summary>
        public static Button PhotosThing(Transform parent, string caption, UnityAction onClick)
        {
            RectTransform drawing = Tile(parent, "Fotos", caption, UiFactory.MutedInk, Face.Body, onClick, out Button button);
            Polaroid(drawing, "Foto de atras", PhotoWarm, new Vector2(-26f, 2f), -9f, 1);
            Image front = Polaroid(drawing, "Foto de adelante", PhotoSky, new Vector2(24f, -4f), 7f, 2);
            button.targetGraphic = front;
            return button;
        }

        /// <summary>Un reloj de pared con los punteros hacia atrás: volver al pasado.</summary>
        public static Button ClockThing(Transform parent, string caption, UnityAction onClick)
        {
            RectTransform drawing = Tile(parent, "Reloj", caption, UiFactory.MutedInk, Face.Body, onClick, out Button button);
            Image rim = Piece(drawing, "Marco", ClockRim, new Vector2(134f, 134f), Vector2.zero, 0f, 0.42f);
            button.targetGraphic = rim;
            RectTransform face = (RectTransform)Piece((RectTransform)rim.transform, "Cara", UiFactory.Paper, new Vector2(112f, 112f), Vector2.zero, 0f, 0.5f).transform;
            for (int hour = 0; hour < 12; hour += 3)
            {
                float angle = hour * 30f * Mathf.Deg2Rad;
                Piece(face, "Marca", UiFactory.PaperMutedInk, new Vector2(6f, 6f), new Vector2(Mathf.Sin(angle) * 44f, Mathf.Cos(angle) * 44f), 0f, 6f);
            }

            Hand(face, "Horario", 30f, 8f, 60f);
            Hand(face, "Minutero", 44f, 6f, -30f);
            Piece(face, "Centro", UiFactory.KeyInk, new Vector2(14f, 14f), Vector2.zero, 0f, 4f);
            return button;
        }

        /// <summary>
        /// La carpeta de cartulina del juzgado con una hoja asomada y su timbre; si hay una causa
        /// abierta, la hoja asoma más y el timbre va en rojo.
        /// </summary>
        public static Button FolderThing(Transform parent, string caption, bool open, UnityAction onClick)
        {
            RectTransform drawing = Tile(parent, "Carpeta del juzgado", caption, open ? UiFactory.Bad : UiFactory.MutedInk, Face.Body, onClick, out Button button);
            Piece(drawing, "Pestana", FolderTab, new Vector2(64f, 26f), new Vector2(-48f, 50f), -4f, 2f);
            Piece(drawing, "Hoja", UiFactory.Paper, new Vector2(132f, 104f), new Vector2(6f, open ? 24f : 10f), 3f, 3f);
            Image body = Piece(drawing, "Cartulina", CourtScreen.Cardboard, new Vector2(170f, 112f), new Vector2(0f, -12f), -4f, 2f);
            button.targetGraphic = body;
            Piece((RectTransform)body.transform, "Timbre", open ? UiFactory.KeyInk : FolderTab, new Vector2(54f, 30f), new Vector2(42f, -24f), -10f, 3f);
            return button;
        }

        /// <summary>Un hueco del mismo alto, para que las cosas queden en columnas parejas.</summary>
        public static void Empty(Transform parent)
        {
            RectTransform gap = UiFactory.Child(parent, "Vacio");
            LayoutElement size = gap.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = TileHeight;
        }

        /// <summary>
        /// Una cosa tocable: el dibujo arriba (centrado, sin orden automático: cada pieza va donde
        /// se dice) y su nombre chico abajo.
        /// </summary>
        private static RectTransform Tile(Transform parent, string name, string caption, Color captionColor, Face face, UnityAction onClick, out Button button)
        {
            RectTransform tile = UiFactory.Child(parent, name);
            LayoutElement size = tile.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = TileHeight;

            // Todo el cuadro recibe el toque, aunque el dibujo sea chico.
            Image hit = tile.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            button = tile.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;

            RectTransform drawing = UiFactory.Child(tile, "Dibujo");
            drawing.anchorMin = new Vector2(0.5f, 1f);
            drawing.anchorMax = new Vector2(0.5f, 1f);
            drawing.pivot = new Vector2(0.5f, 0.5f);
            drawing.sizeDelta = new Vector2(180f, 160f);
            drawing.anchoredPosition = new Vector2(0f, -84f);

            TextMeshProUGUI label = UiFactory.Label(tile, caption, UiFactory.MinFontSize, captionColor, face == Face.Mono ? FontStyles.Bold : FontStyles.Normal, face);
            RectTransform text = label.rectTransform;
            text.anchorMin = new Vector2(0f, 0f);
            text.anchorMax = new Vector2(1f, 0f);
            text.pivot = new Vector2(0.5f, 0f);
            text.sizeDelta = new Vector2(0f, 52f);
            text.anchoredPosition = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return drawing;
        }

        /// <summary>Una foto de papel con su imagen y dos cabecitas.</summary>
        private static Image Polaroid(RectTransform parent, string name, Color picture, Vector2 position, float angle, int people)
        {
            Image paper = Piece(parent, name, UiFactory.Paper, new Vector2(104f, 124f), position, angle, 2.2f);
            RectTransform image = (RectTransform)Piece((RectTransform)paper.transform, "Imagen", picture, new Vector2(88f, 84f), new Vector2(0f, 12f), 0f, 6f).transform;
            for (int i = 0; i < people; i++)
            {
                float x = people == 1 ? 0f : (i == 0 ? -18f : 18f);
                Piece(image, "Cabeza", Skin, new Vector2(26f, 26f), new Vector2(x, 4f), 0f, 2.2f);
                Piece(image, "Cuerpo", i == 0 ? UiFactory.PaperOption : UiFactory.Health, new Vector2(34f, 30f), new Vector2(x, -28f), 0f, 2f);
            }

            return paper;
        }

        /// <summary>Un puntero del reloj, que sale del centro con el ángulo dado (0 = las 12).</summary>
        private static void Hand(RectTransform face, string name, float length, float width, float angle)
        {
            RectTransform hand = UiFactory.Child(face, name);
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.pivot = new Vector2(0.5f, 0f);
            hand.sizeDelta = new Vector2(width, length);
            hand.anchoredPosition = Vector2.zero;
            hand.localRotation = Quaternion.Euler(0f, 0f, -angle);
            UiFactory.RoundedImage(hand.gameObject, UiFactory.PaperInk).pixelsPerUnitMultiplier = 6f;
        }

        /// <summary>
        /// Una pieza del dibujo: un rectángulo redondeado de un tamaño, en una posición (desde el
        /// centro) y con un giro. <paramref name="corners"/> más chico redondea más (0,5 en un
        /// cuadrado lo vuelve un círculo).
        /// </summary>
        private static Image Piece(RectTransform parent, string name, Color color, Vector2 size, Vector2 position, float angle, float corners = 1.4f)
        {
            RectTransform piece = UiFactory.Child(parent, name);
            piece.anchorMin = piece.anchorMax = new Vector2(0.5f, 0.5f);
            piece.pivot = new Vector2(0.5f, 0.5f);
            piece.sizeDelta = size;
            piece.anchoredPosition = position;
            piece.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image image = UiFactory.RoundedImage(piece.gameObject, color);
            image.pixelsPerUnitMultiplier = corners;
            image.raycastTarget = false;
            return image;
        }
    }
}
