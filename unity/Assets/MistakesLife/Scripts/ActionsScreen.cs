#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Lo que puedes hacer con el tiempo parado dentro de un grupo (D3, bocetos D3Grupo y
    /// D3Resultado): la franja con el nombre del grupo, el contador de tardes libres del mes y,
    /// en tarjetas de papel, cada cosa que puedes hacer esta tarde con lo que te da (verde) y
    /// lo que te cuesta (rojo, y las tardes). Al tocar una se hace al tiro
    /// (<see cref="LifePresenter.DoAction"/>) y la misma pantalla cuenta qué pasó y qué cambió,
    /// sin avanzar el tiempo. Abajo, un botón para ver la pantalla de siempre del grupo (tu
    /// libreta, tu gente) y otro para cerrar. Todo lo que dice lo da el núcleo.
    /// </summary>
    public sealed class ActionsScreen
    {
        /// <summary>El color de la franja de cada grupo (Estudios con el azul de la libreta).</summary>
        private static readonly Dictionary<string, Color> Bands = new Dictionary<string, Color>
        {
            { "gente", UiFactory.Hex(0x7A4E63) },
            { "estudios", StudiesScreen.SchoolBand },
            { "trabajo", TeamScreen.Band },
            { "salud", UiFactory.Hex(0x2F6B5A) },
            { "dinero", Things.Wallet },
        };

        private static readonly Color GoodChip = UiFactory.Hex(0xDCEFE3);
        private static readonly Color GoodInk = UiFactory.Hex(0x1D6B4F);
        private static readonly Color BadChip = UiFactory.Hex(0xF8E1DC);
        private static readonly Color BadInk = UiFactory.Hex(0x8A2A20);

        /// <summary>Un puntito del contador: amarillo si la tarde queda, gris si ya se usó.</summary>
        public static readonly Color DotLeft = UiFactory.Accent;
        public static readonly Color DotUsed = UiFactory.Hex(0x3A4566);

        private readonly Transform _parent;
        private RectTransform? _layer;
        private Image _band = null!;
        private TextMeshProUGUI _title = null!;
        private RectTransform _body = null!;
        private Button _open = null!;
        private TextMeshProUGUI _openLabel = null!;
        private TextMeshProUGUI _closeLabel = null!;
        private System.Action? _openGroup;

        public ActionsScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama después de hacer algo, para que la pantalla principal muestre las barras nuevas.</summary>
        public System.Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>
        /// Abre el grupo. <paramref name="openGroup"/> es la pantalla de siempre del grupo (null si
        /// no tiene, como Salud) y <paramref name="openLabelKey"/> la clave del botón que la abre.
        /// </summary>
        public void Show(LifePresenter presenter, string group, string titleKey, System.Action? openGroup, string? openLabelKey)
        {
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            _band.color = Bands.TryGetValue(group, out Color band) ? band : UiFactory.Card;
            _title.text = presenter.Text(titleKey);
            _openGroup = openGroup;
            _open.gameObject.SetActive(openGroup != null && openLabelKey != null);
            _openLabel.text = openLabelKey != null ? presenter.Text(openLabelKey) : string.Empty;
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            DrawList(presenter, group);
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Que puedes hacer", UiFactory.Background);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            RectTransform band = UiFactory.Child(_layer, "Franja");
            _band = band.gameObject.AddComponent<Image>();
            VerticalLayoutGroup bandLayout = band.gameObject.AddComponent<VerticalLayoutGroup>();
            bandLayout.padding = new RectOffset(56, 56, 56, 48);
            bandLayout.childControlWidth = true;
            bandLayout.childControlHeight = true;
            bandLayout.childForceExpandWidth = true;
            bandLayout.childForceExpandHeight = false;
            _title = UiFactory.Label(band, string.Empty, 64, UiFactory.Ink, FontStyles.Normal, Face.Title);

            _body = UiFactory.ScrollColumn(_layer, "Acciones", 40, 28);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = Color.clear;

            RectTransform footer = UiFactory.Column(_layer, "Abajo", 0, 16);
            footer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 8, 40);
            _open = UiFactory.MakeButton(footer, string.Empty, UiFactory.Option, UiFactory.Ink, () =>
            {
                Sounds.Play(Sound.PageTurn);
                Close();
                _openGroup?.Invoke();
            });
            _openLabel = _open.GetComponentInChildren<TextMeshProUGUI>();
            Button close = UiFactory.MakeButton(footer, string.Empty, UiFactory.Accent, UiFactory.AccentText, () =>
            {
                Sounds.Play(Sound.WoodKnock);
                Close();
            });
            close.GetComponent<LayoutElement>().minHeight = 156;
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>El contador y la lista de lo que puedes hacer esta tarde.</summary>
        private void DrawList(LifePresenter presenter, string group)
        {
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            FreeTimeView? free = presenter.FreeTime();
            if (free != null)
            {
                Counter(_body, free);
            }

            IReadOnlyList<DoableActionView> actions = presenter.Actions(group);
            UiFactory.Label(_body, presenter.Text("ui.accion.titulo").ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.MutedInk, FontStyles.Bold)
                .characterSpacing = 2f;
            if (actions.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.accion.vacio"), 38, UiFactory.MutedInk);
                return;
            }

            foreach (DoableActionView action in actions)
            {
                ActionCard(presenter, group, action);
            }
        }

        /// <summary>
        /// Una cosa que puedes hacer: el nombre grande, por qué tiene sentido ahora, lo que da y
        /// lo que cuesta. Si ya no te quedan tardes, se ve apagada y no se puede tocar.
        /// </summary>
        private void ActionCard(LifePresenter presenter, string group, DoableActionView action)
        {
            RectTransform card = UiFactory.Column(_body, action.Title, 32, 12, UiFactory.Paper);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 30, 32);
            CanvasGroup fade = card.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = action.CanAfford ? 1f : 0.45f;

            RectTransform head = UiFactory.Child(card, "Nombre y costo");
            HorizontalLayoutGroup headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.spacing = 20;
            headRow.childControlWidth = true;
            headRow.childControlHeight = true;
            headRow.childForceExpandWidth = false;
            headRow.childForceExpandHeight = false;
            TextMeshProUGUI title = UiFactory.Label(head, action.Title, 44, UiFactory.PaperInk, FontStyles.Bold);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI cost = UiFactory.Label(head, action.Cost, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            cost.alignment = TextAlignmentOptions.TopRight;
            cost.textWrappingMode = TextWrappingModes.NoWrap;

            if (action.Why.Length > 0)
            {
                UiFactory.Label(card, action.Why, 37, UiFactory.PaperMutedInk);
            }

            Chips(card, action.Gives);

            Image hit = card.GetComponent<Image>();
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.interactable = action.CanAfford;
            string id = action.Id;
            int? person = action.PersonId;
            button.onClick.AddListener(() =>
            {
                ActionResultView result = presenter.DoAction(id, person);
                Sounds.Play(Sound.PencilTap);
                DrawResult(presenter, group, result);
                Changed?.Invoke();
            });
        }

        /// <summary>
        /// El resultado, al tiro (boceto D3Resultado): cuándo, qué hiciste, qué pasó, lo que
        /// cambió de verdad y las tardes que te quedan. "Listo" vuelve a la lista del grupo.
        /// </summary>
        private void DrawResult(LifePresenter presenter, string group, ActionResultView result)
        {
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            RectTransform card = UiFactory.Column(_body, "Resultado", 0, 14, UiFactory.Paper);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 36, 40);
            UiFactory.Label(card, presenter.Text("ui.accion.cuando", result.DateLabel).ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold)
                .characterSpacing = 2f;
            UiFactory.Label(card, result.Title, 50, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(card, result.Text, 40, UiFactory.PaperInk);
            Chips(card, result.Changes);

            Counter(_body, result.FreeTime);
            UiFactory.MakeButton(_body, presenter.Text("ui.boton.listo"), UiFactory.Option, UiFactory.Ink, () =>
            {
                Sounds.Play(Sound.PageTurn);
                DrawList(presenter, group);
            });
        }

        /// <summary>Lo que cambia, en fichas: verdes lo bueno, rojas lo que cuesta. Si no caben en una fila, bajan a otra.</summary>
        public static void Chips(RectTransform parent, IReadOnlyList<ChangeView> changes)
        {
            if (changes.Count == 0)
            {
                return;
            }

            // De a dos por fila: así cada ficha tiene ancho para su texto sin achicar la letra.
            RectTransform? row = null;
            for (int i = 0; i < changes.Count; i++)
            {
                if (i % 2 == 0)
                {
                    row = UiFactory.Child(parent, "Fichas");
                    HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 14;
                    layout.childAlignment = TextAnchor.MiddleLeft;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;
                }

                ChangeView change = changes[i];
                RectTransform chip = UiFactory.Child(row!, change.Label);
                UiFactory.RoundedImage(chip.gameObject, change.IsGood ? GoodChip : BadChip);
                HorizontalLayoutGroup chipLayout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
                chipLayout.padding = new RectOffset(22, 22, 10, 10);
                chipLayout.childControlWidth = true;
                chipLayout.childControlHeight = true;
                TextMeshProUGUI label = UiFactory.Label(chip, change.Label, UiFactory.MinFontSize, change.IsGood ? GoodInk : BadInk, FontStyles.Bold);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        /// <summary>
        /// El contador de tardes libres: "5 tardes libres en marzo", la nota de abajo y un puntito
        /// por cada tarde que deja tu horario (amarillos los que quedan). Lo usa también la
        /// pantalla principal, en su franja.
        /// </summary>
        public static RectTransform Counter(RectTransform parent, FreeTimeView free, string? note = null, bool dots = true)
        {
            RectTransform box = UiFactory.Column(parent, "Tardes libres", 0, 8, UiFactory.Card);
            box.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 26, 28);
            UiFactory.Label(box, free.Label, 42, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Label(box, note ?? free.Note, UiFactory.MinFontSize, UiFactory.MutedInk);
            if (dots && free.Total > 0)
            {
                Dots(box, free.Left, free.Total);
            }

            return box;
        }

        private static void Dots(RectTransform parent, int left, int total)
        {
            RectTransform row = UiFactory.Child(parent, "Puntitos");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(0, 0, 8, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            for (int i = 0; i < total; i++)
            {
                RectTransform dot = UiFactory.Child(row, "Tarde");
                Image image = dot.gameObject.AddComponent<Image>();
                image.sprite = Icons.Get(Icons.Disc);
                image.color = i < left ? DotLeft : DotUsed;
                LayoutElement size = dot.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = 26;
                size.minHeight = size.preferredHeight = 26;
            }
        }
    }
}
