#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La pantalla de la vida: tarjeta del personaje, la casa en 3D, barras de salud, felicidad e inteligencia,
    /// el evento con sus opciones como botones, la familia y el diario.
    /// Se crea sola al apretar Play, en cualquier escena: no hay que configurar nada.
    /// Regla: aquí no se decide nada. Se dibuja la foto de LifePresenter (núcleo) y se le pasan los toques.
    /// </summary>
    public sealed class LifeScreen : MonoBehaviour
    {
        /// <summary>Cuántas líneas del diario se muestran (las más nuevas primero).</summary>
        private const int JournalLines = 60;

        private readonly List<JournalLine> _journal = new List<JournalLine>();
        private ContentDatabase? _content;
        private LifePresenter? _presenter;

        private Text _name = null!;
        private Text _details = null!;
        private Text _healthLabel = null!;
        private Text _happinessLabel = null!;
        private Text _smartsLabel = null!;
        private RectTransform _health = null!;
        private RectTransform _happiness = null!;
        private RectTransform _smarts = null!;
        private Text _money = null!;
        private RectTransform _story = null!;
        private RectTransform _footer = null!;
        private HomeScene _home = null!;
        private Button _roomButton = null!;
        private Button _blockButton = null!;
        private Button _workButton = null!;
        private RectTransform _viewButtons = null!;
        private Text _placeLabel = null!;

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
                if (!ContinueSavedLife())
                {
                    NewLife();
                }
            }
            catch (System.Exception ex)
            {
                // Sin contenido no hay juego: se muestra el error para poder copiarlo.
                _name.text = "Error";
                _details.text = ex.Message;
                Debug.LogException(ex);
            }
        }

        private void NewLife()
        {
            // Fuera de la simulación sí se puede usar el reloj: solo elige el número de partida.
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks;
            _presenter = new LifePresenter(new LifeSession(_content!, seed));
            _journal.Clear();
            Refresh();
            Save();
        }

        /// <summary>
        /// Al abrir el juego se sigue la vida guardada, si hay. Si el guardado está dañado
        /// se aparta y se empieza una vida nueva (el error queda en la consola).
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

            // El diario de antes se muestra completo; lo nuevo empieza desde aquí.
            _journal.Clear();
            IReadOnlyList<JournalLine> past = _presenter.FullJournal();
            for (int i = past.Count - 1; i >= 0 && _journal.Count < JournalLines; i--)
            {
                _journal.Add(past[i]);
            }

            _presenter.TakeNewJournal();
            Refresh();
            return true;
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
            RectTransform safe = UiFactory.Child(canvas, "Zona segura");
            UiFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            RectTransform page = UiFactory.Column(safe, "Pagina", 32, 24);
            UiFactory.Stretch(page);

            RectTransform header = UiFactory.Column(page, "Personaje", 36, 8, UiFactory.Card);
            _name = UiFactory.Label(header, string.Empty, 56, UiFactory.Ink, FontStyle.Bold);
            _details = UiFactory.Label(header, string.Empty, 32, UiFactory.MutedInk);

            // Hueco transparente donde se ve la casa en 3D.
            RectTransform window = UiFactory.Child(page, "Casa");
            window.gameObject.AddComponent<LayoutElement>().preferredHeight = 560;
            _home = HomeScene.Create();
            _home.Viewport = window;
            BuildViewSwitch(window);

            RectTransform stats = UiFactory.Column(page, "Estado", 36, 18, UiFactory.Card);
            _health = UiFactory.Bar(stats, "Salud", UiFactory.Health, out _healthLabel);
            _happiness = UiFactory.Bar(stats, "Felicidad", UiFactory.Happiness, out _happinessLabel);
            _smarts = UiFactory.Bar(stats, "Inteligencia", UiFactory.Smarts, out _smartsLabel);
            _money = UiFactory.Label(stats, string.Empty, 36, UiFactory.Ink, FontStyle.Bold);

            _story = UiFactory.ScrollColumn(page, "Historia", 36, 20);
            _footer = UiFactory.Column(page, "Acciones", 0, 16);
        }

        /// <summary>
        /// Encima de la ventana 3D: botones para ver la pieza, el barrio o el trabajo (arriba a la derecha)
        /// y una etiqueta que dice adónde va el personaje esta semana (abajo a la izquierda).
        /// </summary>
        private void BuildViewSwitch(RectTransform window)
        {
            RectTransform buttons = UiFactory.Child(window, "Vista");
            _viewButtons = buttons;
            buttons.anchorMin = new Vector2(1f, 1f);
            buttons.anchorMax = new Vector2(1f, 1f);
            buttons.pivot = new Vector2(1f, 1f);
            buttons.anchoredPosition = new Vector2(-16f, -16f);
            buttons.sizeDelta = new Vector2(560f, 84f);
            HorizontalLayoutGroup row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            _roomButton = UiFactory.MakeButton(buttons, string.Empty, UiFactory.Option, UiFactory.Ink, () => SetView(SceneView.Room), 30, 0, 12);
            _blockButton = UiFactory.MakeButton(buttons, string.Empty, UiFactory.Option, UiFactory.Ink, () => SetView(SceneView.Neighborhood), 30, 0, 12);
            _workButton = UiFactory.MakeButton(buttons, string.Empty, UiFactory.Option, UiFactory.Ink, () => SetView(SceneView.Work), 30, 0, 12);

            RectTransform place = UiFactory.Child(window, "Lugar");
            place.anchorMin = Vector2.zero;
            place.anchorMax = Vector2.zero;
            place.pivot = Vector2.zero;
            place.anchoredPosition = new Vector2(16f, 16f);
            place.sizeDelta = new Vector2(480f, 64f);
            place.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);
            _placeLabel = UiFactory.Label(place, string.Empty, 28, UiFactory.Ink, FontStyle.Bold);
            UiFactory.Stretch(_placeLabel.rectTransform);
            _placeLabel.alignment = TextAnchor.MiddleCenter;
            PaintViewSwitch();
        }

        private void SetView(SceneView view)
        {
            _home.View = view;
            PaintViewSwitch();
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
            button.GetComponentInChildren<Text>().color = active ? UiFactory.AccentText : UiFactory.Ink;
        }

        /// <summary>Vuelve a dibujar todo con la foto actual de la partida.</summary>
        private void Refresh()
        {
            LifePresenter presenter = _presenter!;
            foreach (JournalLine line in presenter.TakeNewJournal())
            {
                _journal.Insert(0, line);
            }

            if (_journal.Count > JournalLines)
            {
                _journal.RemoveRange(JournalLines, _journal.Count - JournalLines);
            }

            LifeSnapshot life = presenter.Snapshot();
            _name.text = $"{life.PlayerName}, {life.AgeLabel}";
            _details.text = $"{life.Place}\n{life.DateLabel} · {presenter.Text("ui.generacion", life.Generation)} · {presenter.Text("ui.partida", life.Seed)}";

            _healthLabel.text = $"{presenter.Text("ui.salud")}  {life.Health}";
            _happinessLabel.text = $"{presenter.Text("ui.felicidad")}  {life.Happiness}";
            _smartsLabel.text = $"{presenter.Text("ui.inteligencia")}  {life.Smarts}";
            UiFactory.SetBar(_health, life.Health);
            UiFactory.SetBar(_happiness, life.Happiness);
            UiFactory.SetBar(_smarts, life.Smarts);
            _money.text = $"{presenter.Text("ui.dinero_titulo")}: {life.MoneyLabel}";

            _home.Show(life.Home, life.DailyPlace, life.Workplace);
            _roomButton.GetComponentInChildren<Text>().text = presenter.Text("ui.vista.casa");
            _blockButton.GetComponentInChildren<Text>().text = presenter.Text("ui.vista.barrio");
            _workButton.GetComponentInChildren<Text>().text = presenter.Text("ui.vista.trabajo");

            // El botón Trabajo solo aparece si trabaja; si pierde el trabajo, se vuelve a la casa.
            _workButton.gameObject.SetActive(life.Workplace != null);
            _viewButtons.sizeDelta = new Vector2(life.Workplace != null ? 560f : 380f, 84f);
            if (life.Workplace == null && _home.View == SceneView.Work)
            {
                SetView(SceneView.Room);
            }
            _placeLabel.text = life.DailyPlaceLabel;
            DrawStory(presenter, life);
            DrawActions(presenter, life);
        }

        /// <summary>Parte con scroll: el evento (si hay), la familia y el diario.</summary>
        private void DrawStory(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_story);
            switch (life.Phase)
            {
                case LifePhase.Choosing:
                    EventView pending = life.PendingEvent!;
                    UiFactory.Label(_story, pending.Text, 42, UiFactory.Ink, FontStyle.Bold);
                    for (int i = 0; i < pending.Options.Count; i++)
                    {
                        int option = i;
                        UiFactory.MakeButton(_story, pending.Options[i], UiFactory.Option, UiFactory.Ink, () => Choose(option));
                    }

                    break;
                case LifePhase.ChoosingHeir:
                    UiFactory.Label(_story, presenter.Text("ui.legado"), 42, UiFactory.Ink, FontStyle.Bold);
                    for (int i = 0; i < life.Heirs.Count; i++)
                    {
                        int heir = i;
                        string text = presenter.Text("ui.boton.seguir_con", life.Heirs[i].Name, life.Heirs[i].AgeInYears);
                        UiFactory.MakeButton(_story, text, UiFactory.Option, UiFactory.Ink, () => ContinueWith(heir));
                    }

                    break;
                case LifePhase.Over:
                    UiFactory.Label(_story, presenter.Text("ui.fin"), 42, UiFactory.Ink, FontStyle.Bold);
                    break;
            }

            if (life.Family.Count > 0)
            {
                UiFactory.Label(_story, presenter.Text("ui.familia"), 38, UiFactory.Accent, FontStyle.Bold);
                foreach (PersonView person in life.Family)
                {
                    string age = person.IsAlive ? person.AgeInYears.ToString() : presenter.Text("ui.fallecido");
                    UiFactory.Label(_story, $"{person.Relation}: {person.Name} ({age})", 34, UiFactory.Ink);
                }
            }

            UiFactory.Label(_story, presenter.Text("ui.diario"), 38, UiFactory.Accent, FontStyle.Bold);
            foreach (JournalLine line in _journal)
            {
                UiFactory.Label(_story, $"{line.AgeLabel}: {line.Text}", 34, UiFactory.Ink);
            }
        }

        /// <summary>Botón grande de abajo: seguir viviendo o empezar otra vida.</summary>
        private void DrawActions(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_footer);
            if (life.Phase == LifePhase.Living)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.boton.seguir_viviendo"), UiFactory.Accent, UiFactory.AccentText, Advance);
            }
            else if (life.Phase == LifePhase.Over)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.boton.nueva_vida"), UiFactory.Accent, UiFactory.AccentText, NewLife);
            }
        }

        private void Advance()
        {
            _presenter!.AdvanceUntilSomething();
            Refresh();
            Save();
        }

        private void Choose(int option)
        {
            _presenter!.Choose(option);
            Refresh();
            Save();
        }

        private void ContinueWith(int heir)
        {
            _presenter!.ContinueWith(heir);
            Refresh();
            Save();
        }
    }
}
