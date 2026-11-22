#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Cada sección es un lugar que se toca (D5c, bocetos BEstudios y BTrabajo, docs/diseno-juego.md).
    /// Al abrir Estudios o Trabajo, la escena 3D crece y es el menú: tu sala, tu aula o tu pega
    /// con la gente real, cada una con su nombre encima. Tocas a alguien (su figura o su nombre)
    /// y abajo sale lo que pueden hacer juntos, con lo que cuesta en tardes y lo que da; tocas
    /// tu libreta, la pizarra o tu agenda y se abre lo suyo. La lista de siempre sigue abajo,
    /// a un toque. Todo lo que dice lo da el núcleo (<see cref="LifePresenter.Place"/> y
    /// <see cref="LifePresenter.PersonCard"/>); aquí solo se dibuja.
    /// </summary>
    public sealed class PlaceScreen
    {
        private static readonly Color Bar = new Color(0.08f, 0.11f, 0.18f, 0.92f);
        private static readonly Color TagBack = UiFactory.Hex(0xF4EFE4);
        private static readonly Color ThingBack = new Color(0.08f, 0.11f, 0.18f, 0.88f);

        /// <summary>Alto de la hoja de abajo sin nadie elegido, y con alguien elegido.</summary>
        private const float IdleSheet = 520f;
        private const float PersonSheet = 1040f;

        private readonly Transform _parent;
        private readonly HomeScene _home;
        private RectTransform? _layer;
        private TextMeshProUGUI _title = null!;
        private TextMeshProUGUI _freeTime = null!;
        private TextMeshProUGUI _hint = null!;
        private TextMeshProUGUI _backLabel = null!;
        private RectTransform _hole = null!;
        private RectTransform _tags = null!;
        private LayoutElement _sheetSize = null!;
        private RectTransform _body = null!;
        private PlaceTags _follow = null!;

        private LifePresenter? _presenter;
        private string _group = "estudios";
        private PlaceView? _place;
        private int? _selected;
        private SceneView _previousView;
        private RectTransform? _previousFocus;

        public PlaceScreen(Transform parent, HomeScene home)
        {
            _parent = parent;
            _home = home;
        }

        /// <summary>Se hizo algo con el tiempo parado: la vida se vuelve a dibujar.</summary>
        public System.Action? Changed { get; set; }

        /// <summary>Se pidió un momento (pedir un aumento, proponer algo): se cierra el lugar para decidirlo en su carta.</summary>
        public System.Action? Requested { get; set; }

        /// <summary>Se abrió o se cerró el lugar (la vida esconde o muestra su hoja).</summary>
        public System.Action<bool>? Toggled { get; set; }

        /// <summary>La lista de siempre del grupo (el curso o el equipo).</summary>
        public System.Action<string>? OpenList { get; set; }

        /// <summary>Tu libreta de notas.</summary>
        public System.Action? OpenNotebook { get; set; }

        /// <summary>Tus ramos con preguntas (estudiar de verdad, D5a).</summary>
        public System.Action? OpenStudy { get; set; }

        /// <summary>Tu agenda de la pega, con las tareas del mes.</summary>
        public System.Action? OpenAgenda { get; set; }

        /// <summary>Lo demás que puedes hacer en el grupo con el tiempo parado (D3).</summary>
        public System.Action<string>? OpenOthers { get; set; }

        /// <summary>La ficha completa de una persona, en Tu gente.</summary>
        public System.Action<int>? OpenCard { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>Si ese grupo tiene un lugar que se toca ahora.</summary>
        public static bool Has(LifePresenter presenter, string group) => presenter.Place(group) != null;

        /// <summary>Abre el lugar de un grupo ("estudios" o "trabajo").</summary>
        public void Show(LifePresenter presenter, string group)
        {
            PlaceView? place = presenter.Place(group);
            if (place == null)
            {
                return;
            }

            Ensure();
            _presenter = presenter;
            _group = group;
            _selected = null;
            if (!IsOpen)
            {
                _previousView = _home.View;
                _previousFocus = _home.Focus;
            }

            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            _home.Focus = _hole;
            _home.View = SceneView.Place;
            Toggled?.Invoke(true);
            Draw(place, null);
            Sounds.Play(Sound.PageTurn);
        }

        /// <summary>Cierra el lugar y la escena vuelve a lo que se veía antes.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _layer!.gameObject.SetActive(false);
            _home.Focus = _previousFocus;
            _home.View = _previousView;
            Toggled?.Invoke(false);
        }

        /// <summary>Vuelve a dibujar el lugar (después de hacer algo, o si la vida cambió debajo).</summary>
        public void Redraw()
        {
            if (!IsOpen || _presenter == null)
            {
                return;
            }

            PlaceView? place = _presenter.Place(_group);
            if (place == null)
            {
                Close();
                return;
            }

            Draw(place, null);
        }

        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Lugar", Color.clear);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            // Arriba: volver, el nombre del lugar y tus tardes libres.
            RectTransform top = UiFactory.Child(_layer, "Barra del lugar");
            top.gameObject.AddComponent<Image>().color = Bar;
            HorizontalLayoutGroup topRow = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topRow.padding = new RectOffset(32, 32, 28, 28);
            topRow.spacing = 24;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;
            Button back = UiFactory.MakeButton(top, string.Empty, UiFactory.Option, UiFactory.Ink, () =>
            {
                Sounds.Play(Sound.WoodKnock);
                Close();
            }, UiFactory.MinFontSize, 0, 16);
            back.GetComponent<LayoutElement>().preferredWidth = 220;
            _backLabel = back.GetComponentInChildren<TextMeshProUGUI>();
            _title = UiFactory.Label(top, string.Empty, 48, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            RectTransform chip = UiFactory.Child(top, "Tardes libres");
            UiFactory.RoundedImage(chip.gameObject, UiFactory.Accent);
            HorizontalLayoutGroup chipRow = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipRow.padding = new RectOffset(24, 24, 12, 12);
            chipRow.childControlWidth = true;
            chipRow.childControlHeight = true;
            _freeTime = UiFactory.Label(chip, string.Empty, UiFactory.MinFontSize, UiFactory.AccentText, FontStyles.Bold);
            _freeTime.textWrappingMode = TextWrappingModes.NoWrap;

            // El hueco: la escena se ve aquí; tocar sin arrastrar elige lo que está ahí, arrastrar gira.
            _hole = UiFactory.Child(_layer, "Hueco del lugar");
            LayoutElement hole = _hole.gameObject.AddComponent<LayoutElement>();
            hole.flexibleHeight = 1;
            hole.minHeight = 600;
            OrbitPad pad = OrbitPad.AddTo(_hole, _home);
            pad.Tapped = Tapped;

            RectTransform hintRow = UiFactory.Child(_hole, "Ayuda");
            hintRow.anchorMin = new Vector2(0.5f, 1f);
            hintRow.anchorMax = new Vector2(0.5f, 1f);
            hintRow.pivot = new Vector2(0.5f, 1f);
            hintRow.anchoredPosition = new Vector2(0f, -24f);
            UiFactory.RoundedImage(hintRow.gameObject, ThingBack).raycastTarget = false;
            HorizontalLayoutGroup hintLayout = hintRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hintLayout.padding = new RectOffset(28, 28, 12, 12);
            hintLayout.childControlWidth = true;
            hintLayout.childControlHeight = true;
            hintRow.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hintRow.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _hint = UiFactory.Label(hintRow, string.Empty, UiFactory.MinFontSize, UiFactory.Ink, FontStyles.Bold);
            _hint.textWrappingMode = TextWrappingModes.NoWrap;

            // Los nombres van encima de la escena y la siguen cuando se gira.
            _tags = UiFactory.Child(_hole, "Nombres");
            UiFactory.Stretch(_tags);
            _follow = _layer.gameObject.AddComponent<PlaceTags>();
            _follow.Home = _home;

            // Abajo, la hoja de papel: lo que se puede hacer con lo que tocaste.
            RectTransform sheet = UiFactory.Column(_layer, "Hoja del lugar", 0, 0, UiFactory.Paper);
            _sheetSize = sheet.gameObject.AddComponent<LayoutElement>();
            _sheetSize.preferredHeight = IdleSheet;
            _body = UiFactory.ScrollColumn(sheet, "Lo que tocaste", 40, 20);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = Color.clear;
        }

        private void Draw(PlaceView place, ActionResultView? result)
        {
            LifePresenter presenter = _presenter!;
            _place = place;
            _home.ShowPlace(place);
            _backLabel.text = presenter.Text("ui.boton.volver");
            _title.text = place.Title;
            _hint.text = place.Hint;
            FreeTimeView? free = presenter.FreeTime();
            _freeTime.text = free?.Label ?? string.Empty;
            _freeTime.transform.parent.gameObject.SetActive(free != null);
            DrawTags(place);

            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            PersonCardView? card = _selected.HasValue ? presenter.PersonCard(_selected.Value) : null;
            if (card == null)
            {
                _selected = null;
                _sheetSize.preferredHeight = IdleSheet;
                DrawIdle(place);
            }
            else
            {
                _sheetSize.preferredHeight = PersonSheet;
                DrawPerson(place, card, result);
            }
        }

        /// <summary>Un nombre sobre cada persona y una etiqueta sobre cada cosa; se tocan igual que la figura.</summary>
        private void DrawTags(PlaceView place)
        {
            UiFactory.Clear(_tags);
            var follow = new List<(string Id, RectTransform Tag)>();
            void Add(PlacePersonView person)
            {
                bool chosen = _selected == person.Id;
                int id = person.Id;
                follow.Add((PlaceBuilder.PersonPrefix + person.Id, Tag(person.Tag, chosen ? UiFactory.Accent : TagBack, UiFactory.PaperInk, () => Select(id))));
            }

            if (place.Lead != null)
            {
                Add(place.Lead);
            }

            foreach (PlacePersonView person in place.People)
            {
                Add(person);
            }

            foreach (PlaceThingView thing in place.Things)
            {
                string id = thing.Id;
                follow.Add((thing.Id, Tag(thing.Label, ThingBack, UiFactory.Ink, () => Touch(id))));
            }

            _follow.Follow(follow);
        }

        /// <summary>Una etiqueta redonda que se puede tocar (el nombre de alguien o una cosa).</summary>
        private RectTransform Tag(string text, Color back, Color ink, UnityEngine.Events.UnityAction onTap)
        {
            RectTransform tag = UiFactory.Child(_tags, text);
            tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 0.5f);
            tag.pivot = new Vector2(0.5f, 0f);
            Image image = UiFactory.RoundedImage(tag.gameObject, back);
            Button button = tag.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onTap);
            HorizontalLayoutGroup layout = tag.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            ContentSizeFitter fit = tag.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI label = UiFactory.Label(tag, text, UiFactory.MinFontSize, ink, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return tag;
        }

        /// <summary>Se tocó la escena: una persona abre lo suyo; una cosa, su pantalla.</summary>
        private void Tapped(Vector2 screenPoint)
        {
            string? id = _home.Pick(screenPoint);
            if (id == null)
            {
                return;
            }

            if (id.StartsWith(PlaceBuilder.PersonPrefix, System.StringComparison.Ordinal)
                && int.TryParse(id.Substring(PlaceBuilder.PersonPrefix.Length), out int person))
            {
                Select(person);
            }
            else
            {
                Touch(id);
            }
        }

        /// <summary>Elige a alguien como si se hubiera tocado (para las capturas).</summary>
        public void SelectForCapture(int personId) => Select(personId);

        private void Select(int personId)
        {
            Sounds.Play(Sound.PhotoClick);
            _selected = personId;
            Draw(_presenter!.Place(_group) ?? _place!, null);
        }

        private void Touch(string thingId)
        {
            if (_place == null || !HasThing(_place, thingId))
            {
                return;
            }

            Sounds.Play(Sound.PageTurn);
            switch (thingId)
            {
                case PlaceView.Notebook:
                    OpenNotebook?.Invoke();
                    break;
                case PlaceView.Board:
                    OpenStudy?.Invoke();
                    break;
                case PlaceView.Agenda:
                    OpenAgenda?.Invoke();
                    break;
            }
        }

        private static bool HasThing(PlaceView place, string id)
        {
            foreach (PlaceThingView thing in place.Things)
            {
                if (thing.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sin nadie elegido: qué se puede tocar y los botones a la lista de siempre.</summary>
        private void DrawIdle(PlaceView place)
        {
            LifePresenter presenter = _presenter!;
            UiFactory.Label(_body, place.Hint, 40, UiFactory.PaperInk, FontStyles.Bold);
            if (_group == "estudios" && presenter.StudySubjects().Count > 0)
            {
                AddButton(presenter.Text("ui.lugar.ver_ramos"), () => OpenStudy?.Invoke());
            }

            if (presenter.FreeTime() != null && presenter.Actions(_group).Count > 0)
            {
                string group = _group;
                AddButton(presenter.Text("ui.lugar.mas_acciones"), () => OpenOthers?.Invoke(group));
            }

            string list = _group;
            AddButton(presenter.Text("ui.lugar.ver_lista." + _group), () => OpenList?.Invoke(list));
        }

        /// <summary>
        /// Con alguien elegido: quién es, su vida en una línea y lo que pueden hacer juntos (cada
        /// cosa con su costo en tardes y lo que da); lo que le puedes pedir y, si es tu jefa, lo
        /// de la pega. Al final, su ficha completa.
        /// </summary>
        private void DrawPerson(PlaceView place, PersonCardView card, ActionResultView? result)
        {
            LifePresenter presenter = _presenter!;
            PlacePersonView? who = Find(place, card.Person.Id);
            string role = who?.Role ?? card.Person.Relation;
            TextMeshProUGUI kicker = UiFactory.Label(_body, presenter.Text("ui.lugar.tocaste", card.Person.Name.Split(' ')[0].ToUpperInvariant()) + " · " + role.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            kicker.characterSpacing = 2f;
            UiFactory.Label(_body, card.Person.Name, 54, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(_body, card.Subtitle + " · " + Capitalized(card.Person.Feeling), 38, UiFactory.PaperMutedInk);
            if (card.Life.Length > 0)
            {
                UiFactory.Label(_body, card.Life, 38, UiFactory.PaperInk);
            }

            if (result != null)
            {
                RectTransform done = UiFactory.Column(_body, "Lo que pasó", 28, 10, UiFactory.PaperOption);
                UiFactory.Label(done, result.Title, 42, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
                UiFactory.Label(done, result.Text, 38, UiFactory.PaperInk);
                ActionsScreen.Chips(done, result.Changes);
            }

            bool any = false;
            foreach (DoableActionView action in card.Actions)
            {
                any = true;
                ActionRow(card, action);
            }

            int personId = card.Person.Id;
            foreach (ActionView request in card.Requests)
            {
                any = true;
                string eventId = request.EventId;
                AddButton(request.Label, () =>
                {
                    if (presenter.Session.Request(eventId, personId))
                    {
                        Close();
                        Requested?.Invoke();
                    }
                });
            }

            if (who != null && who.Lead)
            {
                foreach (ActionView ask in place.LeadActions)
                {
                    any = true;
                    string eventId = ask.EventId;
                    AddButton(ask.Label, () =>
                    {
                        if (presenter.Session.Request(eventId))
                        {
                            Close();
                            Requested?.Invoke();
                        }
                    });
                }
            }

            if (!any)
            {
                UiFactory.Label(_body, presenter.Text("ui.lugar.nada"), 38, UiFactory.PaperMutedInk);
            }

            AddButton(presenter.Text("ui.lugar.ficha"), () => OpenCard?.Invoke(personId));
        }

        /// <summary>Algo que pueden hacer juntos: el nombre, cuántas tardes cuesta y lo que da. Se hace al tiro.</summary>
        private void ActionRow(PersonCardView card, DoableActionView action)
        {
            RectTransform row = UiFactory.Column(_body, action.Title, 0, 10, UiFactory.PaperOption);
            row.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(28, 28, 22, 24);
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
            if (action.Why.Length > 0)
            {
                UiFactory.Label(row, action.Why, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            }

            ActionsScreen.Chips(row, action.Gives);

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.interactable = action.CanAfford;
            string id = action.Id;
            int? personId = action.PersonId;
            button.onClick.AddListener(() =>
            {
                ActionResultView done = _presenter!.DoAction(id, personId);
                Sounds.Play(Sound.PencilTap);
                Changed?.Invoke();
                PlaceView? fresh = _presenter.Place(_group);
                if (fresh == null)
                {
                    Close();
                    return;
                }

                Draw(fresh, done);
            });
        }

        private void AddButton(string text, UnityEngine.Events.UnityAction onTap)
        {
            UiFactory.MakeButton(_body, text, UiFactory.PaperOption, UiFactory.PaperInk, () =>
            {
                Sounds.Play(Sound.PencilTap);
                onTap();
            }, 38, 0, 24);
        }

        private static PlacePersonView? Find(PlaceView place, int id)
        {
            if (place.Lead != null && place.Lead.Id == id)
            {
                return place.Lead;
            }

            foreach (PlacePersonView person in place.People)
            {
                if (person.Id == id)
                {
                    return person;
                }
            }

            return null;
        }

        private static string Capitalized(string text) =>
            text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
