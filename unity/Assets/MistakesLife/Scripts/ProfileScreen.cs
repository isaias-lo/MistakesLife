#nullable enable

using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tu ficha (D6-1a, boceto aprobado por Isaias el 22-11-2026): se abre al tocar tu nombre en la
    /// cabecera. Una tarjeta de papel con tu cara, tu nombre completo, tu edad y cumpleaños, y lo que
    /// eres hoy en filas (LifePresenter.Profile()). Se cierra tocando fuera.
    /// </summary>
    public sealed class ProfileScreen
    {
        private readonly Transform _parent;
        private readonly PlayerPortrait _portrait;
        private RectTransform? _layer;
        private RectTransform _card = null!;

        public ProfileScreen(Transform parent, PlayerPortrait portrait)
        {
            _parent = parent;
            _portrait = portrait;
        }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        public void Show(LifePresenter presenter)
        {
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Draw(presenter, presenter.Profile());
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Arma una sola vez: el fondo oscuro que cierra al tocarlo y la tarjeta de papel arriba.</summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Tu ficha", UiFactory.Shade);
            Button outside = _layer.gameObject.AddComponent<Button>();
            outside.transition = Selectable.Transition.None;
            outside.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PageTurn);
                Close();
            });

            _card = UiFactory.Column(_layer, "Tarjeta", 48, 18, UiFactory.Paper);
            _card.anchorMin = new Vector2(0.04f, 1f);
            _card.anchorMax = new Vector2(0.96f, 1f);
            _card.pivot = new Vector2(0.5f, 1f);
            _card.anchoredPosition = new Vector2(0f, -200f);
            ContentSizeFitter fit = _card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Tocar la tarjeta no la cierra: su propio botón (que no hace nada) se queda con el toque.
            _card.GetComponent<Image>().raycastTarget = true;
            _card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        }

        private void Draw(LifePresenter presenter, ProfileView profile)
        {
            UiFactory.Clear(_card);

            RectTransform top = UiFactory.Child(_card, "Quien");
            HorizontalLayoutGroup row = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 32;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            PortraitImage(top, _portrait.Texture, 180);
            RectTransform names = UiFactory.Column(top, "Nombre", 0, 6);
            names.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI name = UiFactory.Label(names, profile.Name, 60, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            name.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Label(names, profile.AgeLine, 36, UiFactory.PaperMutedInk);

            foreach (ProfileRowView line in profile.Rows)
            {
                Line(line);
            }

            TextMeshProUGUI hint = UiFactory.Label(_card, presenter.Text("ui.ficha.cerrar"), UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            hint.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>Una fila: a la izquierda qué es; a la derecha lo principal y, debajo, el detalle.</summary>
        private void Line(ProfileRowView line)
        {
            RectTransform rule = UiFactory.Child(_card, "Raya");
            rule.gameObject.AddComponent<Image>().color = UiFactory.PaperOption;
            LayoutElement ruleSize = rule.gameObject.AddComponent<LayoutElement>();
            ruleSize.minHeight = ruleSize.preferredHeight = 3;

            RectTransform rect = UiFactory.Child(_card, "Fila " + line.Id);
            HorizontalLayoutGroup row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 24;
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            TextMeshProUGUI label = UiFactory.Label(rect, line.Label, 36, UiFactory.PaperMutedInk);
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;

            RectTransform right = UiFactory.Column(rect, "Valor", 0, 2);
            right.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI value = UiFactory.Label(right, line.Value, 40, UiFactory.PaperInk, FontStyles.Bold);
            value.alignment = TextAlignmentOptions.TopRight;
            value.textWrappingMode = TextWrappingModes.Normal;
            if (line.Detail.Length > 0)
            {
                TextMeshProUGUI detail = UiFactory.Label(right, line.Detail, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
                detail.alignment = TextAlignmentOptions.TopRight;
                detail.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        /// <summary>Tu cara en un círculo con anillo amarillo (lo tuyo).</summary>
        public static RawImage PortraitImage(Transform parent, Texture texture, float size)
        {
            RectTransform ring = UiFactory.Child(parent, "Retrato");
            Image ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = Icons.Get(Icons.Disc);
            ringImage.color = UiFactory.Accent;
            ringImage.raycastTarget = false;
            LayoutElement layout = ring.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = size;
            layout.minHeight = layout.preferredHeight = size;

            RectTransform mask = UiFactory.Child(ring, "Circulo");
            UiFactory.Stretch(mask);
            float border = Mathf.Max(5f, size * 0.05f);
            mask.offsetMin = new Vector2(border, border);
            mask.offsetMax = new Vector2(-border, -border);
            Image maskImage = mask.gameObject.AddComponent<Image>();
            maskImage.sprite = Icons.Get(Icons.Disc);
            maskImage.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform photo = UiFactory.Child(mask, "Cara");
            UiFactory.Stretch(photo);
            RawImage image = photo.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }
    }
}
