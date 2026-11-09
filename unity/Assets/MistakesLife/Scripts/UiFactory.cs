#nullable enable

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma piezas de interfaz desde código (lienzo, textos, botones, barras, listas con scroll).
    /// Así la pantalla no depende de escenas ni prefabs armados a mano en el editor.
    /// Solo forma y color: nada de reglas del juego.
    /// </summary>
    public static class UiFactory
    {
        // Colores acordados en docs/diseno-app.md (sección "Colores"). Para cambiar el
        // estilo de toda la app, se cambian aquí.

        /// <summary>Noche: el fondo de toda la interfaz (#141B2D).</summary>
        public static readonly Color Background = Hex(0x141B2D);

        /// <summary>Noche 2: tarjetas y botones sobre el fondo (#1E2740).</summary>
        public static readonly Color Card = Hex(0x1E2740);

        /// <summary>Botones sobre una tarjeta, un poco más claros que ella.</summary>
        public static readonly Color Option = Hex(0x2B3758);

        /// <summary>Texto principal sobre la noche (#F3F5FA).</summary>
        public static readonly Color Ink = Hex(0xF3F5FA);

        /// <summary>Texto secundario (#C3CBDD); nunca más oscuro, para que se lea bien.</summary>
        public static readonly Color MutedInk = Hex(0xC3CBDD);

        /// <summary>Amarillo del jugador (#F5C842): solo lo tuyo (tu botón principal, lo que elegiste).</summary>
        public static readonly Color Accent = Hex(0xF5C842);

        /// <summary>Texto sobre el amarillo: la noche, para que se lea.</summary>
        public static readonly Color AccentText = Hex(0x141B2D);

        public static readonly Color BarBack = Hex(0x2B3758);
        public static readonly Color Health = Hex(0x4FD1A5);
        public static readonly Color Happiness = Hex(0xF2A65A);
        public static readonly Color Smarts = Hex(0x7FA7FF);

        /// <summary>Papel (#F4EFE4): tarjetas de eventos, boletas y hojas.</summary>
        public static readonly Color Paper = Hex(0xF4EFE4);

        /// <summary>Tinta sobre el papel (#1C2236).</summary>
        public static readonly Color PaperInk = Hex(0x1C2236);

        /// <summary>Tinta suave sobre el papel.</summary>
        public static readonly Color PaperMutedInk = Hex(0x5A6178);

        /// <summary>Botones sobre el papel: el mismo papel, un poco más oscuro.</summary>
        public static readonly Color PaperOption = Hex(0xE6DCC8);

        /// <summary>"Día clave": un rojo ladrillo, como timbre sobre el papel.</summary>
        public static readonly Color KeyInk = Hex(0xB5462B);

        /// <summary>Cambios buenos y malos (siempre acompañados de palabras).</summary>
        public static readonly Color Good = Hex(0x4FD1A5);
        public static readonly Color Bad = Hex(0xFF8A7A);

        /// <summary>Fondo oscuro y semitransparente detrás de un aviso (tutorial, confirmar).</summary>
        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.6f);

        /// <summary>Radio de las esquinas redondeadas, en píxeles de la pantalla de referencia (1080 de ancho).</summary>
        private const int CornerRadius = 28;

        /// <summary>Tamaño mínimo de letra: 12 px en un celular de 390 de ancho (aquí la pantalla mide 1080).</summary>
        public const int MinFontSize = 34;

        /// <summary>Alto mínimo de un botón: 44 px en un celular de 390 de ancho.</summary>
        public const int MinButtonHeight = 124;

        private static Sprite? _rounded;

        /// <summary>Un color escrito como en la web (0xRRGGBB).</summary>
        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        /// <summary>
        /// Rectángulo blanco con esquinas redondeadas, dibujado por código (sin archivos de imagen).
        /// Es "9-slice": las esquinas no se deforman aunque el botón sea largo.
        /// </summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded != null)
                {
                    return _rounded;
                }

                int size = CornerRadius * 2 + 4;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Distancia al centro de la esquina más cercana; afuera del radio es transparente (con borde suave).
                        float cx = Mathf.Clamp(x + 0.5f, CornerRadius, size - CornerRadius);
                        float cy = Mathf.Clamp(y + 0.5f, CornerRadius, size - CornerRadius);
                        float distance = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        byte alpha = (byte)(Mathf.Clamp01(CornerRadius - distance + 0.5f) * 255f);
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                var border = new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius);
                _rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                return _rounded;
            }
        }

        /// <summary>Pone el fondo redondeado a una imagen.</summary>
        public static Image RoundedImage(GameObject target, Color color)
        {
            Image image = target.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        /// <summary>
        /// Capa que tapa toda la pantalla (fuera del orden de la columna), con un color de fondo.
        /// Sirve para el menú de inicio, elegir la vida y el tutorial.
        /// </summary>
        public static RectTransform Overlay(Transform parent, string name, Color background)
        {
            RectTransform rect = Child(parent, name);
            Stretch(rect);
            rect.gameObject.AddComponent<Image>().color = background;
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rect.SetAsLastSibling();
            return rect;
        }

        /// <summary>Lienzo vertical transparente que se escala igual en cualquier celular, más el sistema de toques.</summary>
        public static RectTransform CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // Sin imagen de fondo: el color de fondo lo pinta una cámara (ver HomeScene),
            // así la escena 3D se ve en el hueco que le deja la interfaz.
            return (RectTransform)go.transform;
        }

        /// <summary>Objeto vacío de interfaz, hijo de otro.</summary>
        public static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Hace que un objeto ocupe todo su padre.</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Contenedor que apila a sus hijos de arriba hacia abajo.</summary>
        public static RectTransform Column(Transform parent, string name, int padding, int spacing, Color? color = null)
        {
            RectTransform rect = Child(parent, name);
            if (color.HasValue)
            {
                RoundedImage(rect.gameObject, color.Value);
            }

            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        /// <summary>
        /// Un texto con TextMeshPro (nítido en cualquier pantalla), con la letra pedida.
        /// Nunca más chico que <see cref="MinFontSize"/>.
        /// </summary>
        public static TextMeshProUGUI Label(Transform parent, string text, int size, Color color, FontStyles style = FontStyles.Normal, Face face = Face.Body)
        {
            TextMeshProUGUI label = Child(parent, "Texto").gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset? font = Fonts.Get(face);
            if (font != null)
            {
                label.font = font;
            }

            label.text = text;
            label.fontSize = Mathf.Max(size, MinFontSize);
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        public static Button MakeButton(Transform parent, string text, Color background, Color textColor, UnityAction onClick)
        {
            return MakeButton(parent, text, background, textColor, onClick, 40, MinButtonHeight, 32);
        }

        /// <summary>Botón con tamaño de letra, alto mínimo y margen a elección (para botones chicos).</summary>
        public static Button MakeButton(Transform parent, string text, Color background, Color textColor, UnityAction onClick, int fontSize, int minHeight, int padding)
        {
            RectTransform rect = Child(parent, "Boton");
            Image image = RoundedImage(rect.gameObject, background);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding * 7 / 8, padding * 7 / 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            // El texto queda al centro del botón también de arriba abajo. (No se estira el texto:
            // eso haría que el botón pida todo el alto que sobra en la pantalla.)
            layout.childAlignment = TextAnchor.MiddleCenter;
            rect.gameObject.AddComponent<LayoutElement>().minHeight = Mathf.Max(minHeight, MinButtonHeight);

            TextMeshProUGUI label = Label(rect, text, fontSize, textColor, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        /// <summary>Barra de 0 a 100 con su nombre encima. Devuelve el relleno para actualizarlo.</summary>
        public static RectTransform Bar(Transform parent, string title, Color color, out TextMeshProUGUI label)
        {
            RectTransform column = Column(parent, title, 0, 8);
            label = Label(column, string.Empty, 34, MutedInk);

            RectTransform back = Child(column, "Barra");
            back.gameObject.AddComponent<Image>().color = BarBack;
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

            RectTransform fill = Child(back, "Relleno");
            fill.gameObject.AddComponent<Image>().color = color;
            Stretch(fill);
            return fill;
        }

        /// <summary>Muestra un valor de 0 a 100 en una barra hecha con <see cref="Bar"/>.</summary>
        public static void SetBar(RectTransform fill, int value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
        }

        /// <summary>Lista vertical con scroll que ocupa el espacio que sobra. Devuelve donde se ponen los hijos.</summary>
        public static RectTransform ScrollColumn(Transform parent, string name, int padding, int spacing)
        {
            RectTransform scroll = Child(parent, name);
            RoundedImage(scroll.gameObject, Card);
            scroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = Child(scroll, "Vista");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Column(viewport, "Contenido", padding, spacing);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        /// <summary>Borra todos los hijos de un contenedor.</summary>
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                // Se apaga antes de borrarlo: Unity borra al final del cuadro y,
                // mientras tanto, el orden de la pantalla ya no lo cuenta.
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }
    }
}
