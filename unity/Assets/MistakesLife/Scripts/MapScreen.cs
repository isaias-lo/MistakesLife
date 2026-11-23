#nullable enable

using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tu gente abre el mapa chico de tu barrio (D5c2, bocetos BMapa, BLugar, BElegir y BSalida en
    /// docs/diseno-juego.md). Tocas un lugar (la plaza, el mall con cine, el café, la fonda en
    /// septiembre si vives en Chile) y abajo eliges <b>qué hacer</b>, con lo que cuesta y lo que trae,
    /// y <b>con quién</b>: 3 sugeridos grandes con su motivo real y "Elegir a otra persona" con
    /// buscador y pestañas, nunca una lista larga con letra chica. Al ir, la escena muestra la
    /// salida: el lugar contigo y con esa persona. Todo lo que dice lo da el núcleo
    /// (<see cref="LifePresenter.Neighborhood"/>, <see cref="LifePresenter.PlaceActivities"/>,
    /// <see cref="LifePresenter.Companions"/> y <see cref="LifePresenter.Outing"/>); aquí solo se dibuja.
    /// </summary>
    public sealed class MapScreen
    {
        private static readonly Color Bar = new Color(0.08f, 0.11f, 0.18f, 0.92f);
        private static readonly Color TagBack = UiFactory.Hex(0xF4EFE4);
        private static readonly Color HintBack = new Color(0.08f, 0.11f, 0.18f, 0.88f);
        private static readonly Color Chosen = UiFactory.Hex(0xFFF3C4);

        /// <summary>Alto de la hoja de abajo: con el mapa solo, con un lugar elegido y al elegir con quién.</summary>
        private const float IdleSheet = 560f;
        private const float PlaceSheet = 1180f;
        private const float ChooseSheet = 1500f;

        private readonly Transform _parent;
        private readonly HomeScene _home;
        private RectTransform? _layer;
        private TextMeshProUGUI _title = null!;
        private TextMeshProUGUI _freeTime = null!;
        private LayoutElement _freeTimeSize = null!;
        private TextMeshProUGUI _hint = null!;
        private TextMeshProUGUI _backLabel = null!;
        private RectTransform _hole = null!;
        private RectTransform _tags = null!;
        private LayoutElement _sheetSize = null!;
        private RectTransform _body = null!;
        private PlaceTags _follow = null!;

        private LifePresenter? _presenter;
        private HomeMemberView? _player;
        private string? _placeId;
        private string? _activityId;
        private int? _companion;
        private bool _alone;
        private bool _choosing;
        private string _tab = string.Empty;
        private string _query = string.Empty;
        private ActionResultView? _result;
        private OutingView? _outing;
        private SceneView _previousView;
        private RectTransform? _previousFocus;

        public MapScreen(Transform parent, HomeScene home)
        {
            _parent = parent;
            _home = home;
        }

        /// <summary>Se hizo algo con el tiempo parado: la vida se vuelve a dibujar.</summary>
        public System.Action? Changed { get; set; }

        /// <summary>Se abrió o se cerró el mapa (la vida esconde o muestra su hoja).</summary>
        public System.Action<bool>? Toggled { get; set; }

        /// <summary>La lista de siempre de Tu gente.</summary>
        public System.Action? OpenPeople { get; set; }

        /// <summary>La ficha de una persona.</summary>
        public System.Action<int>? OpenCard { get; set; }

        /// <summary>Tu sala o tu pega ("estudios" o "trabajo"): se cierra el mapa y se abre ese lugar.</summary>
        public System.Action<string>? OpenSection { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>Si hay mapa del barrio ahora (desde los 6 años, con horario).</summary>
        public static bool Has(LifePresenter presenter) => presenter.Neighborhood() != null;

        /// <summary>Abre el mapa de tu barrio.</summary>
        public void Show(LifePresenter presenter)
        {
            if (presenter.Neighborhood() == null)
            {
                return;
            }

            Ensure();
            _presenter = presenter;
            _player = presenter.Snapshot().Home.Members.FirstOrDefault(m => m.IsPlayer);
            Reset();
            if (!IsOpen)
            {
                _previousView = _home.View;
                _previousFocus = _home.Focus;
            }

            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            _home.Focus = _hole;
            _home.View = SceneView.Map;
            Toggled?.Invoke(true);
            Draw();
            Sounds.Play(Sound.PageTurn);
        }

        /// <summary>Cierra el mapa y la escena vuelve a lo que se veía antes.</summary>
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

        /// <summary>Vuelve a dibujar (si la vida cambió debajo).</summary>
        public void Redraw()
        {
            if (IsOpen && _presenter != null)
            {
                Draw();
            }
        }

        /// <summary>Para las capturas: toca un lugar y, si se pide, elige lo primero que se puede hacer, abre "elegir a otra persona" o va.</summary>
        public bool PrepareForCapture(string placeId, bool choose, bool go)
        {
            if (_presenter == null || !_presenter.Neighborhood()!.Places.Any(p => p.Id == placeId))
            {
                return false;
            }

            SelectPlace(placeId);
            PlaceActivityView? first = _presenter.PlaceActivities(placeId).FirstOrDefault(a => a.CanAfford);
            if (first == null)
            {
                return true;
            }

            SelectActivity(first);
            if (choose && first.WithSomeone)
            {
                _choosing = true;
                Draw();
            }
            else if (go)
            {
                Go();
            }

            return true;
        }

        private void Reset()
        {
            _placeId = null;
            _activityId = null;
            _companion = null;
            _alone = false;
            _choosing = false;
            _query = string.Empty;
            _tab = string.Empty;
            _result = null;
            _outing = null;
        }

        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Mapa del barrio", Color.clear);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            // Arriba: volver, "Tu gente · Puente Alto" y tus tardes libres.
            RectTransform top = UiFactory.Child(_layer, "Barra del mapa");
            top.gameObject.AddComponent<Image>().color = Bar;
            HorizontalLayoutGroup topRow = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topRow.padding = new RectOffset(32, 32, 28, 28);
            topRow.spacing = 24;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;
            Button back = UiFactory.MakeButton(top, string.Empty, UiFactory.Option, UiFactory.Ink, Back, UiFactory.MinFontSize, 0, 16);
            back.GetComponent<LayoutElement>().preferredWidth = 220;
            _backLabel = back.GetComponentInChildren<TextMeshProUGUI>();
            _title = UiFactory.Label(top, string.Empty, 46, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            RectTransform chip = UiFactory.Child(top, "Tardes libres");
            UiFactory.RoundedImage(chip.gameObject, UiFactory.Accent);
            HorizontalLayoutGroup chipRow = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipRow.padding = new RectOffset(24, 24, 12, 12);
            chipRow.childControlWidth = true;
            chipRow.childControlHeight = true;
            _freeTime = UiFactory.Label(chip, string.Empty, UiFactory.MinFontSize, UiFactory.AccentText, FontStyles.Bold);
            _freeTime.textWrappingMode = TextWrappingModes.NoWrap;
            // Sin esto el fondo redondeado pide su propio tamaño y el aviso sale como un cuadro gigante.
            _freeTimeSize = chip.gameObject.AddComponent<LayoutElement>();
            _freeTimeSize.layoutPriority = 1;
            _freeTimeSize.preferredHeight = 76;
            _freeTimeSize.flexibleWidth = 0;
            _freeTimeSize.flexibleHeight = 0;

            // El hueco: el mapa se ve aquí; tocar un lugar lo elige, arrastrar gira.
            _hole = UiFactory.Child(_layer, "Hueco del mapa");
            LayoutElement hole = _hole.gameObject.AddComponent<LayoutElement>();
            hole.flexibleHeight = 1;
            hole.minHeight = 420;
            OrbitPad pad = OrbitPad.AddTo(_hole, _home);
            pad.Tapped = Tapped;

            RectTransform hintRow = UiFactory.Child(_hole, "Ayuda");
            hintRow.anchorMin = new Vector2(0.5f, 1f);
            hintRow.anchorMax = new Vector2(0.5f, 1f);
            hintRow.pivot = new Vector2(0.5f, 1f);
            hintRow.anchoredPosition = new Vector2(0f, -24f);
            UiFactory.RoundedImage(hintRow.gameObject, HintBack).raycastTarget = false;
            HorizontalLayoutGroup hintLayout = hintRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hintLayout.padding = new RectOffset(28, 28, 12, 12);
            hintLayout.childControlWidth = true;
            hintLayout.childControlHeight = true;
            ContentSizeFitter hintFit = hintRow.gameObject.AddComponent<ContentSizeFitter>();
            hintFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hintFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _hint = UiFactory.Label(hintRow, string.Empty, UiFactory.MinFontSize, UiFactory.Ink, FontStyles.Bold);
            _hint.textWrappingMode = TextWrappingModes.NoWrap;

            // Los nombres de los lugares van encima del mapa y lo siguen cuando se gira.
            _tags = UiFactory.Child(_hole, "Nombres de los lugares");
            UiFactory.Stretch(_tags);
            // Los nombres solo se ven dentro del hueco: no se montan sobre la barra de arriba ni la hoja.
            _tags.gameObject.AddComponent<RectMask2D>();
            _follow = _layer.gameObject.AddComponent<PlaceTags>();
            _follow.Home = _home;

            // Abajo, la hoja de papel: tu gente, o lo que se hace en el lugar que tocaste.
            RectTransform sheet = UiFactory.Column(_layer, "Hoja del mapa", 0, 0, UiFactory.Paper);
            _sheetSize = sheet.gameObject.AddComponent<LayoutElement>();
            _sheetSize.preferredHeight = IdleSheet;
            _body = UiFactory.ScrollColumn(sheet, "Lo que tocaste", 40, 20);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = Color.clear;
        }

        /// <summary>Volver: de elegir con quién al lugar, de la salida o del lugar al mapa, y del mapa a la vida.</summary>
        private void Back()
        {
            Sounds.Play(Sound.WoodKnock);
            if (_choosing)
            {
                _choosing = false;
            }
            else if (_result != null)
            {
                Reset();
            }
            else if (_placeId != null)
            {
                Reset();
            }
            else
            {
                Close();
                return;
            }

            Draw();
        }

        private void Draw()
        {
            LifePresenter presenter = _presenter!;
            NeighborhoodView? map = presenter.Neighborhood();
            if (map == null)
            {
                Close();
                return;
            }

            _backLabel.text = presenter.Text("ui.boton.volver");
            _freeTime.text = map.FreeTime?.Label ?? string.Empty;
            _freeTimeSize.minWidth = _freeTime.GetPreferredValues(_freeTime.text).x + 48;
            _freeTimeSize.preferredWidth = _freeTimeSize.minWidth;
            _freeTime.transform.parent.gameObject.SetActive(map.FreeTime != null);
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;

            if (_result != null && _outing != null)
            {
                _home.View = SceneView.Outing;
                _home.ShowOuting(_outing);
                _title.text = _outing.Title;
                _hint.transform.parent.gameObject.SetActive(false);
                _follow.Follow(new List<(string Id, RectTransform Tag)>());
                UiFactory.Clear(_tags);
                _sheetSize.preferredHeight = IdleSheet + 200f;
                DrawResult(presenter);
                return;
            }

            _home.View = SceneView.Map;
            _home.ShowMap(map, _player);
            _title.text = map.Title;
            _hint.transform.parent.gameObject.SetActive(true);
            _hint.text = map.Hint;
            DrawTags(map);

            MapPlaceView? place = _placeId == null ? null : map.Places.FirstOrDefault(p => p.Id == _placeId);
            if (place == null)
            {
                _placeId = null;
                _sheetSize.preferredHeight = IdleSheet;
                DrawIdle(presenter, map);
            }
            else if (_choosing && _activityId != null)
            {
                _sheetSize.preferredHeight = ChooseSheet;
                DrawChooser(presenter);
            }
            else
            {
                _sheetSize.preferredHeight = PlaceSheet;
                DrawPlace(presenter, place);
            }
        }

        /// <summary>Un nombre sobre cada lugar; se toca igual que el lugar.</summary>
        private void DrawTags(NeighborhoodView map)
        {
            UiFactory.Clear(_tags);
            var follow = new List<(string Id, RectTransform Tag)>();
            int count = Mathf.Min(map.Places.Count, MapBuilder.Slots);
            for (int i = 0; i < count; i++)
            {
                MapPlaceView place = map.Places[i];
                string id = place.Id;
                bool chosen = _placeId == id;
                follow.Add((MapBuilder.PlacePrefix + id, Tag(place.Name, chosen ? UiFactory.Accent : TagBack, UiFactory.PaperInk, () => SelectPlace(id))));
            }

            _follow.Follow(follow);
        }

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

        private void Tapped(Vector2 screenPoint)
        {
            if (_result != null)
            {
                return;
            }

            string? id = _home.Pick(screenPoint);
            if (id != null && id.StartsWith(MapBuilder.PlacePrefix, System.StringComparison.Ordinal))
            {
                SelectPlace(id.Substring(MapBuilder.PlacePrefix.Length));
            }
        }

        private void SelectPlace(string placeId)
        {
            Sounds.Play(Sound.PhotoClick);
            Reset();
            _placeId = placeId;
            Draw();
        }

        private void SelectActivity(PlaceActivityView activity)
        {
            Sounds.Play(Sound.PencilTap);
            _activityId = activity.Id;
            _companion = null;
            _alone = !activity.WithSomeone;
            if (activity.WithSomeone)
            {
                CompanionsView with = _presenter!.Companions(activity.Id);
                if (with.Suggested.Count > 0)
                {
                    _companion = with.Suggested[0].Id;
                }
                else
                {
                    _alone = with.Alone != null;
                }
            }

            Draw();
        }

        /// <summary>Sin lugar elegido: tu gente más cercana (se toca para su ficha) y la lista completa.</summary>
        private void DrawIdle(LifePresenter presenter, NeighborhoodView map)
        {
            Kicker(presenter.Text("ui.barrio.tu_gente"));
            RectTransform row = Row(_body, "Tu gente cercana", 16);
            foreach (CompanionView person in map.Closest)
            {
                int id = person.Id;
                PersonCard(row, person, false, () =>
                {
                    Sounds.Play(Sound.PhotoClick);
                    OpenCard?.Invoke(id);
                }, showReason: false);
            }

            UiFactory.Label(_body, map.PeopleHint, 38, UiFactory.PaperMutedInk);
            AddButton(presenter.Text("ui.barrio.ver_gente"), () => OpenPeople?.Invoke());
        }

        /// <summary>
        /// Un lugar elegido: qué es, "1 · Qué haces" (cada cosa con lo que cuesta y lo que trae),
        /// "2 · Con quién" (los sugeridos con su motivo y "Elegir a otra persona") y el botón para ir.
        /// </summary>
        private void DrawPlace(LifePresenter presenter, MapPlaceView place)
        {
            Kicker(place.CountryNote?.ToUpperInvariant() ?? presenter.Text("ui.barrio.lugar"));
            UiFactory.Label(_body, place.Name, 54, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(_body, place.Description, 38, UiFactory.PaperMutedInk);

            switch (place.Opens)
            {
                case "casa":
                    AddButton(presenter.Text("ui.barrio.ir_casa"), Close);
                    break;
                case "estudios":
                case "trabajo":
                    string group = place.Opens;
                    string label = group == "estudios" ? presenter.Text("ui.barrio.abrir_estudios") : presenter.Text("ui.barrio.abrir_trabajo");
                    AddButton(label, () =>
                    {
                        Close();
                        OpenSection?.Invoke(group);
                    });
                    break;
            }

            IReadOnlyList<PlaceActivityView> activities = presenter.PlaceActivities(place.Id);
            if (activities.Count == 0)
            {
                if (place.Opens == null)
                {
                    UiFactory.Label(_body, presenter.Text("ui.barrio.nada"), 38, UiFactory.PaperMutedInk);
                }

                return;
            }

            Kicker(presenter.Text("ui.barrio.que_haces"));
            PlaceActivityView? chosen = null;
            foreach (PlaceActivityView activity in activities)
            {
                bool selected = activity.Id == _activityId;
                if (selected)
                {
                    chosen = activity;
                }

                ActivityRow(activity, selected);
            }

            if (chosen == null)
            {
                return;
            }

            if (chosen.WithSomeone)
            {
                CompanionsView with = presenter.Companions(chosen.Id);
                Kicker(presenter.Text(with.Suggested.Count > 0 ? "ui.barrio.con_quien_sugeridos" : "ui.barrio.con_quien"));
                RectTransform faces = Row(_body, "Sugeridos", 16);
                var shown = with.Suggested.ToList();
                if (_companion.HasValue && shown.All(c => c.Id != _companion.Value))
                {
                    // Alguien que elegiste en la lista: va primero, en grande, con los otros sugeridos.
                    CompanionView? picked = with.Tabs.SelectMany(t => t.People).FirstOrDefault(c => c.Id == _companion.Value);
                    if (picked != null)
                    {
                        shown.Insert(0, picked);
                        shown = shown.Take(LifePresenter.MaxSuggested).ToList();
                    }
                }

                foreach (CompanionView person in shown)
                {
                    int id = person.Id;
                    PersonCard(faces, person, !_alone && _companion == id, () =>
                    {
                        Sounds.Play(Sound.PhotoClick);
                        _companion = id;
                        _alone = false;
                        Draw();
                    }, showReason: true);
                }

                if (with.Alone != null)
                {
                    AloneCard(faces, with.Alone, _alone);
                }

                if (with.Total > shown.Count)
                {
                    UiFactory.MakeButton(_body, with.ChooseOther, UiFactory.Paper, UiFactory.PaperInk, () =>
                    {
                        Sounds.Play(Sound.PageTurn);
                        _choosing = true;
                        _query = string.Empty;
                        _tab = string.Empty;
                        Draw();
                    }, 38, 0, 24);
                }
            }

            GoButton(presenter, chosen);
        }

        /// <summary>Algo que se puede hacer: el nombre, por qué, lo que cuesta (en gris) y lo que trae (en verde). Se elige tocándolo.</summary>
        private void ActivityRow(PlaceActivityView activity, bool selected)
        {
            RectTransform row = UiFactory.Column(_body, activity.Title, 0, 10, selected ? Chosen : UiFactory.PaperOption);
            row.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(28, 28, 22, 24);
            CanvasGroup fade = row.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = activity.CanAfford ? 1f : 0.5f;
            UiFactory.Label(row, activity.Title, 42, UiFactory.PaperInk, FontStyles.Bold);
            if (activity.Why.Length > 0)
            {
                UiFactory.Label(row, activity.Why, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            }

            var chips = activity.Costs.Select(c => new ChangeView(c, false)).Concat(activity.Gives.Select(g => new ChangeView(g, true))).ToList();
            ActionsScreen.Chips(row, chips);
            if (activity.Blocked != null)
            {
                UiFactory.Label(row, activity.Blocked, UiFactory.MinFontSize, UiFactory.KeyInk, FontStyles.Bold);
            }

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row.GetComponent<Image>();
            button.interactable = activity.CanAfford;
            PlaceActivityView chosen = activity;
            button.onClick.AddListener(() => SelectActivity(chosen));
        }

        /// <summary>Una persona en grande: su cara, su nombre, cómo están y (si se pide) su motivo.</summary>
        private static void PersonCard(RectTransform parent, CompanionView person, bool selected, UnityEngine.Events.UnityAction onTap, bool showReason)
        {
            RectTransform card = UiFactory.Column(parent, person.Name, 0, 8, selected ? Chosen : UiFactory.PaperOption);
            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 22, 22);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Circle(card, Initials(person.Initials), FaceColor(person.Id));
            TextMeshProUGUI name = UiFactory.Label(card, person.Name.Split(' ')[0], 40, UiFactory.PaperInk, FontStyles.Bold);
            name.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI feeling = UiFactory.Label(card, Capitalized(person.Feeling), UiFactory.MinFontSize, LevelInk(person.Level), FontStyles.Bold);
            feeling.alignment = TextAlignmentOptions.Center;
            string detail = showReason && person.Reason != null ? person.Reason : person.Relation;
            TextMeshProUGUI why = UiFactory.Label(card, detail, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            why.alignment = TextAlignmentOptions.Center;
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(onTap);
        }

        private void AloneCard(RectTransform parent, string label, bool selected)
        {
            RectTransform card = UiFactory.Column(parent, label, 0, 8, selected ? Chosen : UiFactory.PaperOption);
            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 22, 22);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Circle(card, "–", UiFactory.PaperMutedInk);
            TextMeshProUGUI text = UiFactory.Label(card, label, 40, UiFactory.PaperInk, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PhotoClick);
                _alone = true;
                _companion = null;
                Draw();
            });
        }

        /// <summary>El botón grande para ir: "Ver una película con Tomás", o "Elige con quién" si falta.</summary>
        private void GoButton(LifePresenter presenter, PlaceActivityView activity)
        {
            string label;
            bool ready = activity.CanAfford;
            if (_alone || !activity.WithSomeone)
            {
                label = activity.WithSomeone ? presenter.Text("ui.barrio.ir_solo", activity.Title) : activity.Title;
            }
            else if (_companion.HasValue && Name(presenter, activity.Id, _companion.Value) is string name)
            {
                label = presenter.Text("ui.barrio.ir_con", activity.Title, name);
            }
            else
            {
                label = presenter.Text("ui.barrio.elige_a_alguien");
                ready = false;
            }

            Button go = UiFactory.MakeButton(_body, label, UiFactory.Accent, UiFactory.AccentText, Go, 44, 0, 28);
            go.interactable = ready;
        }

        private static string? Name(LifePresenter presenter, string actionId, int id) =>
            presenter.Companions(actionId).Tabs.SelectMany(t => t.People).FirstOrDefault(c => c.Id == id)?.Name.Split(' ')[0];

        private void Go()
        {
            LifePresenter presenter = _presenter!;
            if (_activityId == null)
            {
                return;
            }

            int? with = _alone ? (int?)null : _companion;
            _outing = presenter.Outing(_activityId, with);
            _result = presenter.DoAction(_activityId, with);
            Sounds.Play(Sound.PencilTap);
            Changed?.Invoke();
            _choosing = false;
            Draw();
        }

        /// <summary>Lo que pasó en la salida, con la escena del lugar arriba, y el botón para volver al mapa.</summary>
        private void DrawResult(LifePresenter presenter)
        {
            ActionResultView result = _result!;
            Kicker(result.DateLabel.ToUpperInvariant());
            UiFactory.Label(_body, result.Title, 50, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(_body, result.Text, 40, UiFactory.PaperInk);
            ActionsScreen.Chips(_body, result.Changes);
            AddButton(presenter.Text("ui.barrio.volver_mapa"), () =>
            {
                Reset();
                Draw();
            });
        }

        /// <summary>
        /// "Elegir a otra persona" (boceto BElegir): buscar por nombre, pestañas por grupo y filas
        /// grandes, primero los más cercanos. Al tocar a alguien, vuelve al lugar con esa persona elegida.
        /// </summary>
        private void DrawChooser(LifePresenter presenter)
        {
            CompanionsView with = presenter.Companions(_activityId!);
            Kicker(with.Title.ToUpperInvariant());
            Kicker(presenter.Text("ui.barrio.buscar"));
            RectTransform list = null!;
            Search(presenter.Text("ui.barrio.buscar_ayuda"), query =>
            {
                _query = query;
                Rows(presenter, with, list);
            });

            if (_tab.Length == 0 || with.Tabs.All(t => t.Key != _tab))
            {
                _tab = with.Tabs.Count > 0 ? with.Tabs[0].Key : string.Empty;
            }

            RectTransform tabs = Row(_body, "Pestañas", 12);
            foreach (CompanionTabView tab in with.Tabs)
            {
                string key = tab.Key;
                bool on = key == _tab;
                Button button = UiFactory.MakeButton(tabs, tab.Label + " " + tab.People.Count, on ? UiFactory.PaperInk : UiFactory.PaperOption, on ? UiFactory.Paper : UiFactory.PaperInk, () =>
                {
                    Sounds.Play(Sound.PencilTap);
                    _tab = key;
                    Draw();
                }, UiFactory.MinFontSize, 0, 16);
                button.GetComponent<LayoutElement>().flexibleWidth = 1;
            }

            Kicker(presenter.Text("ui.barrio.mas_cercanos"));
            list = UiFactory.Column(_body, "Gente", 0, 6);
            Rows(presenter, with, list);
        }

        /// <summary>Las filas de la pestaña (o de todas, si se busca un nombre).</summary>
        private void Rows(LifePresenter presenter, CompanionsView with, RectTransform list)
        {
            UiFactory.Clear(list);
            string query = _query.Trim().ToLowerInvariant();
            IEnumerable<CompanionView> people = query.Length > 0
                ? with.Tabs.SelectMany(t => t.People).Where(p => p.Name.ToLowerInvariant().Contains(query)).OrderByDescending(p => p.Closeness)
                : with.Tabs.Where(t => t.Key == _tab).SelectMany(t => t.People);
            bool any = false;
            foreach (CompanionView person in people)
            {
                any = true;
                int id = person.Id;
                RectTransform row = UiFactory.Child(list, person.Name);
                UiFactory.RoundedImage(row.gameObject, _companion == id ? Chosen : UiFactory.Paper);
                HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(20, 20, 18, 18);
                layout.spacing = 24;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                row.gameObject.AddComponent<LayoutElement>().minHeight = 150;
                Circle(row, Initials(person.Initials), FaceColor(person.Id));
                RectTransform text = UiFactory.Column(row, "Nombre", 0, 2);
                text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UiFactory.Label(text, person.Name, 42, UiFactory.PaperInk, FontStyles.Bold);
                UiFactory.Label(text, person.Relation + " · " + person.Feeling, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
                if (person.Reason != null)
                {
                    TextMeshProUGUI reason = UiFactory.Label(row, person.Reason, UiFactory.MinFontSize, LevelInk(LifeBarLevel.Good), FontStyles.Bold);
                    reason.alignment = TextAlignmentOptions.Right;
                    reason.gameObject.AddComponent<LayoutElement>().preferredWidth = 300;
                }

                Button button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row.GetComponent<Image>();
                button.onClick.AddListener(() =>
                {
                    Sounds.Play(Sound.PhotoClick);
                    _companion = id;
                    _alone = false;
                    _choosing = false;
                    Draw();
                });
            }

            if (!any)
            {
                UiFactory.Label(list, presenter.Text("ui.barrio.nadie"), 38, UiFactory.PaperMutedInk);
            }
        }

        /// <summary>El buscador: una caja grande para escribir un nombre.</summary>
        private void Search(string placeholderText, UnityEngine.Events.UnityAction<string> changed)
        {
            RectTransform box = UiFactory.Child(_body, "Buscar");
            Image back = UiFactory.RoundedImage(box.gameObject, UiFactory.Paper);
            LayoutElement size = box.gameObject.AddComponent<LayoutElement>();
            size.minHeight = UiFactory.MinButtonHeight;
            size.preferredHeight = UiFactory.MinButtonHeight;
            RectTransform area = UiFactory.Child(box, "Area del texto");
            UiFactory.Stretch(area);
            area.offsetMin = new Vector2(32f, 12f);
            area.offsetMax = new Vector2(-32f, -12f);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI placeholder = UiFactory.Label(area, placeholderText, 40, UiFactory.PaperMutedInk);
            UiFactory.Stretch(placeholder.rectTransform);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            TextMeshProUGUI text = UiFactory.Label(area, string.Empty, 40, UiFactory.PaperInk);
            UiFactory.Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_InputField input = box.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = back;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = text.font;
            input.pointSize = 40;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.text = _query;
            input.onValueChanged.AddListener(changed);
        }

        private void Kicker(string text)
        {
            TextMeshProUGUI kicker = UiFactory.Label(_body, text, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            kicker.characterSpacing = 2f;
        }

        private static RectTransform Row(Transform parent, string name, int spacing)
        {
            RectTransform row = UiFactory.Child(parent, name);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        /// <summary>Una cara redonda con sus iniciales.</summary>
        private static void Circle(Transform parent, string initials, Color color)
        {
            RectTransform circle = UiFactory.Child(parent, "Cara");
            UiFactory.RoundedImage(circle.gameObject, color);
            LayoutElement size = circle.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = size.minWidth = 104;
            size.preferredHeight = size.minHeight = 104;
            TextMeshProUGUI label = UiFactory.Label(circle, initials, 38, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
        }

        private static readonly Color[] Faces =
        {
            UiFactory.Hex(0x5B8DB8), UiFactory.Hex(0xB86B8F), UiFactory.Hex(0x6FA37E), UiFactory.Hex(0xC98F4E), UiFactory.Hex(0x8E7CC3),
        };

        private static Color FaceColor(int id) => Faces[Mathf.Abs(id) % Faces.Length];

        private static string Initials(string initials) => initials.ToUpperInvariant();

        private static Color LevelInk(LifeBarLevel level) => level switch
        {
            LifeBarLevel.Good => UiFactory.Hex(0x2F7A55),
            LifeBarLevel.Middle => UiFactory.Hex(0x8A6A12),
            _ => UiFactory.KeyInk,
        };

        private void AddButton(string text, UnityEngine.Events.UnityAction onTap)
        {
            UiFactory.MakeButton(_body, text, UiFactory.PaperOption, UiFactory.PaperInk, () =>
            {
                Sounds.Play(Sound.PencilTap);
                onTap();
            }, 38, 0, 24);
        }

        private static string Capitalized(string text) =>
            text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
