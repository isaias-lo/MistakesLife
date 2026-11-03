#nullable enable

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
        public static readonly Color Background = new Color(0.96f, 0.94f, 0.90f);
        public static readonly Color Card = Color.white;
        public static readonly Color Ink = new Color(0.16f, 0.15f, 0.14f);
        public static readonly Color MutedInk = new Color(0.45f, 0.43f, 0.40f);
        public static readonly Color Accent = new Color(0.20f, 0.45f, 0.55f);
        public static readonly Color AccentText = Color.white;
        public static readonly Color Option = new Color(0.90f, 0.93f, 0.95f);
        public static readonly Color BarBack = new Color(0.88f, 0.86f, 0.83f);
        public static readonly Color Health = new Color(0.36f, 0.62f, 0.40f);
        public static readonly Color Happiness = new Color(0.90f, 0.66f, 0.25f);
        public static readonly Color Smarts = new Color(0.38f, 0.48f, 0.78f);

        private static Font? _font;

        /// <summary>Fuente que viene con Unity, para no depender de archivos extra.</summary>
        public static Font DefaultFont => _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Lienzo vertical que se escala igual en cualquier celular, más el sistema de toques.</summary>
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

            Image background = Child(go.transform, "Fondo").gameObject.AddComponent<Image>();
            background.color = Background;
            Stretch(background.rectTransform);
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
                rect.gameObject.AddComponent<Image>().color = color.Value;
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

        public static Text Label(Transform parent, string text, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            Text label = Child(parent, "Texto").gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            return label;
        }

        public static Button MakeButton(Transform parent, string text, Color background, Color textColor, UnityAction onClick)
        {
            RectTransform rect = Child(parent, "Boton");
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = background;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            rect.gameObject.AddComponent<LayoutElement>().minHeight = 120;

            Text label = Label(rect, text, 40, textColor);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        /// <summary>Barra de 0 a 100 con su nombre encima. Devuelve el relleno para actualizarlo.</summary>
        public static RectTransform Bar(Transform parent, string title, Color color, out Text label)
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
            scroll.gameObject.AddComponent<Image>().color = Card;
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
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
