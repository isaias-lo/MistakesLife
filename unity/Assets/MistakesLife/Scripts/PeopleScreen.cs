#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tu gente (D4a, bocetos D4ListaA y D4Ficha). La lista: la franja con el título, los
    /// cercanos arriba, cada uno en su tarjeta de papel con su cara (iniciales), nombre y edad,
    /// qué es tuyo, su vida en una línea y la relación con barra y palabra; abajo "Conocidos",
    /// que parte cerrado y se abre al tocarlo. Al tocar a alguien se abre su ficha en la misma
    /// pantalla: su vida, lo que han vivido, qué pueden hacer (gasta tardes libres, con el
    /// resultado al tiro) y lo que le puedes proponer. Todo lo que dice lo da el núcleo
    /// (<see cref="LifePresenter.People"/> y <see cref="LifePresenter.PersonCard"/>).
    /// </summary>
    public sealed class PeopleScreen
    {
        /// <summary>El color de Tu gente: un ciruela apagado, adulto.</summary>
        public static readonly Color Band = UiFactory.Hex(0x7A4E63);

        private static readonly Color BandInk = UiFactory.Hex(0xEBDDE4);
        private static readonly Color ChipBack = UiFactory.Hex(0xEBDDE4);
        private static readonly Color ChipInk = UiFactory.Hex(0x5A3446);
        private static readonly Color BarBack = UiFactory.Hex(0xDCD3C2);
        private static readonly Color BarGood = UiFactory.Hex(0x3BAE7A);
        private static readonly Color BarMiddle = UiFactory.Hex(0xE0A93B);
        private static readonly Color BarLow = UiFactory.Hex(0xD9534F);
        private static readonly Color Gone = UiFactory.Hex(0x9AA4BD);

        /// <summary>Los colores de las caras: cálidos y apagados, uno por persona según su id.</summary>
        private static readonly Color[] Faces =
        {
            UiFactory.Hex(0xC9906C), UiFactory.Hex(0x8FB3D9), UiFactory.Hex(0xB5A2D6), UiFactory.Hex(0xE0B36A),
            UiFactory.Hex(0x8CC5A5), UiFactory.Hex(0xD98F8F), UiFactory.Hex(0xA9B8C9),
        };

        private readonly Transform _parent;
        private RectTransform? _layer;
        private RectTransform _band = null!;
        private RectTransform _body = null!;
        private TextMeshProUGUI _closeLabel = null!;
        private LifePresenter? _presenter;
        private bool _acquaintancesOpen;

        public PeopleScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama después de hacer algo o pedir un momento, para que la vida se vuelva a dibujar.</summary>
        public System.Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>Abre la lista (<paramref name="personId"/> null) o directo la ficha de alguien.</summary>
        public void Show(LifePresenter presenter, int? personId = null)
        {
            Ensure();
            _presenter = presenter;
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            if (personId.HasValue && presenter.PersonCard(personId.Value) is PersonCardView card)
            {
                DrawCard(card, null);
            }
            else
            {
                _acquaintancesOpen = false;
                DrawList();
            }
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

            _layer = UiFactory.Overlay(_parent, "Tu gente", UiFactory.Background);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            _band = UiFactory.Child(_layer, "Franja");
            _band.gameObject.AddComponent<Image>().color = Band;
            VerticalLayoutGroup bandLayout = _band.gameObject.AddComponent<VerticalLayoutGroup>();
            bandLayout.padding = new RectOffset(48, 48, 44, 40);
            bandLayout.spacing = 18;
            bandLayout.childControlWidth = true;
            bandLayout.childControlHeight = true;
            bandLayout.childForceExpandWidth = true;
            bandLayout.childForceExpandHeight = false;

            _body = UiFactory.ScrollColumn(_layer, "Gente", 40, 22);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = Color.clear;

            RectTransform footer = UiFactory.Column(_layer, "Abajo", 40, 0);
            footer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 8, 40);
            Button close = UiFactory.MakeButton(footer, string.Empty, UiFactory.Accent, UiFactory.AccentText, () =>
            {
                Sounds.Play(Sound.WoodKnock);
                Close();
            });
            close.GetComponent<LayoutElement>().minHeight = 156;
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>La lista: cercanos, y Conocidos cerrado (o abierto) abajo.</summary>
        private void DrawList()
        {
            LifePresenter presenter = _presenter!;
            LifeSnapshot life = presenter.Snapshot();
            PeopleView people = presenter.People();

            UiFactory.Clear(_band);
            UiFactory.Label(_band, presenter.Text("ui.gente.titulo"), 64, UiFactory.Ink, FontStyles.Normal, Face.Title);
            UiFactory.Label(_band, life.PlayerName + " · " + presenter.Text("ui.gente.anos", life.AgeInYears), 38, BandInk);

            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            SectionLabel(_body, presenter.Text("ui.gente.cercanos"));
            foreach (PersonRowView person in people.Close)
            {
                PersonCard(person);
            }

            // Conocidos: una fila oscura que abre o cierra la lista de abajo.
            RectTransform toggle = UiFactory.Child(_body, "Conocidos");
            Image back = UiFactory.RoundedImage(toggle.gameObject, UiFactory.Card);
            HorizontalLayoutGroup row = toggle.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(36, 36, 32, 32);
            row.spacing = 20;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            RectTransform words = UiFactory.Column(toggle, "Texto", 0, 4);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(words, people.AcquaintancesLabel, 42, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Label(words, people.AcquaintancesNote, UiFactory.MinFontSize, UiFactory.MutedInk);
            TextMeshProUGUI arrow = UiFactory.Label(toggle, _acquaintancesOpen ? "−" : "+", 52, UiFactory.MutedInk, FontStyles.Bold);
            arrow.alignment = TextAlignmentOptions.Center;
            Button open = toggle.gameObject.AddComponent<Button>();
            open.targetGraphic = back;
            open.interactable = people.Acquaintances.Count > 0;
            open.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PageTurn);
                _acquaintancesOpen = !_acquaintancesOpen;
                DrawList();
            });

            if (_acquaintancesOpen)
            {
                foreach (PersonRowView person in people.Acquaintances)
                {
                    PersonCard(person);
                }
            }
        }

        /// <summary>
        /// Una persona en la lista: su cara, "Nombre, edad" con lo que es tuyo al lado, su vida en
        /// una línea (si el juego sabe algo) y la relación con barra y palabra. Se toca para abrir su ficha.
        /// </summary>
        private void PersonCard(PersonRowView person)
        {
            RectTransform card = UiFactory.Child(_body, person.Name);
            Image back = UiFactory.RoundedImage(card.gameObject, UiFactory.Paper);
            HorizontalLayoutGroup row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(30, 30, 28, 28);
            row.spacing = 28;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            FaceDisc(card, person, 120f);
            RectTransform words = UiFactory.Column(card, "Texto", 0, 10);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            RectTransform head = UiFactory.Child(words, "Nombre");
            HorizontalLayoutGroup headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.spacing = 16;
            headRow.childAlignment = TextAnchor.MiddleLeft;
            headRow.childControlWidth = true;
            headRow.childControlHeight = true;
            headRow.childForceExpandWidth = false;
            headRow.childForceExpandHeight = false;
            TextMeshProUGUI name = UiFactory.Label(head, person.Name + ", " + person.AgeInYears, 42, UiFactory.PaperInk, FontStyles.Bold);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Chip(head, person.Relation);

            if (person.Life.Length > 0)
            {
                UiFactory.Label(words, person.Life, 36, UiFactory.PaperMutedInk);
            }

            if (person.IsAlive)
            {
                RelationBar(words, person, BarBack, UiFactory.PaperInk);
            }

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            int id = person.Id;
            button.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PhotoClick);
                if (_presenter!.PersonCard(id) is PersonCardView personCard)
                {
                    DrawCard(personCard, null);
                }
            });
        }

        /// <summary>
        /// La ficha de una persona: arriba (en la franja) su cara, nombre, qué es tuyo, edad y
        /// comuna, y la relación; abajo su vida, lo que han vivido, qué pueden hacer y lo que le
        /// puedes proponer. Si recién hiciste algo con ella, el resultado va primero.
        /// </summary>
        private void DrawCard(PersonCardView card, ActionResultView? result)
        {
            LifePresenter presenter = _presenter!;
            PersonRowView person = card.Person;

            UiFactory.Clear(_band);
            Button back = UiFactory.MakeButton(_band, presenter.Text("ui.boton.volver"), Translucent(Color.white, 0.14f), UiFactory.Ink, () =>
            {
                Sounds.Play(Sound.PageTurn);
                DrawList();
            }, UiFactory.MinFontSize, 0, 16);
            back.GetComponent<LayoutElement>().preferredWidth = 260;
            RectTransform top = UiFactory.Child(_band, "Quien");
            HorizontalLayoutGroup topRow = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topRow.spacing = 32;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;
            FaceDisc(top, person, 170f);
            RectTransform who = UiFactory.Column(top, "Nombre", 0, 6);
            who.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(who, person.Name, 58, UiFactory.Ink, FontStyles.Normal, Face.Title);
            UiFactory.Label(who, card.Subtitle, 37, BandInk);
            if (person.IsAlive)
            {
                RelationBar(_band, person, Translucent(Color.white, 0.2f), UiFactory.Ink);
            }

            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            if (result != null)
            {
                RectTransform done = Section(presenter.Text("ui.accion.cuando", result.DateLabel));
                UiFactory.Label(done, result.Title, 46, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
                UiFactory.Label(done, result.Text, 38, UiFactory.PaperInk);
                ActionsScreen.Chips(done, result.Changes);
                UiFactory.Label(done, result.FreeTime.Label, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            }

            if (card.Life.Length > 0)
            {
                RectTransform lifeBox = Section(presenter.Text("ui.gente.vida.titulo"));
                UiFactory.Label(lifeBox, card.Life, 40, UiFactory.PaperInk);
            }

            RectTransform lived = Section(presenter.Text("ui.gente.vivido.titulo"));
            if (card.Lived.Count == 0)
            {
                UiFactory.Label(lived, presenter.Text("ui.gente.vivido.vacio"), 38, UiFactory.PaperMutedInk);
            }

            foreach (LivedView line in card.Lived)
            {
                RectTransform row = UiFactory.Child(lived, "Vivido");
                HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 24;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                TextMeshProUGUI when = UiFactory.Label(row, line.When, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
                LayoutElement whenSize = when.gameObject.AddComponent<LayoutElement>();
                whenSize.minWidth = whenSize.preferredWidth = 170;
                UiFactory.Label(row, line.Text, 38, UiFactory.PaperInk).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }

            if (card.Actions.Count == 0 && card.Requests.Count == 0)
            {
                return;
            }

            RectTransform things = Section(presenter.Text("ui.gente.hacer.titulo"));
            foreach (DoableActionView action in card.Actions)
            {
                DoableRow(things, card, action);
            }

            foreach (ActionView request in card.Requests)
            {
                string eventId = request.EventId;
                int personId = person.Id;
                UiFactory.MakeButton(things, request.Label, UiFactory.PaperOption, UiFactory.PaperInk, () =>
                {
                    Sounds.Play(Sound.PencilTap);
                    if (presenter.Session.Request(eventId, personId))
                    {
                        Close();
                        Changed?.Invoke();
                    }
                }, 38, 0, 24);
            }
        }

        /// <summary>Algo que pueden hacer juntos: el nombre, lo que da en fichas y cuántas tardes cuesta. Al tocarlo se hace al tiro.</summary>
        private void DoableRow(RectTransform parent, PersonCardView card, DoableActionView action)
        {
            RectTransform row = UiFactory.Column(parent, action.Title, 0, 10, UiFactory.PaperOption);
            row.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(28, 28, 24, 26);
            CanvasGroup fade = row.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = action.CanAfford ? 1f : 0.45f;

            RectTransform head = UiFactory.Child(row, "Nombre y costo");
            HorizontalLayoutGroup headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.spacing = 20;
            headRow.childControlWidth = true;
            headRow.childControlHeight = true;
            headRow.childForceExpandWidth = false;
            headRow.childForceExpandHeight = false;
            UiFactory.Label(head, action.Title, 40, UiFactory.PaperInk, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI cost = UiFactory.Label(head, action.Cost, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            cost.textWrappingMode = TextWrappingModes.NoWrap;
            ActionsScreen.Chips(row, action.Gives);

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.interactable = action.CanAfford;
            string id = action.Id;
            int? personId = action.PersonId;
            int cardId = card.Person.Id;
            button.onClick.AddListener(() =>
            {
                ActionResultView result = _presenter!.DoAction(id, personId);
                Sounds.Play(Sound.PencilTap);
                Changed?.Invoke();
                if (_presenter.PersonCard(cardId) is PersonCardView fresh)
                {
                    DrawCard(fresh, result);
                }
            });
        }

        /// <summary>Una sección de la ficha: tarjeta de papel con su título chico en mayúscula.</summary>
        private RectTransform Section(string title)
        {
            RectTransform box = UiFactory.Column(_body, title, 0, 14, UiFactory.Paper);
            box.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 30, 34);
            UiFactory.Label(box, title.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold).characterSpacing = 2f;
            return box;
        }

        private static void SectionLabel(RectTransform parent, string text)
        {
            UiFactory.Label(parent, text.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.MutedInk, FontStyles.Bold).characterSpacing = 2f;
        }

        /// <summary>Qué es tuyo, en una ficha chica de color ciruela claro ("Mamá", "Amistad").</summary>
        private static void Chip(RectTransform parent, string text)
        {
            RectTransform chip = UiFactory.Child(parent, text);
            UiFactory.RoundedImage(chip.gameObject, ChipBack);
            HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 6, 6);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            TextMeshProUGUI label = UiFactory.Label(chip, text, UiFactory.MinFontSize, ChipInk, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>La barra de la relación (verde, amarilla o roja) con su palabra al lado. Nada de números.</summary>
        private static void RelationBar(RectTransform parent, PersonRowView person, Color back, Color ink)
        {
            RectTransform row = UiFactory.Child(parent, "Relacion");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            RectTransform bar = UiFactory.Child(row, "Barra");
            UiFactory.RoundedImage(bar.gameObject, back);
            LayoutElement barSize = bar.gameObject.AddComponent<LayoutElement>();
            barSize.flexibleWidth = 1;
            barSize.minHeight = barSize.preferredHeight = 24;
            RectTransform fill = UiFactory.Child(bar, "Relleno");
            UiFactory.RoundedImage(fill.gameObject, person.Level == LifeBarLevel.Good ? BarGood : person.Level == LifeBarLevel.Middle ? BarMiddle : BarLow);
            UiFactory.Stretch(fill);
            fill.anchorMax = new Vector2(Mathf.Clamp01(person.Closeness / 100f), 1f);
            fill.gameObject.SetActive(person.Closeness > 0);

            TextMeshProUGUI word = UiFactory.Label(row, Capitalized(person.Feeling), UiFactory.MinFontSize, ink, FontStyles.Bold);
            word.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>La cara: un círculo de color con las iniciales (gris si la persona murió).</summary>
        private static void FaceDisc(RectTransform parent, PersonRowView person, float size)
        {
            RectTransform disc = UiFactory.Child(parent, "Cara");
            Image image = disc.gameObject.AddComponent<Image>();
            image.sprite = Icons.Get(Icons.Disc);
            image.color = person.IsAlive ? Faces[Mathf.Abs(person.Id) % Faces.Length] : Gone;
            LayoutElement discSize = disc.gameObject.AddComponent<LayoutElement>();
            discSize.minWidth = discSize.preferredWidth = size;
            discSize.minHeight = discSize.preferredHeight = size;
            TextMeshProUGUI initials = UiFactory.Label(disc, person.Initials, Mathf.RoundToInt(size * 0.36f), UiFactory.PaperInk, FontStyles.Bold);
            UiFactory.Stretch(initials.rectTransform);
            initials.alignment = TextAlignmentOptions.Center;
        }

        private static string Capitalized(string text) =>
            text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        private static Color Translucent(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
