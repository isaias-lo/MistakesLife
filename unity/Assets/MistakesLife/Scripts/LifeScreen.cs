#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La pantalla de la vida (docs/diseno-app.md, "Pantalla de la vida"): la casa en 3D en toda
    /// la pantalla; arriba el retrato, el nombre, la edad y la fecha; a la derecha Casa, Barrio y
    /// Trabajo; abajo una hoja con la última línea del diario, el estado en palabras, tus cosas y
    /// "Vivir la semana". Cuando pasa algo, la hoja muestra el evento en papel. La familia, tu
    /// gente y el diario completo se abren aparte.
    /// Se crea sola al apretar Play, en cualquier escena: no hay que configurar nada.
    /// Regla: aquí no se decide nada. Se dibuja la foto de LifePresenter (núcleo) y se le pasan los toques.
    /// </summary>
    public sealed class LifeScreen : MonoBehaviour
    {
        /// <summary>Cuántas líneas del diario se muestran (las más nuevas primero).</summary>
        private const int JournalLines = 60;

        /// <summary>Cuántas líneas nuevas del diario se destacan en "Lo que pasó".</summary>
        private const int NewsLines = 2;

        /// <summary>Alto máximo de lo que va en la hoja de abajo; si es más largo, tiene scroll.</summary>
        private const float MaxSheetBody = 860f;

        /// <summary>Cuánto se estiran los fondos de la barra y la hoja fuera de la pantalla.</summary>
        private const float Beyond = 400f;

        /// <summary>Ancho de los botones Casa, Barrio y Trabajo (angostos, para tapar poco la pieza).</summary>
        private const float ViewSwitchWidth = 180f;

        private readonly List<JournalLine> _journal = new List<JournalLine>();

        /// <summary>Las líneas del diario que trajo el último paso (las más nuevas primero).</summary>
        private readonly List<JournalLine> _lastNews = new List<JournalLine>();

        /// <summary>Si la parte de abajo muestra la lista de momentos para volver al pasado.</summary>
        private bool _pickingMoment;
        private ContentDatabase? _content;
        private LifePresenter? _presenter;
        private StartMenu? _menu;
        private Tutorial? _tutorial;
        private RectTransform _safe = null!;
        private RectTransform _page = null!;
        private float _sheetWidth;

        /// <summary>Lo que pasó en el último toque (tiempo y cambios), o null si no hay nada que contar.</summary>
        private StepSummary? _lastStep;

        private TextMeshProUGUI _name = null!;
        private TextMeshProUGUI _details = null!;
        private TextMeshProUGUI _initial = null!;
        private RectTransform _summary = null!;
        private RectTransform _sheetBody = null!;
        private LayoutElement _sheetSize = null!;
        private RectTransform _footer = null!;
        private RectTransform _history = null!;
        private RectTransform _historyBody = null!;
        private TextMeshProUGUI _historyTitle = null!;
        private Button _closeHistory = null!;
        private HomeScene _home = null!;
        private Button _roomButton = null!;
        private Button _blockButton = null!;
        private Button _workButton = null!;
        private RectTransform _viewButtons = null!;
        private string _placeCaption = string.Empty;
        private string? _workCaption;
        private TextMeshProUGUI _placeLabel = null!;
        private Button _menuButton = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnPlay()
        {
            if (FindAnyObjectByType<LifeScreen>() == null)
            {
                new GameObject(nameof(LifeScreen)).AddComponent<LifeScreen>();
            }
        }

        private void Start()
        {
            Build();
            try
            {
                _content = CoreLoader.LoadContent();
                _menu = new StartMenu(_safe, _content)
                {
                    LifeChosen = StartLife,
                    ContinueChosen = Refresh,
                    HelpChosen = () => _tutorial!.Show(),
                    CurrentLife = () => _presenter?.Snapshot(),
                };
                _tutorial = new Tutorial(_safe, _content);
                ContinueSavedLife();
                ShowMenu();
            }
            catch (System.Exception ex)
            {
                // Sin contenido no hay juego: se muestra el error para poder copiarlo.
                _name.text = "Error";
                _details.text = ex.Message;
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Solo para las capturas automáticas (SceneCaptures): arma la pantalla con una vida
        /// ya avanzada, sin esperar al Play, y con el menú de inicio encima si se pide.
        /// </summary>
        public void ShowForCapture(ContentDatabase content, LifePresenter life, bool withMenu)
        {
            Build();
            _content = content;
            _menu = new StartMenu(_safe, content)
            {
                LifeChosen = StartLife,
                ContinueChosen = Refresh,
                CurrentLife = () => _presenter?.Snapshot(),
            };
            _tutorial = new Tutorial(_safe, content);
            _presenter = life;
            ReloadJournal();
            Refresh();
            if (withMenu)
            {
                _menu.ShowMenu();
            }
        }

        /// <summary>El menú de inicio, encima de la vida en curso (si hay una, se puede continuar).</summary>
        private void ShowMenu()
        {
            Save();
            _menu!.ShowMenu();
        }

        /// <summary>Empieza la vida que el jugador eligió entre los nacimientos posibles.</summary>
        private void StartLife(LifePresenter life)
        {
            _presenter = life;
            _journal.Clear();
            _pickingMoment = false;
            _lastStep = null;
            _presenter.TakeNewJournal();
            Refresh();
            Save();
            _tutorial!.ShowFirstTime();
        }

        /// <summary>
        /// Al abrir el juego se carga la vida guardada, si hay, para ofrecer "Continuar" en el menú.
        /// Si el guardado está dañado se aparta (el error queda en la consola) y el menú solo ofrece una vida nueva.
        /// </summary>
        private bool ContinueSavedLife()
        {
            byte[]? data = SaveStore.Read();
            if (data == null)
            {
                return false;
            }

            try
            {
                _presenter = new LifePresenter(LifeSession.LoadFromBytes(_content!, data));
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                SaveStore.SetAsideBroken();
                return false;
            }

            ReloadJournal();
            Refresh();
            return true;
        }

        /// <summary>El diario se vuelve a leer completo (al abrir el juego o al volver al pasado); lo nuevo empieza desde aquí.</summary>
        private void ReloadJournal()
        {
            _journal.Clear();
            IReadOnlyList<JournalLine> past = _presenter!.FullJournal();
            for (int i = past.Count - 1; i >= 0 && _journal.Count < JournalLines; i--)
            {
                _journal.Add(past[i]);
            }

            _presenter.TakeNewJournal();
        }

        /// <summary>Guarda la partida después de cada paso (la app se puede cerrar en cualquier momento).</summary>
        private void Save()
        {
            if (_presenter == null)
            {
                return;
            }

            try
            {
                SaveStore.Write(_presenter.Session.SaveToBytes());
            }
            catch (System.Exception ex)
            {
                // Sin espacio o sin permiso: se sigue jugando y se intenta de nuevo en el próximo paso.
                Debug.LogException(ex);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        /// <summary>Arma la estructura fija de la pantalla una sola vez.</summary>
        private void Build()
        {
            RectTransform canvas = UiFactory.CreateCanvas("Pantalla de la vida");

            // La casa en 3D ocupa toda la pantalla, detrás de todo.
            RectTransform window = UiFactory.Child(canvas, "Casa");
            UiFactory.Stretch(window);
            _home = HomeScene.Create();
            _home.Viewport = window;

            RectTransform safe = UiFactory.Child(canvas, "Zona segura");
            _safe = safe;
            UiFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            // La barra y la hoja llegan a los bordes: así no queda un marco de cielo alrededor.
            RectTransform page = UiFactory.Column(safe, "Pagina", 0, 0);
            UiFactory.Stretch(page);
            _page = page;

            BuildTopBar(page);

            // El hueco libre: aquí se centra la casa y van los botones para cambiar de lugar.
            RectTransform focus = UiFactory.Child(page, "Hueco");
            LayoutElement room = focus.gameObject.AddComponent<LayoutElement>();
            room.flexibleHeight = 1;
            room.minHeight = 380;
            _home.Focus = focus;
            BuildViewSwitch(focus);

            // La hoja de abajo, oscura para leer encima del 3D.
            RectTransform sheet = UiFactory.Column(page, "Hoja", 28, 16);
            Backdrop(sheet, UiFactory.Background, 0f, Beyond);
            // La hoja mide lo que tiene: todo el alto que sobra es para el 3D.
            sheet.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0;
            _summary = UiFactory.Column(sheet, "Lo que paso", 0, 6);
            _sheetBody = UiFactory.ScrollColumn(sheet, "Cuerpo", 0, 16);
            RectTransform scroll = (RectTransform)_sheetBody.parent!.parent!;
            scroll.GetComponent<Image>().color = Color.clear;
            _sheetSize = scroll.GetComponent<LayoutElement>();
            _sheetSize.flexibleHeight = 0;
            _footer = UiFactory.Column(sheet, "Acciones", 0, 12);

            BuildHistory(safe);
        }

        /// <summary>Arriba: retrato con anillo amarillo, nombre y edad, la fecha y el lugar; el botón Menú a la derecha.</summary>
        private void BuildTopBar(RectTransform page)
        {
            RectTransform top = UiFactory.Child(page, "Arriba");
            Backdrop(top, UiFactory.Card, Beyond, 0f);
            HorizontalLayoutGroup row = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(24, 24, 20, 20);
            row.spacing = 20;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            // Retrato: anillo amarillo (lo tuyo) con la inicial; el juguete de madera llega en U18c.
            RectTransform ring = UiFactory.Child(top, "Retrato");
            UiFactory.RoundedImage(ring.gameObject, UiFactory.Accent);
            LayoutElement size = ring.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 124;
            size.minHeight = size.preferredHeight = 124;
            RectTransform face = UiFactory.Child(ring, "Cara");
            UiFactory.Stretch(face);
            face.offsetMin = new Vector2(8f, 8f);
            face.offsetMax = new Vector2(-8f, -8f);
            UiFactory.RoundedImage(face.gameObject, UiFactory.Card);
            _initial = UiFactory.Label(face, string.Empty, 56, UiFactory.Ink, FontStyles.Normal, Face.Title);
            UiFactory.Stretch(_initial.rectTransform);
            _initial.alignment = TextAlignmentOptions.Center;

            RectTransform who = UiFactory.Column(top, "Quien", 0, 4);
            who.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            _name = UiFactory.Label(who, string.Empty, 46, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _details = UiFactory.Label(who, string.Empty, UiFactory.MinFontSize, UiFactory.MutedInk);

            _menuButton = UiFactory.MakeButton(top, string.Empty, UiFactory.Option, UiFactory.Ink, ShowMenu, UiFactory.MinFontSize, 0, 12);
            LayoutElement menu = _menuButton.GetComponent<LayoutElement>();
            menu.minWidth = menu.preferredWidth = 170;
        }

        /// <summary>
        /// En el hueco: botones para ver la casa, el barrio o el trabajo (a la derecha, uno bajo
        /// el otro) y una etiqueta abajo que dice adónde va el personaje esta semana.
        /// </summary>
        private void BuildViewSwitch(RectTransform window)
        {
            RectTransform buttons = UiFactory.Child(window, "Vista");
            _viewButtons = buttons;
            buttons.anchorMin = new Vector2(1f, 1f);
            buttons.anchorMax = new Vector2(1f, 1f);
            buttons.pivot = new Vector2(1f, 1f);
            buttons.anchoredPosition = new Vector2(-16f, -16f);
            VerticalLayoutGroup column = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 8;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = true;
            _roomButton = UiFactory.MakeButton(buttons, string.Empty, UiFactory.Option, UiFactory.Ink, () => SetView(SceneView.Room), UiFactory.MinFontSize, 0, 12);
            _blockButton = UiFactory.MakeButton(buttons, string.Empty, UiFactory.Option, UiFactory.Ink, () => SetView(SceneView.Neighborhood), UiFactory.MinFontSize, 0, 12);
            _workButton = UiFactory.MakeButton(buttons, string.Empty, UiFactory.Option, UiFactory.Ink, () => SetView(SceneView.Work), UiFactory.MinFontSize, 0, 12);
            SizeViewSwitch(3);

            RectTransform place = UiFactory.Child(window, "Lugar");
            // Arriba a la izquierda, al lado de los botones, para no tapar el piso de la pieza.
            // Tiene espacio para dos líneas: empresa y cargo juntos son largos.
            place.anchorMin = new Vector2(0f, 1f);
            place.anchorMax = new Vector2(1f, 1f);
            place.pivot = new Vector2(0f, 1f);
            place.offsetMin = new Vector2(16f, -116f);
            place.offsetMax = new Vector2(-(ViewSwitchWidth + 32f), -16f);
            UiFactory.RoundedImage(place.gameObject, Translucent(UiFactory.Background, 0.85f));
            _placeLabel = UiFactory.Label(place, string.Empty, UiFactory.MinFontSize, UiFactory.Ink, FontStyles.Bold);
            UiFactory.Stretch(_placeLabel.rectTransform);
            _placeLabel.alignment = TextAlignmentOptions.Center;
            _placeLabel.margin = new Vector4(12f, 4f, 12f, 4f);
            // Si no cabe en dos líneas, termina en "..." en vez de achicar la letra bajo el mínimo.
            _placeLabel.overflowMode = TextOverflowModes.Ellipsis;
            PaintViewSwitch();
        }

        private void SizeViewSwitch(int count)
        {
            _viewButtons.sizeDelta = new Vector2(ViewSwitchWidth, (count * UiFactory.MinButtonHeight) + ((count - 1) * 8f));
        }

        /// <summary>Tu gente y el diario completo: una hoja aparte, encima de todo, que se cierra con un botón.</summary>
        private void BuildHistory(RectTransform safe)
        {
            _history = UiFactory.Column(safe, "Tu gente y diario", 28, 16, UiFactory.Background);
            UiFactory.Stretch(_history);
            RectTransform header = UiFactory.Child(_history, "Titulo");
            HorizontalLayoutGroup row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            _historyTitle = UiFactory.Label(header, string.Empty, 52, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _historyTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            _closeHistory = UiFactory.MakeButton(header, string.Empty, UiFactory.Option, UiFactory.Ink, () => ShowHistory(false), UiFactory.MinFontSize, 0, 12);
            LayoutElement close = _closeHistory.GetComponent<LayoutElement>();
            close.minWidth = close.preferredWidth = 200;
            _historyBody = UiFactory.ScrollColumn(_history, "Lista", 28, 14);
            _history.gameObject.SetActive(false);
        }

        private void ShowHistory(bool show)
        {
            _history.gameObject.SetActive(show);
            if (show && _presenter != null)
            {
                DrawHistory(_presenter, _presenter.Snapshot());
            }
        }

        /// <summary>
        /// Fondo liso de la barra o de la hoja, que se estira hacia afuera de la pantalla
        /// (arriba o abajo) para tapar también las zonas del notch y de la barra del teléfono.
        /// </summary>
        private static void Backdrop(RectTransform target, Color color, float up, float down)
        {
            RectTransform back = UiFactory.Child(target, "Fondo");
            back.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            back.gameObject.AddComponent<Image>().color = color;
            back.anchorMin = Vector2.zero;
            back.anchorMax = Vector2.one;
            back.offsetMin = new Vector2(-Beyond, -down);
            back.offsetMax = new Vector2(Beyond, up);
            back.SetAsFirstSibling();
        }

        private static Color Translucent(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void SetView(SceneView view)
        {
            _home.View = view;
            PaintViewSwitch();
            ShowCaption();
        }

        /// <summary>En la vista Trabajo se muestra el cargo; en las otras, adónde va esta semana.</summary>
        private void ShowCaption()
        {
            _placeLabel.text = _home.View == SceneView.Work && _workCaption != null ? _workCaption : _placeCaption;
        }

        /// <summary>El botón de la vista activa va con el color de acento.</summary>
        private void PaintViewSwitch()
        {
            Paint(_roomButton, _home.View == SceneView.Room);
            Paint(_blockButton, _home.View == SceneView.Neighborhood);
            Paint(_workButton, _home.View == SceneView.Work);
        }

        private static void Paint(Button button, bool active)
        {
            ((Image)button.targetGraphic).color = active ? UiFactory.Accent : UiFactory.Option;
            button.GetComponentInChildren<TextMeshProUGUI>().color = active ? UiFactory.AccentText : UiFactory.Ink;
        }

        /// <summary>Vuelve a dibujar todo con la foto actual de la partida.</summary>
        private void Refresh()
        {
            LifePresenter presenter = _presenter!;
            IReadOnlyList<JournalLine> fresh = presenter.TakeNewJournal();
            foreach (JournalLine line in fresh)
            {
                _journal.Insert(0, line);
            }

            // Lo que se anotó en el diario en este paso se muestra arriba de la hoja, en "Lo que pasó".
            _lastNews.Clear();
            for (int i = fresh.Count - 1; i >= 0 && _lastNews.Count < NewsLines; i--)
            {
                _lastNews.Add(fresh[i]);
            }

            if (_journal.Count > JournalLines)
            {
                _journal.RemoveRange(JournalLines, _journal.Count - JournalLines);
            }

            LifeSnapshot life = presenter.Snapshot();
            _name.text = $"{life.PlayerName}, {life.AgeLabel}";
            // La fecha y el lugar en dos líneas, para que el lugar no se corte a la mitad.
            _details.text = $"{life.DateLabel}\n{life.Place}";
            _initial.text = life.PlayerName.Length > 0 ? life.PlayerName.Substring(0, 1) : string.Empty;
            _menuButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.boton.menu");

            _home.Show(life.Home, life.DailyPlace, life.Workplace);
            _home.SetMood(life.Happiness);
            _roomButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.vista.casa");
            _blockButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.vista.barrio");
            _workButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.vista.trabajo");

            // El botón Trabajo solo aparece si trabaja; si pierde el trabajo, se vuelve a la casa.
            _workButton.gameObject.SetActive(life.Workplace != null);
            SizeViewSwitch(life.Workplace != null ? 3 : 2);
            if (life.Workplace == null && _home.View == SceneView.Work)
            {
                SetView(SceneView.Room);
            }

            _placeCaption = life.DailyPlaceLabel;
            _workCaption = life.Workplace?.Caption;
            ShowCaption();
            DrawLastStep(presenter);
            DrawSheet(presenter, life);
            DrawActions(presenter, life);
            FitSheet();
            if (_history.gameObject.activeSelf)
            {
                DrawHistory(presenter, life);
            }
        }

        /// <summary>La hoja de abajo crece con lo que tiene, hasta un máximo; si es más, tiene scroll.</summary>
        private void FitSheet()
        {
            // Primero se ordena la página entera, para que los textos sepan su ancho
            // (sin ancho, cada palabra iría en su línea y la hoja saldría altísima).
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_page);
            float height = LayoutUtility.GetPreferredHeight(_sheetBody);
            _sheetSize.preferredHeight = Mathf.Min(height, MaxSheetBody);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_page);
            _sheetBody.anchoredPosition = Vector2.zero;
            _sheetWidth = _sheetBody.rect.width;
        }

        /// <summary>Si cambia el ancho de la pantalla (girar, primer cuadro), la hoja se vuelve a medir.</summary>
        private void LateUpdate()
        {
            if (_presenter != null && _sheetBody != null && Mathf.Abs(_sheetBody.rect.width - _sheetWidth) > 0.5f)
            {
                FitSheet();
            }
        }

        /// <summary>Lo que va en la hoja: la semana (diario, estado y tus cosas), el evento, el legado o el final.</summary>
        private void DrawSheet(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_sheetBody);
            if (_pickingMoment)
            {
                DrawMoments(presenter);
                return;
            }

            switch (life.Phase)
            {
                case LifePhase.Choosing:
                    DrawEventCard(presenter, life);
                    break;
                case LifePhase.ChoosingHeir:
                    UiFactory.Label(_sheetBody, presenter.Text("ui.legado"), 42, UiFactory.Ink, FontStyles.Normal, Face.Title);
                    for (int i = 0; i < life.Heirs.Count; i++)
                    {
                        int heir = i;
                        string text = presenter.Text("ui.boton.seguir_con", life.Heirs[i].Name, life.Heirs[i].AgeInYears);
                        UiFactory.MakeButton(_sheetBody, text, UiFactory.Option, UiFactory.Ink, () => ContinueWith(heir));
                    }

                    break;
                case LifePhase.Over:
                    UiFactory.Label(_sheetBody, presenter.Text("ui.fin"), 42, UiFactory.Ink, FontStyles.Normal, Face.Title);
                    break;
                default:
                    DrawWeek(presenter, life);
                    break;
            }
        }

        /// <summary>
        /// Una semana normal: la última línea del diario con la letra a mano, el estado en palabras
        /// (sin números) y "tus cosas": el dinero, tu gente y el diario, y volver al pasado si se puede.
        /// </summary>
        private void DrawWeek(LifePresenter presenter, LifeSnapshot life)
        {
            if (_journal.Count > 0)
            {
                UiFactory.Label(_sheetBody, _journal[0].Text, 42, UiFactory.Ink, FontStyles.Normal, Face.Hand);
            }

            RectTransform states = Row(_sheetBody, "Estado");
            State(states, presenter.Text("ui.salud"), presenter.StateWord("salud", life.Health));
            State(states, presenter.Text("ui.animo"), presenter.StateWord("animo", life.Happiness));
            State(states, presenter.Text("ui.mente"), presenter.StateWord("mente", life.Smarts));

            RectTransform things = Row(_sheetBody, "Tus cosas");
            RectTransform money = UiFactory.Column(things, "Dinero", 0, 2);
            UiFactory.Label(money, presenter.Text("ui.dinero_titulo"), UiFactory.MinFontSize, UiFactory.MutedInk);
            UiFactory.Label(money, life.MoneyLabel, UiFactory.MinFontSize, UiFactory.Ink, FontStyles.Bold, Face.Mono);
            UiFactory.MakeButton(things, presenter.Text("ui.boton.tu_gente_diario"), UiFactory.Option, UiFactory.Ink, () => ShowHistory(true), UiFactory.MinFontSize, 0, 16);
            if (presenter.TimeTravel().CanGoBack)
            {
                UiFactory.MakeButton(things, presenter.Text("ui.boton.volver_pasado"), UiFactory.Option, UiFactory.Ink, () => PickMoment(true), UiFactory.MinFontSize, 0, 16);
            }
            else
            {
                // Un hueco vacío, para que las tres columnas queden alineadas con las del estado.
                UiFactory.Child(things, "Vacio");
            }

            Even(states);
            Even(things);
        }

        /// <summary>Todas las cosas de la fila con el mismo ancho, como columnas.</summary>
        private static void Even(RectTransform row)
        {
            for (int i = 0; i < row.childCount; i++)
            {
                GameObject item = row.GetChild(i).gameObject;
                LayoutElement element = item.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = item.AddComponent<LayoutElement>();
                }
                element.minWidth = 0;
                element.preferredWidth = 0;
                element.flexibleWidth = 1;
            }
        }

        /// <summary>Un estado: su nombre chico y, debajo, la palabra.</summary>
        private static void State(RectTransform row, string name, string word)
        {
            RectTransform box = UiFactory.Column(row, name, 0, 2);
            UiFactory.Label(box, name, UiFactory.MinFontSize, UiFactory.MutedInk);
            UiFactory.Label(box, word, 38, UiFactory.Ink, FontStyles.Bold);
        }

        /// <summary>Una fila de cosas del mismo ancho.</summary>
        private static RectTransform Row(RectTransform parent, string name)
        {
            RectTransform row = UiFactory.Child(parent, name);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        /// <summary>La familia, tu gente y el diario completo, en la hoja aparte.</summary>
        private void DrawHistory(LifePresenter presenter, LifeSnapshot life)
        {
            _historyTitle.text = presenter.Text("ui.boton.tu_gente_diario");
            _closeHistory.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.boton.cerrar");
            UiFactory.Clear(_historyBody);
            _historyBody.anchoredPosition = Vector2.zero;
            UiFactory.Label(_historyBody, $"{presenter.Text("ui.generacion", life.Generation)} · {presenter.Text("ui.partida", life.Seed)}", UiFactory.MinFontSize, UiFactory.MutedInk);

            if (life.Family.Count > 0)
            {
                UiFactory.Label(_historyBody, presenter.Text("ui.familia"), 40, UiFactory.Ink, FontStyles.Normal, Face.Title);
                foreach (PersonView person in life.Family)
                {
                    DrawPerson(presenter, person);
                }
            }

            if (life.People.Count > 0)
            {
                UiFactory.Label(_historyBody, presenter.Text("ui.gente"), 40, UiFactory.Ink, FontStyles.Normal, Face.Title);
                foreach (PersonView person in life.People)
                {
                    DrawPerson(presenter, person);
                }
            }

            UiFactory.Label(_historyBody, presenter.Text("ui.diario"), 40, UiFactory.Ink, FontStyles.Normal, Face.Title);
            foreach (JournalLine line in _journal)
            {
                UiFactory.Label(_historyBody, $"{line.AgeLabel}: {line.Text}", 34, UiFactory.Ink);
            }
        }

        /// <summary>
        /// "Lo que pasó" en el último toque, arriba de la hoja: cuánto tiempo corrió, lo que quedó
        /// en el diario y los cambios, en verde lo bueno y en rojo lo malo.
        /// </summary>
        private void DrawLastStep(LifePresenter presenter)
        {
            UiFactory.Clear(_summary);
            if (_lastStep == null && _lastNews.Count == 0)
            {
                return;
            }

            string title = presenter.Text("ui.paso.titulo");
            if (_lastStep != null && _lastStep.ElapsedLabel.Length > 0)
            {
                title += " · " + _lastStep.ElapsedLabel;
            }

            UiFactory.Label(_summary, title, UiFactory.MinFontSize, UiFactory.MutedInk, FontStyles.Bold);
            if (_lastStep != null && _lastStep.Changes.Count > 0)
            {
                var parts = new List<string>();
                foreach (ChangeView change in _lastStep.Changes)
                {
                    string color = ColorUtility.ToHtmlStringRGB(change.IsGood ? UiFactory.Good : UiFactory.Bad);
                    parts.Add($"<color=#{color}>{change.Label}</color>");
                }

                TextMeshProUGUI changes = UiFactory.Label(_summary, string.Join("     ", parts), UiFactory.MinFontSize, UiFactory.Ink, FontStyles.Bold);
                changes.richText = true;
            }
        }

        /// <summary>
        /// El evento como tarjeta: la edad arriba, "Momento clave" si lo es (con un color
        /// cálido), el texto grande y cada opción como un botón ancho.
        /// </summary>
        private void DrawEventCard(LifePresenter presenter, LifeSnapshot life)
        {
            EventView pending = life.PendingEvent!;
            // El evento es una hoja de papel (docs/diseno-app.md, "Un momento").
            RectTransform card = UiFactory.Column(_sheetBody, "Evento", 36, 18, UiFactory.Paper);

            string tag = pending.IsKeyMoment ? $"{life.AgeLabel} · {presenter.Text("ui.momento_clave")}" : life.AgeLabel;
            UiFactory.Label(card, pending.IsKeyMoment ? tag.ToUpperInvariant() : tag, 34, pending.IsKeyMoment ? UiFactory.KeyInk : UiFactory.PaperMutedInk, FontStyles.Bold);
            UiFactory.Label(card, pending.Text, 44, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            for (int i = 0; i < pending.Options.Count; i++)
            {
                int option = i;

                // Después de volver al pasado, la opción de la otra vez se marca como intuición.
                bool intuition = pending.IntuitionOption == i;
                string text = intuition ? presenter.Text("ui.intuicion", pending.Options[i]) : pending.Options[i];
                Button button = UiFactory.MakeButton(card, text, intuition ? UiFactory.Accent : UiFactory.PaperOption, intuition ? UiFactory.AccentText : UiFactory.PaperInk, () => Choose(option));
                button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            }
        }

        /// <summary>Una persona en una línea: relación, nombre, edad y, si está viva, cómo está la relación.</summary>
        private void DrawPerson(LifePresenter presenter, PersonView person)
        {
            string age = person.IsAlive ? person.AgeInYears.ToString() : presenter.Text("ui.fallecido");
            string feeling = person.Feeling.Length > 0 ? $", {person.Feeling}" : string.Empty;
            UiFactory.Label(_historyBody, $"{person.Relation}: {person.Name} ({age}{feeling})", 34, UiFactory.Ink);
        }

        /// <summary>Volver al pasado: las cargas que quedan y un botón por cada momento al que se puede volver.</summary>
        private void DrawMoments(LifePresenter presenter)
        {
            TimeTravelView travel = presenter.TimeTravel();
            UiFactory.Label(_sheetBody, presenter.Text("ui.pasado.titulo"), 42, UiFactory.Ink, FontStyles.Normal, Face.Title);
            UiFactory.Label(_sheetBody, travel.ChargesLabel, 34, UiFactory.MutedInk);
            for (int i = 0; i < travel.Moments.Count; i++)
            {
                int moment = i;
                UiFactory.MakeButton(_sheetBody, travel.Moments[i].Label, UiFactory.Option, UiFactory.Ink, () => GoBack(moment));
            }

            UiFactory.MakeButton(_sheetBody, presenter.Text("ui.boton.cancelar"), UiFactory.Card, UiFactory.MutedInk, () => PickMoment(false));
        }

        /// <summary>
        /// Botones de abajo: "Vivir la semana" (amarillo) y "Seguir viviendo" (hasta que pase algo),
        /// o empezar otra vida al final. "Volver al pasado" va en tus cosas durante la semana y
        /// aquí cuando hay un evento o la vida terminó.
        /// </summary>
        private void DrawActions(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_footer);
            if (_pickingMoment)
            {
                return;
            }

            if (life.Phase != LifePhase.Living && presenter.TimeTravel().CanGoBack)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.boton.volver_pasado"), UiFactory.Option, UiFactory.Ink, () => PickMoment(true));
            }

            if (life.Phase == LifePhase.Living)
            {
                // Dos ritmos: una semana a la vez, o correr hasta que pase algo (como mucho un año).
                RectTransform row = Row(_footer, "Avanzar");
                HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                Button week = UiFactory.MakeButton(row, presenter.Text("ui.boton.una_semana"), UiFactory.Accent, UiFactory.AccentText, () => Advance(false));
                week.GetComponent<LayoutElement>().flexibleWidth = 3;
                Button jump = UiFactory.MakeButton(row, presenter.Text("ui.boton.seguir_viviendo"), UiFactory.Option, UiFactory.Ink, () => Advance(true));
                jump.GetComponent<LayoutElement>().flexibleWidth = 2;
            }
            else if (life.Phase == LifePhase.Over)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.boton.nueva_vida"), UiFactory.Accent, UiFactory.AccentText, () => _menu!.ShowBirths());
            }
        }

        /// <summary>Avanza una semana o, con <paramref name="untilSomething"/>, hasta que pase algo (máximo un año).</summary>
        private void Advance(bool untilSomething)
        {
            LifeSnapshot before = _presenter!.Snapshot();
            int weeks = untilSomething ? _presenter.AdvanceUntilSomething() : _presenter.AdvanceWeek();
            RememberStep(before, weeks);
            Refresh();
            Save();
        }

        private void Choose(int option)
        {
            LifeSnapshot before = _presenter!.Snapshot();
            _presenter.Choose(option);
            RememberStep(before, 0);
            Refresh();
            Save();
        }

        /// <summary>Guarda el resumen del paso (lo calcula el núcleo) para mostrarlo arriba de la historia.</summary>
        private void RememberStep(LifeSnapshot before, int weeks)
        {
            StepSummary summary = _presenter!.Summarize(before, _presenter.Snapshot(), weeks);
            _lastStep = summary.IsEmpty ? null : summary;
        }

        private void PickMoment(bool picking)
        {
            _pickingMoment = picking;
            Refresh();
        }

        private void GoBack(int moment)
        {
            _presenter!.GoBack(moment);
            _pickingMoment = false;
            _lastStep = null;
            ReloadJournal();
            Refresh();
            Save();
        }

        private void ContinueWith(int heir)
        {
            _presenter!.ContinueWith(heir);
            _lastStep = null;
            Refresh();
            Save();
        }
    }
}
