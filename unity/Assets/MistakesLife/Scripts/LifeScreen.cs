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
    /// La pantalla principal (docs/diseno-juego.md, "Pantalla principal", boceto S1Filas aprobado).
    /// De arriba hacia abajo: la cabecera (retrato, nombre y edad, curso y comuna, el mes y el
    /// día), la escena 3D del lugar donde estás, "Lo que pasa" (lo que trajo el último paso),
    /// las barras de la vida con su ícono y su porcentaje, los botones (el círculo de Volver al
    /// pasado, "Seguir un mes" y "Un año") y la barra de grupos (Tu gente, Estudios, Dinero,
    /// Celular y Más). Cuando pasa algo importante, la carta del momento ocupa el lugar de las
    /// barras y los botones hasta que el jugador decide.
    /// Se crea sola al apretar Play, en cualquier escena: no hay que configurar nada.
    /// Regla: aquí no se decide nada. Se dibuja la foto de LifePresenter (núcleo) y se le pasan los toques.
    /// </summary>
    public sealed class LifeScreen : MonoBehaviour
    {
        /// <summary>Cuántas líneas del diario se muestran (las más nuevas primero).</summary>
        private const int JournalLines = 60;

        /// <summary>Cuántas líneas nuevas del diario se destacan en "Lo que pasa".</summary>
        private const int NewsLines = 3;

        /// <summary>Alto máximo de lo que va en la hoja de abajo; si es más largo, tiene scroll.</summary>
        private const float MaxSheetBody = 860f;

        /// <summary>Alto máximo de "Lo que pasa" en un mes normal (el resto es para el 3D y las barras).</summary>
        private const float MaxWhatHappens = 420f;

        /// <summary>Fondo de las barras y de la barra de grupos (boceto S1Filas).</summary>
        private static readonly Color Panel = UiFactory.Hex(0x1B2338);

        /// <summary>Fondo vacío de cada barra y la línea de arriba de la barra de grupos.</summary>
        private static readonly Color PanelLine = UiFactory.Hex(0x2A3350);

        /// <summary>Colores de una barra según cómo va: verde, amarilla o roja.</summary>
        private static readonly Color BarGood = UiFactory.Hex(0x3BAE7A);
        private static readonly Color BarMiddle = UiFactory.Hex(0xE0A93B);
        private static readonly Color BarLow = UiFactory.Hex(0xD9534F);

        /// <summary>El botón "Un año": azul, el segundo en importancia.</summary>
        private static readonly Color YearButton = UiFactory.Hex(0x24407A);

        /// <summary>La línea de color de cada cosa de "Lo que pasa": azul lo tuyo, ámbar las noticias.</summary>
        private static readonly Color JournalStrip = UiFactory.Hex(0x5B8DB8);
        private static readonly Color NewsStrip = UiFactory.Hex(0xE0A93B);

        /// <summary>Gris de las etiquetas chicas en mayúscula ("LO QUE PASA").</summary>
        private static readonly Color LabelInk = UiFactory.Hex(0x9AA4BD);

        /// <summary>Cuánto se estiran los fondos de la barra y la hoja fuera de la pantalla.</summary>
        private const float Beyond = 400f;

        /// <summary>Cuánto demora la carta del evento en subir.</summary>
        private const float SlideSeconds = 0.35f;

        /// <summary>Ancho de cada botón Casa, Barrio y Trabajo (van en fila, arriba a la derecha del 3D).</summary>
        private const float ViewSwitchWidth = 180f;

        /// <summary>Alto de la etiqueta de adónde vas, abajo del 3D (dos líneas).</summary>
        private const float PlaceHeight = 100f;

        private readonly List<JournalLine> _journal = new List<JournalLine>();

        /// <summary>Las líneas del diario que trajo el último paso (las más nuevas primero).</summary>
        private readonly List<JournalLine> _lastNews = new List<JournalLine>();

        /// <summary>La noticia del país o la ciudad que salió en el último paso, si salió alguna.</summary>
        private NewsView? _lastHeadline;

        /// <summary>Si la parte de abajo muestra la lista de momentos para volver al pasado.</summary>
        private bool _pickingMoment;

        /// <summary>El capítulo del año que se acaba de cerrar, mientras se muestra en la hoja.</summary>
        private ChapterView? _chapter;
        private ContentDatabase? _content;
        private LifePresenter? _presenter;
        private StartMenu? _menu;
        private Tutorial? _tutorial;
        private PhoneScreen _phone = null!;
        private MoneyScreen _money = null!;
        private CourtScreen _court = null!;
        private StudiesScreen _studies = null!;
        private StudyScreen _study = null!;
        private TeamScreen _team = null!;
        private ActionsScreen _actions = null!;
        private PeopleScreen _people = null!;
        private RectTransform _freeTime = null!;
        private RectTransform _safe = null!;
        private RectTransform _page = null!;
        private float _sheetWidth;
        private RectTransform _sheet = null!;

        // La carta del evento sube desde abajo: cuándo empezó a subir (o -1 si está quieta)
        // y de qué evento es, para animar solo cuando llega uno nuevo.
        private float _slideStart = -1f;
        private EventView? _shownEvent;

        /// <summary>
        /// Transparencia de la hoja mientras la carta sube: parte invisible, así nunca se ve
        /// la carta un instante en su lugar final antes de empezar a subir (el "doble toque").
        /// </summary>
        private CanvasGroup _sheetFade = null!;

        /// <summary>Si la carta se anima al llegar (no en las capturas automáticas, que son fotos quietas).</summary>
        private bool _animate = true;

        /// <summary>El aviso "Arrastra para girar", hasta que el jugador gire la maqueta una vez.</summary>
        private RectTransform _orbitHint = null!;
        private TextMeshProUGUI _orbitHintLabel = null!;

        /// <summary>Clave guardada en el teléfono cuando el jugador ya giró la maqueta (el aviso no vuelve).</summary>
        private const string OrbitSeenKey = "giro_visto";

        /// <summary>Lo que pasó en el último toque (tiempo y cambios), o null si no hay nada que contar.</summary>
        private StepSummary? _lastStep;

        private TextMeshProUGUI _name = null!;
        private TextMeshProUGUI _details = null!;
        private TextMeshProUGUI _initial = null!;
        private RectTransform _summary = null!;
        private TextMeshProUGUI _monthLabel = null!;
        private TextMeshProUGUI _dayLabel = null!;
        private RectTransform _bars = null!;
        private RectTransform _groups = null!;

        /// <summary>Alto de la barra de grupos: ícono, nombre y márgenes (unos 70 puntos en un celular de 390).</summary>
        private const float GroupBarHeight = 194f;
        private RectTransform _more = null!;
        private RectTransform _moreBody = null!;
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
                _details.textWrappingMode = TextWrappingModes.Normal;
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
            _animate = false;
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

        /// <summary>Abre el celular encima de la vida (en sus apps, en el correo o en Mensajito), para las capturas.</summary>
        public void OpenPhoneForCapture(bool mail, bool chats = false, bool openFirstChat = false)
        {
            _phone.Show(_presenter!, mail, chats, openFirstChat);
        }

        /// <summary>Abre la carpeta del juzgado encima de la vida, para las capturas.</summary>
        public void OpenCourtForCapture()
        {
            _court.Show(_presenter!);
        }

        /// <summary>Abre tus estudios en una hoja (0 notas, 1 curso o sección, 2 malla), para las capturas.</summary>
        public void OpenStudiesForCapture(int page)
        {
            _studies.Show(_presenter!, page);
        }

        /// <summary>Abre tu equipo del trabajo encima de la vida, para las capturas.</summary>
        public void OpenTeamForCapture()
        {
            _team.Show(_presenter!);
        }

        /// <summary>Abre lo que puedes hacer en un grupo (gente, estudios, salud), para las capturas.</summary>
        public void OpenActionsForCapture(string group)
        {
            OpenActions(_presenter!, group);
        }

        /// <summary>Abre Tu gente (la lista o la ficha de una persona), para las capturas.</summary>
        public void OpenPeopleForCapture(bool card)
        {
            LifePresenter presenter = _presenter!;
            PeopleView people = presenter.People();
            int? id = card && people.Close.Count > 0 ? people.Close[0].Id : (int?)null;
            _people.Show(presenter, id);
        }

        /// <summary>Abre Tu dinero encima de la vida, para las capturas.</summary>
        public void OpenMoneyForCapture()
        {
            _money.Show(_presenter!);
        }

        /// <summary>Gira la maqueta, para las capturas (la vista desde otro lado).</summary>
        public void OrbitForCapture(float degrees)
        {
            _home.Orbit(degrees);
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
            _presenter.TakeNewNews();
            _lastHeadline = null;
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
            _presenter.TakeNewNews();
            _lastHeadline = null;
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
            room.minHeight = 420;
            _home.Focus = focus;
            OrbitPad pad = OrbitPad.AddTo(focus, _home);
            pad.Tapped = TapScene;
            pad.Turned = OrbitSeen;
            BuildViewSwitch(focus);
            BuildOrbitHint(focus);

            // La hoja de abajo, oscura para leer encima del 3D: lo que pasa, las barras y los botones.
            RectTransform sheet = UiFactory.Column(page, "Hoja", 0, 24);
            VerticalLayoutGroup sheetLayout = sheet.GetComponent<VerticalLayoutGroup>();
            sheetLayout.padding = new RectOffset(44, 44, 28, 28);
            sheet.gameObject.AddComponent<Image>().color = UiFactory.Background;
            _sheet = sheet;
            // La hoja mide lo que tiene: todo el alto que sobra es para el 3D.
            sheet.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0;
            _sheetFade = sheet.gameObject.AddComponent<CanvasGroup>();
            _summary = UiFactory.Column(sheet, "Lo que pasa", 0, 8);
            _sheetBody = UiFactory.ScrollColumn(sheet, "Cuerpo", 0, 20);
            RectTransform scroll = (RectTransform)_sheetBody.parent!.parent!;
            scroll.GetComponent<Image>().color = Color.clear;
            _sheetSize = scroll.GetComponent<LayoutElement>();
            _sheetSize.flexibleHeight = 0;
            // Las tardes libres del mes (D3): cuántas quedan para hacer algo con el tiempo parado.
            _freeTime = UiFactory.Column(sheet, "Tardes libres", 0, 0);
            _bars = UiFactory.Column(sheet, "Barras de la vida", 0, 18, Panel);
            _bars.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(38, 38, 28, 28);
            _footer = UiFactory.Column(sheet, "Botones", 0, 12);

            BuildGroupBar(page);
            BuildHistory(safe);
            BuildMore(safe);
            _phone = new PhoneScreen(safe) { Changed = MailRead };
            _money = new MoneyScreen(safe) { Changed = MailRead };
            _court = new CourtScreen(safe);
            _studies = new StudiesScreen(safe);
            _study = new StudyScreen(safe) { Changed = ActionDone, Requested = MailRead };
            _team = new TeamScreen(safe) { Changed = MailRead };
            _actions = new ActionsScreen(safe) { Changed = ActionDone };
            _people = new PeopleScreen(safe) { Changed = ActionDone };

            // La carta se mueve justo antes de dibujar, después de que Unity ordena la pantalla:
            // si se moviera antes, el orden la devolvería a su lugar y se vería dos veces.
            Canvas.willRenderCanvases += Slide;
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= Slide;
        }

        /// <summary>Abajo del hueco, un aviso chico: "Arrastra para girar". Se va cuando el jugador gira una vez.</summary>
        private void BuildOrbitHint(RectTransform window)
        {
            _orbitHint = UiFactory.Child(window, "Aviso de girar");
            _orbitHint.anchorMin = _orbitHint.anchorMax = new Vector2(0.5f, 0f);
            _orbitHint.pivot = new Vector2(0.5f, 0f);
            _orbitHint.sizeDelta = new Vector2(460f, 72f);
            // Encima de la etiqueta del lugar, que va abajo del todo.
            _orbitHint.anchoredPosition = new Vector2(0f, PlaceHeight + 32f);
            UiFactory.RoundedImage(_orbitHint.gameObject, Translucent(UiFactory.Background, 0.75f)).raycastTarget = false;
            _orbitHintLabel = UiFactory.Label(_orbitHint, string.Empty, UiFactory.MinFontSize, UiFactory.Ink);
            UiFactory.Stretch(_orbitHintLabel.rectTransform);
            _orbitHintLabel.alignment = TextAlignmentOptions.Center;
            _orbitHint.gameObject.SetActive(PlayerPrefs.GetInt(OrbitSeenKey, 0) == 0);
        }

        /// <summary>El jugador giró la maqueta: el aviso se va y no vuelve.</summary>
        private void OrbitSeen()
        {
            _orbitHint.gameObject.SetActive(false);
            PlayerPrefs.SetInt(OrbitSeenKey, 1);
        }

        /// <summary>
        /// Se tocó la maqueta: el objeto que está ahí abre su tema (docs/archivo/diseno-app.md, "La idea").
        /// La mesa y lo que hay del mes (contrato, bolsas del súper, cuentas, la tarjeta) abren Tu
        /// dinero; el celular, el celular; las fotos y la postal, tu gente.
        /// </summary>
        private void TapScene(Vector2 screenPoint)
        {
            LifePresenter? presenter = _presenter;
            if (presenter == null || presenter.Phase != LifePhase.Living)
            {
                return;
            }

            string? id = _home.Pick(screenPoint);
            if (id == null)
            {
                return;
            }

            if (id.StartsWith("escena.casa.celular", System.StringComparison.Ordinal))
            {
                PhoneView phone = presenter.Phone();
                if (phone.HasPhone || phone.HasMail)
                {
                    _phone.Show(presenter);
                }
            }
            else if (id.StartsWith("escena.casa.foto", System.StringComparison.Ordinal) || id.StartsWith("escena.casa.postal", System.StringComparison.Ordinal))
            {
                ShowHistory(true);
            }
            else if (MoneyThings.Contains(id))
            {
                _money.Show(presenter);
            }
            else if (id == "escena.casa.carta_tribunal" || id == "escena.casa.sello_embargo")
            {
                _court.Show(presenter);
            }
            else if ((id == "escena.casa.mochila" || id == "escena.casa.credencial_u") && StudiesScreen.HasStudies(presenter))
            {
                _studies.Show(presenter);
            }
            else if ((id == "escena.casa.credencial_pega" || id == "escena.casa.agenda_pega") && presenter.Team() != null)
            {
                _team.Show(presenter);
            }
        }

        /// <summary>Los objetos de la casa que abren Tu dinero al tocarlos: la mesa y lo que el mes deja encima.</summary>
        private static readonly HashSet<string> MoneyThings = new HashSet<string>
        {
            "mueble.mesa", "escena.casa.cuentas", "escena.casa.contrato_arriendo", "escena.casa.bolsas_super",
            "escena.casa.bebidas", "escena.casa.tarjeta_transporte",
        };

        /// <summary>
        /// Se leyó un correo: se vuelve a dibujar la casa (el celular deja de brillar si ya no
        /// quedan sin leer) y la hoja (el número del botón Celular), sin tocar "Lo que pasó".
        /// </summary>
        private void MailRead()
        {
            LifePresenter presenter = _presenter!;
            LifeSnapshot life = presenter.Snapshot();
            _home.Show(life.Home, life.DailyPlace, life.Workplace);
            DrawSheet(presenter, life);
            DrawGroups(presenter);
            FitSheet();
        }

        /// <summary>
        /// Arriba, siempre a la vista: el retrato con anillo amarillo (lo tuyo) y tus iniciales,
        /// tu nombre y edad, tu curso o pega y tu comuna, y a la derecha el mes en grande y el día.
        /// </summary>
        private void BuildTopBar(RectTransform page)
        {
            RectTransform top = UiFactory.Child(page, "Arriba");
            Backdrop(top, Translucent(UiFactory.Background, 0.92f), Beyond, 0f);
            HorizontalLayoutGroup row = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(44, 44, 30, 26);
            row.spacing = 28;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            RectTransform ring = UiFactory.Child(top, "Retrato");
            Image ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = Icons.Get(Icons.Disc);
            ringImage.color = UiFactory.Accent;
            LayoutElement size = ring.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 112;
            size.minHeight = size.preferredHeight = 112;
            RectTransform face = UiFactory.Child(ring, "Cara");
            UiFactory.Stretch(face);
            face.offsetMin = new Vector2(6f, 6f);
            face.offsetMax = new Vector2(-6f, -6f);
            Image faceImage = face.gameObject.AddComponent<Image>();
            faceImage.sprite = Icons.Get(Icons.Disc);
            faceImage.color = UiFactory.Hex(0xE8C3A0);
            _initial = UiFactory.Label(face, string.Empty, 40, UiFactory.Hex(0x3B2A22), FontStyles.Bold);
            UiFactory.Stretch(_initial.rectTransform);
            _initial.alignment = TextAlignmentOptions.Center;

            RectTransform who = UiFactory.Column(top, "Quien", 0, 2);
            who.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            _name = UiFactory.Label(who, string.Empty, 46, UiFactory.Ink, FontStyles.Bold);
            _name.overflowMode = TextOverflowModes.Ellipsis;
            _name.textWrappingMode = TextWrappingModes.NoWrap;
            _details = UiFactory.Label(who, string.Empty, 37, UiFactory.MutedInk, FontStyles.Bold);
            _details.overflowMode = TextOverflowModes.Ellipsis;
            _details.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform when = UiFactory.Column(top, "Cuando", 0, 2);
            when.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            _monthLabel = UiFactory.Label(when, string.Empty, 55, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _monthLabel.alignment = TextAlignmentOptions.Right;
            _monthLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _dayLabel = UiFactory.Label(when, string.Empty, 36, UiFactory.MutedInk, FontStyles.Bold);
            _dayLabel.alignment = TextAlignmentOptions.Right;
            _dayLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>
        /// Abajo de todo: la barra de grupos (Tu gente, Estudios, Dinero, Celular y Más), con su
        /// ícono de línea fina y un punto rojo si hay algo nuevo. Se llena en <see cref="DrawGroups"/>.
        /// </summary>
        private void BuildGroupBar(RectTransform page)
        {
            RectTransform bar = UiFactory.Child(page, "Grupos");
            LayoutElement barSize = bar.gameObject.AddComponent<LayoutElement>();
            barSize.minHeight = barSize.preferredHeight = GroupBarHeight;
            barSize.flexibleHeight = 0;
            Backdrop(bar, Panel, 0f, Beyond);
            RectTransform line = UiFactory.Child(bar, "Linea");
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            line.gameObject.AddComponent<Image>().color = PanelLine;
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.offsetMin = new Vector2(-Beyond, -3f);
            line.offsetMax = new Vector2(Beyond, 0f);

            // Los botones van en su propia fila, aparte del fondo y la línea: así, al volver a
            // dibujarlos, no se borran el fondo ni la línea.
            _groups = UiFactory.Child(bar, "Botones");
            UiFactory.Stretch(_groups);
            HorizontalLayoutGroup row = _groups.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 22, 22);
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
        }

        /// <summary>"Más": una hoja con los grupos que no caben abajo y el menú del juego.</summary>
        private void BuildMore(RectTransform safe)
        {
            _more = UiFactory.Column(safe, "Mas", 44, 20, UiFactory.Background);
            UiFactory.Stretch(_more);
            _moreBody = UiFactory.ScrollColumn(_more, "Lista", 0, 20);
            ((RectTransform)_moreBody.parent!.parent!).GetComponent<Image>().color = Color.clear;
            _more.gameObject.SetActive(false);
        }

        /// <summary>
        /// En el hueco del 3D: botones para ver la casa, el barrio o el trabajo (en fila, arriba a
        /// la derecha) y, abajo, una etiqueta que dice adónde va el personaje (o su cargo en Trabajo).
        /// </summary>
        private void BuildViewSwitch(RectTransform window)
        {
            RectTransform buttons = UiFactory.Child(window, "Vista");
            _viewButtons = buttons;
            buttons.anchorMin = new Vector2(1f, 1f);
            buttons.anchorMax = new Vector2(1f, 1f);
            buttons.pivot = new Vector2(1f, 1f);
            buttons.anchoredPosition = new Vector2(-24f, -20f);
            HorizontalLayoutGroup column = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
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
            // Abajo, a lo ancho: con dos líneas caben empresa y cargo, que juntos son largos.
            place.anchorMin = new Vector2(0f, 0f);
            place.anchorMax = new Vector2(1f, 0f);
            place.pivot = new Vector2(0.5f, 0f);
            place.offsetMin = new Vector2(24f, 16f);
            place.offsetMax = new Vector2(-24f, 16f + PlaceHeight);
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
            _viewButtons.sizeDelta = new Vector2((count * ViewSwitchWidth) + ((count - 1) * 8f), UiFactory.MinButtonHeight);
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

            // Lo que se anotó en el diario en este paso se muestra en "Lo que pasa".
            _lastNews.Clear();
            for (int i = fresh.Count - 1; i >= 0 && _lastNews.Count < NewsLines; i--)
            {
                _lastNews.Add(fresh[i]);
            }

            // Y la noticia más nueva del país o la ciudad, si salió una en este paso.
            IReadOnlyList<NewsView> headlines = presenter.TakeNewNews();
            _lastHeadline = headlines.Count > 0 ? headlines[headlines.Count - 1] : null;

            if (_journal.Count > JournalLines)
            {
                _journal.RemoveRange(JournalLines, _journal.Count - JournalLines);
            }

            LifeSnapshot life = presenter.Snapshot();
            HeaderView header = presenter.Header();
            _name.text = $"{ShortName(life.PlayerName)} · {life.AgeLabel}";
            var details = new List<string>();
            if (header.Role.Length > 0)
            {
                details.Add(header.Role);
            }

            if (header.Commune.Length > 0)
            {
                details.Add(header.Commune);
            }

            _details.text = string.Join(" · ", details);
            _initial.text = Initials(life.PlayerName);
            _monthLabel.text = header.Month;
            _dayLabel.text = header.Day;

            _home.Show(life.Home, life.DailyPlace, life.Workplace);
            _home.SetMood(life.Happiness);
            _roomButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.vista.casa");
            _blockButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.vista.barrio");
            _workButton.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.vista.trabajo");
            _orbitHintLabel.text = presenter.Text("ui.vista.girar");

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
            DrawFreeTime(presenter, life);
            DrawBars(presenter, life);
            DrawActions(presenter, life);
            DrawGroups(presenter);
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
            // En un mes normal "Lo que pasa" es corto, para dejar a la vista el 3D y las barras;
            // la carta de un momento, un capítulo o la lista de volver al pasado tienen más alto.
            bool normal = _presenter != null && _presenter.Phase == LifePhase.Living && !_pickingMoment && _chapter == null;
            _sheetSize.preferredHeight = Mathf.Min(height, normal ? MaxWhatHappens : MaxSheetBody);
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

        /// <summary>
        /// La hoja con la carta sube desde abajo en un tercio de segundo, frenando al final, y
        /// aparece de a poco. Corre justo antes de dibujar cada cuadro (Canvas.willRenderCanvases),
        /// después de que Unity ordenó la pantalla: así el orden no la devuelve a su lugar final
        /// a mitad de camino (antes se veía la carta un instante arriba, luego abajo y subiendo).
        /// </summary>
        private void Slide()
        {
            if (_slideStart < 0f || _sheet == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_page);
            float t = Mathf.Clamp01((UnityEngine.Time.unscaledTime - _slideStart) / SlideSeconds);
            float eased = 1f - ((1f - t) * (1f - t) * (1f - t));
            Vector2 position = _sheet.anchoredPosition;
            position.y -= (1f - eased) * _sheet.rect.height;
            _sheet.anchoredPosition = position;
            _sheetFade.alpha = Mathf.Clamp01(t * 2.5f);
            if (t >= 1f)
            {
                _slideStart = -1f;
                _sheetFade.alpha = 1f;
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

            if (_chapter != null)
            {
                DrawChapter(presenter, _chapter);
                return;
            }

            if (life.Phase != LifePhase.Choosing)
            {
                _shownEvent = null;
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
        /// Un mes normal: "Lo que pasa" con lo que trajo el último paso (lo que quedó en tu
        /// diario, con su día, y la noticia del país si salió una). Si el paso no trajo nada, la
        /// última línea del diario; si todavía no hay ninguna, una línea que lo dice.
        /// </summary>
        private void DrawWeek(LifePresenter presenter, LifeSnapshot life)
        {
            var lines = new List<JournalLine>(_lastNews);
            if (lines.Count == 0 && _journal.Count > 0)
            {
                lines.Add(_journal[0]);
            }

            if (_lastHeadline != null)
            {
                string tag = presenter.Text("ui.lo_que_pasa.noticias");
                if (_lastHeadline.DateLabel.Length > 0)
                {
                    tag += " · " + _lastHeadline.DateLabel;
                }

                Happening(_sheetBody, tag, _lastHeadline.Headline, NewsStrip);
            }

            foreach (JournalLine line in lines)
            {
                Happening(_sheetBody, line.DateLabel.Length > 0 ? line.DateLabel : line.AgeLabel, line.Text, JournalStrip);
            }

            if (lines.Count == 0 && _lastHeadline == null)
            {
                UiFactory.Label(_sheetBody, presenter.Text("ui.lo_que_pasa.vacio"), 40, UiFactory.MutedInk);
            }
        }

        /// <summary>
        /// Una cosa de "Lo que pasa": una tarjeta de papel con su línea de color a la izquierda,
        /// cuándo fue (chico, en mayúscula) y qué pasó, con letra grande y legible.
        /// </summary>
        private static void Happening(RectTransform parent, string when, string text, Color strip)
        {
            RectTransform card = UiFactory.Child(parent, "Lo que pasa");
            UiFactory.RoundedImage(card.gameObject, UiFactory.Paper);
            HorizontalLayoutGroup row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(30, 32, 26, 26);
            row.spacing = 28;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            RectTransform line = UiFactory.Child(card, "Linea");
            UiFactory.RoundedImage(line.gameObject, strip);
            LayoutElement lineSize = line.gameObject.AddComponent<LayoutElement>();
            lineSize.minWidth = lineSize.preferredWidth = 11;

            RectTransform words = UiFactory.Column(card, "Texto", 0, 4);
            words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(words, when.ToUpperInvariant(), 33, UiFactory.PaperMutedInk, FontStyles.Bold).characterSpacing = 2f;
            UiFactory.Label(words, text, 41, UiFactory.PaperInk);
        }

        /// <summary>
        /// Las barras de la vida (boceto S1Filas): cada una con su ícono propio, su nombre, la
        /// barra verde, amarilla o roja y su porcentaje. Solo en un mes normal: cuando hay que
        /// decidir algo, la carta ocupa su lugar.
        /// </summary>
        private void DrawBars(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_bars);
            bool show = life.Phase == LifePhase.Living && !_pickingMoment && _chapter == null;
            _bars.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            foreach (LifeBarView bar in presenter.LifeBars())
            {
                RectTransform row = UiFactory.Child(_bars, bar.Label);
                HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 28;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                Icons.BarIcon(row, bar.Id, 72f);
                TextMeshProUGUI name = UiFactory.Label(row, bar.Label, 40, UiFactory.Ink, FontStyles.Bold);
                LayoutElement nameSize = name.gameObject.AddComponent<LayoutElement>();
                nameSize.minWidth = nameSize.preferredWidth = 255;

                RectTransform back = UiFactory.Child(row, "Barra");
                UiFactory.RoundedImage(back.gameObject, PanelLine);
                LayoutElement backSize = back.gameObject.AddComponent<LayoutElement>();
                backSize.flexibleWidth = 1;
                backSize.minHeight = backSize.preferredHeight = 34;
                RectTransform fill = UiFactory.Child(back, "Relleno");
                UiFactory.RoundedImage(fill.gameObject, LevelColor(bar.Level));
                UiFactory.Stretch(fill);
                // Una barra en 0 no deja ni la punta redonda: se ve vacía, como debe.
                fill.anchorMax = new Vector2(Mathf.Clamp01(bar.Value / 100f), 1f);
                fill.gameObject.SetActive(bar.Value > 0);

                TextMeshProUGUI percent = UiFactory.Label(row, bar.Value + "%", 39, UiFactory.Ink, FontStyles.Bold);
                percent.alignment = TextAlignmentOptions.Right;
                LayoutElement percentSize = percent.gameObject.AddComponent<LayoutElement>();
                percentSize.minWidth = percentSize.preferredWidth = 110;
            }
        }

        private static Color LevelColor(LifeBarLevel level) =>
            level == LifeBarLevel.Good ? BarGood : level == LifeBarLevel.Middle ? BarMiddle : BarLow;

        /// <summary>
        /// La barra de grupos: Tu gente, Estudios (o Trabajo, si trabajas y no estudias),
        /// Dinero, Celular y Más. Un grupo que todavía no tienes (sin celular, sin estudios ni
        /// pega) se ve apagado y no abre nada. El punto rojo dice que hay algo nuevo o en problemas.
        /// </summary>
        private void DrawGroups(LifePresenter presenter)
        {
            UiFactory.Clear(_groups);
            bool living = presenter.Phase == LifePhase.Living;
            bool studies = StudiesScreen.HasStudies(presenter);
            bool team = presenter.Team() != null;
            PhoneView phone = presenter.Phone();
            bool hasPhone = phone.HasPhone || phone.HasMail;
            MoneyView money = presenter.Money();

            bool failing = false;
            ReportCardView? card = presenter.ReportCard();
            if (card != null)
            {
                foreach (SubjectGradesView subject in card.Subjects)
                {
                    failing |= subject.Failing;
                }
            }

            Group(Icons.People, presenter.Text("ui.grupo.tu_gente"), living, false, () => OpenGroup(presenter, "gente"), Sound.PhotoClick);
            if (studies || !team)
            {
                Group(Icons.Studies, presenter.Text("ui.grupo.estudios"), living && studies, failing, () => OpenGroup(presenter, "estudios"), Sound.PageTurn);
            }
            else
            {
                Group(Icons.Studies, presenter.Text("ui.grupo.trabajo"), living, false, () => OpenGroup(presenter, "trabajo"), Sound.PageTurn);
            }

            Group(Icons.Money, presenter.Text("ui.grupo.dinero"), living, money.Debts.Count > 0, () => _money.Show(presenter), Sound.Register);
            Group(Icons.Phone, presenter.Text("ui.grupo.celular"), living && hasPhone, phone.UnreadMail + phone.UnreadChats > 0, () => _phone.Show(presenter), Sound.PencilTap);
            Group(Icons.More, presenter.Text("ui.grupo.mas"), true, false, () => ShowMore(true), Sound.PageTurn);
        }

        /// <summary>
        /// Al tocar un grupo: si hay algo que hacer en él con el tiempo parado, se abre primero
        /// eso (D3, "dentro de cada grupo"), con un botón a su pantalla de siempre; si no, se abre
        /// directo su pantalla (tu gente y diario, tu libreta, tu equipo).
        /// </summary>
        private void OpenGroup(LifePresenter presenter, string group)
        {
            // Estudios con preguntas reales (D5a): primero tus ramos con su barra de dominio.
            if (group == "estudios" && presenter.StudySubjects().Count > 0)
            {
                System.Action? others = presenter.FreeTime() != null && presenter.Actions(group).Count > 0
                    ? () => OpenActions(presenter, group)
                    : (System.Action?)null;
                _study.Show(presenter, () => _studies.Show(presenter), others);
                return;
            }

            if (presenter.FreeTime() != null && presenter.Actions(group).Count > 0)
            {
                OpenActions(presenter, group);
                return;
            }

            GroupScreen(presenter, group)?.Invoke();
        }

        /// <summary>Abre lo que puedes hacer en un grupo, con el botón a su pantalla de siempre si tiene.</summary>
        private void OpenActions(LifePresenter presenter, string group)
        {
            string title = group == "gente" ? "ui.grupo.tu_gente" : "ui.grupo." + group;
            System.Action? screen = GroupScreen(presenter, group);
            _actions.Show(presenter, group, title, screen, screen != null ? "ui.accion.ver." + group : null);
        }

        /// <summary>La pantalla de siempre de un grupo; null si no tiene (Salud, por ahora).</summary>
        private System.Action? GroupScreen(LifePresenter presenter, string group)
        {
            switch (group)
            {
                case "gente":
                    return () => _people.Show(presenter);
                case "estudios":
                    return () => _studies.Show(presenter);
                case "trabajo":
                    return () => _team.Show(presenter);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Se hizo algo con el tiempo parado: cambian las barras y las tardes libres, y lo que pasó
        /// queda en el diario. El tiempo no avanzó, así que "Lo que pasa" sigue igual.
        /// </summary>
        private void ActionDone()
        {
            LifePresenter presenter = _presenter!;
            LifeSnapshot life = presenter.Snapshot();
            _home.SetMood(life.Happiness);
            DrawFreeTime(presenter, life);
            DrawBars(presenter, life);
            MailRead();
        }

        /// <summary>
        /// La franja de las tardes libres (boceto D3Contador), entre "Lo que pasa" y las barras:
        /// "5 tardes libres en marzo" y que se gastan en lo que haces en cada grupo. Solo en un mes
        /// normal y cuando ya tienes horario.
        /// </summary>
        private void DrawFreeTime(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_freeTime);
            FreeTimeView? free = life.Phase == LifePhase.Living && !_pickingMoment && _chapter == null ? presenter.FreeTime() : null;
            _freeTime.gameObject.SetActive(free != null);
            if (free == null)
            {
                return;
            }

            string note = free.Left > 0 && free.Total > 0 ? presenter.Text("ui.tardes.donde") : free.Note;
            RectTransform box = ActionsScreen.Counter(_freeTime, free, note, dots: false);
            box.GetComponent<Image>().color = Panel;
        }

        /// <summary>Un botón de la barra de grupos: ícono de línea fina, nombre chico y, si hay algo, el punto rojo.</summary>
        private void Group(string icon, string label, bool enabled, bool dot, UnityEngine.Events.UnityAction open, Sound sound)
        {
            RectTransform item = UiFactory.Column(_groups, label, 0, 10);
            // Los cinco del mismo ancho, repartidos en toda la barra.
            LayoutElement width = item.gameObject.AddComponent<LayoutElement>();
            width.minWidth = width.preferredWidth = 0;
            width.flexibleWidth = 1;
            VerticalLayoutGroup layout = item.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            // Una imagen transparente para que se pueda tocar todo el botón, no solo el dibujo.
            Image hit = item.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.interactable = enabled;
            button.onClick.AddListener(() =>
            {
                Sounds.Play(sound);
                open();
            });

            Color color = enabled ? UiFactory.MutedInk : Translucent(UiFactory.MutedInk, 0.35f);
            Image drawing = Icons.Place(item, icon, 72f, color);
            if (dot && enabled)
            {
                RectTransform red = UiFactory.Child(drawing.rectTransform, "Punto rojo");
                Image redImage = red.gameObject.AddComponent<Image>();
                redImage.sprite = Icons.Get(Icons.Disc);
                redImage.color = BarLow;
                red.anchorMin = red.anchorMax = new Vector2(1f, 1f);
                red.sizeDelta = new Vector2(25f, 25f);
                red.anchoredPosition = new Vector2(2f, -2f);
            }

            TextMeshProUGUI name = UiFactory.Label(item, label, UiFactory.MinFontSize, color, FontStyles.Bold);
            name.alignment = TextAlignmentOptions.Center;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.rectTransform.sizeDelta = new Vector2(210f, 44f);
        }

        /// <summary>Abre o cierra "Más": los grupos que no caben abajo y el menú del juego.</summary>
        private void ShowMore(bool show)
        {
            _more.gameObject.SetActive(show);
            if (!show || _presenter == null)
            {
                return;
            }

            LifePresenter presenter = _presenter;
            UiFactory.Clear(_moreBody);
            _moreBody.anchoredPosition = Vector2.zero;
            UiFactory.Label(_moreBody, presenter.Text("ui.mas.titulo"), 52, UiFactory.Ink, FontStyles.Normal, Face.Title);
            bool living = presenter.Phase == LifePhase.Living;
            if (living && StudiesScreen.HasStudies(presenter) && presenter.Team() != null)
            {
                // Si estudias y trabajas a la vez, Estudios va abajo y la pega aquí.
                MoreRow(Icons.Studies, presenter.Text("ui.grupo.trabajo"), () => _team.Show(presenter));
            }

            if (living && presenter.Actions("salud").Count > 0)
            {
                MoreRow(Icons.Health, presenter.Text("ui.grupo.salud"), () => OpenActions(presenter, "salud"));
            }

            if (living && presenter.CourtCases().Count > 0)
            {
                MoreRow(Icons.Law, presenter.Text("ui.grupo.tramites"), () => _court.Show(presenter));
            }

            if (living)
            {
                MoreRow(Icons.History, presenter.Text("ui.grupo.historia"), () => ShowHistory(true));
            }

            MoreRow(Icons.Menu, presenter.Text("ui.mas.menu"), ShowMenu);
            UiFactory.MakeButton(_moreBody, presenter.Text("ui.boton.cerrar"), UiFactory.Option, UiFactory.Ink, () => ShowMore(false));
        }

        /// <summary>Una fila de "Más": ícono y nombre, ancha y fácil de tocar.</summary>
        private void MoreRow(string icon, string label, UnityEngine.Events.UnityAction open)
        {
            RectTransform row = UiFactory.Child(_moreBody, label);
            Image back = UiFactory.RoundedImage(row.gameObject, Panel);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 30, 30);
            layout.spacing = 32;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PageTurn);
                ShowMore(false);
                open();
            });
            Icons.Place(row, icon, 72f, UiFactory.Ink);
            UiFactory.Label(row, label, 44, UiFactory.Ink, FontStyles.Bold).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        /// <summary>
        /// El nombre y el primer apellido ("Vicente Flores Rivera" → "Vicente Flores"), como en el
        /// boceto: así la edad cabe al lado aunque el mes sea largo.
        /// </summary>
        private static string ShortName(string fullName)
        {
            string[] parts = fullName.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 2 ? parts[0] + " " + parts[1] : fullName;
        }

        /// <summary>Las iniciales del nombre y el primer apellido ("Valentina Rojas Pérez" → "VR").</summary>
        private static string Initials(string fullName)
        {
            string[] parts = fullName.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string first = parts[0].Substring(0, 1);
            return parts.Length > 1 ? first + parts[1].Substring(0, 1) : first;
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
            _historyTitle.text = presenter.Text("ui.grupo.historia");
            _closeHistory.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.boton.cerrar");
            UiFactory.Clear(_historyBody);
            _historyBody.anchoredPosition = Vector2.zero;
            UiFactory.Label(_historyBody, $"{presenter.Text("ui.generacion", life.Generation)} · {presenter.Text("ui.partida", life.Seed)}", UiFactory.MinFontSize, UiFactory.MutedInk);

            // El diario por capítulos, del año de ahora hacia atrás.
            UiFactory.Label(_historyBody, presenter.Text("ui.diario"), 40, UiFactory.Ink, FontStyles.Normal, Face.Title);
            IReadOnlyList<ChapterView> chapters = presenter.Chapters();
            for (int i = chapters.Count - 1; i >= 0; i--)
            {
                ChapterView chapter = chapters[i];
                string title = chapter.IsOpen ? presenter.Text("ui.capitulo.abierto") : chapter.Title;
                UiFactory.Label(_historyBody, $"{title} · {chapter.AgeLabel}", 36, UiFactory.Ink, FontStyles.Normal, Face.Hand);
                foreach (ChapterMonthView month in chapter.Months)
                {
                    UiFactory.Label(_historyBody, $"<b>{month.Month}</b>  {string.Join(" ", month.Lines)}", 30, UiFactory.Ink).richText = true;
                }
            }
        }

        /// <summary>
        /// El año que se cerró, como una página del diario: "CAPÍTULO", el título escrito a mano,
        /// la edad, la foto del día que le dio el título (un papel chueco) y lo que pasó mes a mes.
        /// </summary>
        private void DrawChapter(LifePresenter presenter, ChapterView chapter)
        {
            RectTransform page = UiFactory.Column(_sheetBody, "Capitulo", 36, 14, UiFactory.Paper);
            UiFactory.Label(page, presenter.Text("ui.capitulo.titulo").ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            UiFactory.Label(page, chapter.Title, 52, UiFactory.PaperInk, FontStyles.Normal, Face.Hand);
            UiFactory.Label(page, chapter.AgeLabel, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Normal);
            if (chapter.Photo != null)
            {
                var parts = new List<string> { chapter.Photo.When };
                if (chapter.Photo.Place != null)
                {
                    parts.Add(chapter.Photo.Place);
                }

                if (chapter.Photo.Person != null)
                {
                    parts.Add(chapter.Photo.Person);
                }

                RectTransform photo = UiFactory.Column(page, "Foto", 18, 6, UiFactory.PaperOption);
                photo.localRotation = Quaternion.Euler(0f, 0f, 2f);
                UiFactory.Label(photo, string.Join(" · ", parts), UiFactory.MinFontSize, UiFactory.PaperInk, FontStyles.Italic);
            }

            foreach (ChapterMonthView month in chapter.Months)
            {
                UiFactory.Label(page, month.Month, UiFactory.MinFontSize, UiFactory.KeyInk, FontStyles.Bold);
                foreach (string line in month.Lines)
                {
                    UiFactory.Label(page, line, 34, UiFactory.PaperInk, FontStyles.Normal, Face.Hand);
                }
            }

            UiFactory.MakeButton(page, presenter.Text("ui.boton.siguiente"), UiFactory.Accent, UiFactory.AccentText, CloseChapter);
        }

        private void CloseChapter()
        {
            _chapter = null;
            Refresh();
        }

        /// <summary>
        /// El título "LO QUE PASA" arriba de la hoja, con cuánto tiempo corrió en el último
        /// toque, y debajo los cambios que trajo, en verde lo bueno y en rojo lo malo.
        /// </summary>
        private void DrawLastStep(LifePresenter presenter)
        {
            UiFactory.Clear(_summary);
            bool normal = presenter.Phase == LifePhase.Living && !_pickingMoment && _chapter == null;
            if (!normal && _lastStep == null)
            {
                return;
            }

            string title = presenter.Text("ui.lo_que_pasa");
            if (_lastStep != null && _lastStep.ElapsedLabel.Length > 0)
            {
                title += " · " + _lastStep.ElapsedLabel;
            }

            TextMeshProUGUI head = UiFactory.Label(_summary, title.ToUpperInvariant(), UiFactory.MinFontSize, LabelInk, FontStyles.Bold);
            head.characterSpacing = 4f;
            if (_lastStep != null && _lastStep.Changes.Count > 0)
            {
                var parts = new List<string>();
                foreach (ChangeView change in _lastStep.Changes)
                {
                    string color = ColorUtility.ToHtmlStringRGB(change.IsGood ? UiFactory.Good : UiFactory.Bad);
                    parts.Add($"<color=#{color}>{change.Label}</color>");
                }

                TextMeshProUGUI changes = UiFactory.Label(_summary, string.Join("     ", parts), 38, UiFactory.Ink, FontStyles.Bold);
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
            if (_animate && (_shownEvent == null || _shownEvent.EventId != pending.EventId || _shownEvent.Text != pending.Text))
            {
                // Llegó un evento nuevo: la carta sube desde abajo. Hasta el primer cuadro de la
                // subida no se ve, para que no aparezca primero arriba (el "bloque doble").
                _slideStart = UnityEngine.Time.unscaledTime;
                _sheetFade.alpha = 0f;
            }

            _shownEvent = pending;

            // El evento es una hoja de papel (docs/archivo/diseno-app.md, "Un momento").
            RectTransform card = UiFactory.Column(_sheetBody, "Evento", 36, 18, UiFactory.Paper);

            // Arriba: la edad (y "MOMENTO CLAVE") a la izquierda y el sello "ESTO SE RECUERDA" a la derecha.
            RectTransform head = UiFactory.Child(card, "Encabezado");
            HorizontalLayoutGroup headRow = head.gameObject.AddComponent<HorizontalLayoutGroup>();
            headRow.spacing = 16;
            headRow.childAlignment = TextAnchor.MiddleLeft;
            headRow.childControlWidth = true;
            headRow.childControlHeight = true;
            headRow.childForceExpandWidth = false;
            headRow.childForceExpandHeight = false;
            string tag = pending.IsKeyMoment ? $"{life.AgeLabel} · {presenter.Text("ui.momento_clave")}" : life.AgeLabel;
            TextMeshProUGUI tagLabel = UiFactory.Label(head, pending.IsKeyMoment ? tag.ToUpperInvariant() : tag, 34, pending.IsKeyMoment ? UiFactory.KeyInk : UiFactory.PaperMutedInk, FontStyles.Bold);
            tagLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            if (pending.Remembered)
            {
                Stamp(head, presenter.Text("ui.evento.se_recuerda").ToUpperInvariant());
            }

            UiFactory.Label(card, pending.Text, 44, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            if (pending.Watching != null)
            {
                // Los testigos de lo que hiciste antes: están aquí, mirando.
                UiFactory.Label(card, pending.Watching, UiFactory.MinFontSize, UiFactory.KeyInk, FontStyles.Italic);
            }

            if (pending.Origin != null)
            {
                DrawOrigin(card, pending.Origin);
            }

            for (int i = 0; i < pending.Options.Count; i++)
            {
                int option = i;

                // Después de volver al pasado, la opción de la otra vez se marca como intuición.
                bool intuition = pending.IntuitionOption == i;
                string text = intuition ? presenter.Text("ui.intuicion", pending.Options[i]) : pending.Options[i];
                string noticed = pending.NoticedBy[i];
                if (noticed.Length > 0)
                {
                    // Debajo, más chico: quién lo va a notar si eliges esto.
                    text += $"\n<size=80%><i>{noticed}</i></size>";
                }

                Button button = UiFactory.MakeButton(card, text, intuition ? UiFactory.Accent : UiFactory.PaperOption, intuition ? UiFactory.AccentText : UiFactory.PaperInk, () => Choose(option));
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Left;
                label.richText = true;
            }
        }

        /// <summary>
        /// "Esto viene de acá": una foto vieja pegada en la hoja, un poco chueca, con el día en
        /// que empezó lo que hoy vuelve (cuándo, dónde, con quién y qué elegiste) y, si se puede,
        /// el botón para volver a ese día ahí mismo.
        /// </summary>
        private void DrawOrigin(RectTransform card, SeedOriginView origin)
        {
            RectTransform photo = UiFactory.Column(card, "Origen", 22, 8, UiFactory.PaperOption);
            photo.localRotation = Quaternion.Euler(0f, 0f, -1.5f);
            UiFactory.Label(photo, origin.Title.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.KeyInk, FontStyles.Bold);
            var where = new List<string> { origin.When };
            if (origin.Place != null)
            {
                where.Add(origin.Place);
            }

            if (origin.Person != null)
            {
                where.Add(origin.Person);
            }

            UiFactory.Label(photo, string.Join(" · ", where), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            UiFactory.Label(photo, origin.Moment, UiFactory.MinFontSize, UiFactory.PaperInk, FontStyles.Italic);
            if (origin.Choice != null)
            {
                UiFactory.Label(photo, origin.Choice, UiFactory.MinFontSize, UiFactory.PaperInk, FontStyles.Normal);
            }

            if (origin.GoBackLabel != null)
            {
                UiFactory.MakeButton(photo, origin.GoBackLabel, UiFactory.Accent, UiFactory.AccentText, GoBackToOrigin);
            }
        }

        /// <summary>Vuelve al pasado justo frente al error: al día de la foto "Esto viene de acá".</summary>
        private void GoBackToOrigin()
        {
            _presenter!.GoBackToOrigin();
            _pickingMoment = false;
            _chapter = null;
            _lastStep = null;
            ReloadJournal();
            Refresh();
            Save();
        }

        /// <summary>El sello "ESTO SE RECUERDA": un timbre rojo, un poco chueco, como puesto a mano.</summary>
        private static void Stamp(RectTransform parent, string text)
        {
            RectTransform stamp = UiFactory.Child(parent, "Sello");
            UiFactory.RoundedImage(stamp.gameObject, UiFactory.KeyInk);
            HorizontalLayoutGroup inner = stamp.gameObject.AddComponent<HorizontalLayoutGroup>();
            inner.padding = new RectOffset(16, 16, 8, 8);
            inner.childControlWidth = true;
            inner.childControlHeight = true;
            inner.childForceExpandWidth = false;
            inner.childForceExpandHeight = false;
            UiFactory.Label(stamp, text, 28, UiFactory.Paper, FontStyles.Bold);
            stamp.localRotation = Quaternion.Euler(0f, 0f, 4f);
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
        /// Los botones (boceto S1Filas, diseño 1 de los tres botones): el círculo chico de Volver
        /// al pasado con sus cargas (solo si hay cargas y un momento al que volver), "Seguir un
        /// mes" ancho y amarillo, y "Un año" al lado. Los dos se detienen si pasa algo importante.
        /// Cuando hay que decidir algo o la vida terminó, Volver al pasado va como botón ancho.
        /// </summary>
        private void DrawActions(LifePresenter presenter, LifeSnapshot life)
        {
            UiFactory.Clear(_footer);
            if (_pickingMoment)
            {
                return;
            }

            TimeTravelView travel = presenter.TimeTravel();
            if (life.Phase != LifePhase.Living && travel.CanGoBack)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.boton.volver_pasado"), UiFactory.Option, UiFactory.Ink, () => PickMoment(true));
            }

            if (life.Phase == LifePhase.Living && _chapter == null)
            {
                RectTransform row = Row(_footer, "Avanzar");
                HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 28;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                if (travel.CanGoBack && travel.Charges > 0)
                {
                    GoBackCircle(row, presenter, travel.Charges);
                }

                Button month = TimeButton(row, presenter.Text("ui.boton.un_mes"), presenter.Text("ui.boton.un_mes.nota"), UiFactory.Accent, UiFactory.Hex(0x2A2008), UiFactory.Hex(0xB98F1E), AdvanceMonth);
                month.GetComponent<LayoutElement>().flexibleWidth = 1;
                Button year = TimeButton(row, presenter.Text("ui.boton.un_ano"), null, YearButton, UiFactory.Ink, UiFactory.Hex(0x14254A), AdvanceYear);
                LayoutElement yearSize = year.GetComponent<LayoutElement>();
                yearSize.minWidth = yearSize.preferredWidth = 230;
            }
            else if (life.Phase == LifePhase.Over)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.boton.nueva_vida"), UiFactory.Accent, UiFactory.AccentText, () => _menu!.ShowBirths());
            }
        }

        /// <summary>
        /// Un botón de avanzar el tiempo: con su sombra abajo (se ve apretable), el nombre en la
        /// letra de títulos y, si tiene, una nota chica debajo.
        /// </summary>
        private static Button TimeButton(RectTransform row, string text, string? note, Color color, Color ink, Color shadow, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = UiFactory.Child(row, text);
            LayoutElement size = rect.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 172;

            RectTransform under = UiFactory.Child(rect, "Sombra");
            UiFactory.RoundedImage(under.gameObject, shadow).raycastTarget = false;
            UiFactory.Stretch(under);
            under.offsetMin = new Vector2(0f, -11f);
            under.offsetMax = new Vector2(0f, -11f);

            RectTransform face = UiFactory.Column(rect, "Boton", 0, 0, color);
            UiFactory.Stretch(face);
            VerticalLayoutGroup layout = face.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(16, 16, 12, 12);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face.GetComponent<Image>();
            button.onClick.AddListener(onClick);

            TextMeshProUGUI label = UiFactory.Label(face, text, 55, ink, FontStyles.Normal, Face.Title);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (note != null)
            {
                TextMeshProUGUI small = UiFactory.Label(face, note, UiFactory.MinFontSize, ink, FontStyles.Bold);
                small.alignment = TextAlignmentOptions.Center;
            }

            return button;
        }

        /// <summary>El círculo chico de Volver al pasado: el reloj con la flecha hacia atrás y, en la esquina, las cargas que quedan.</summary>
        private void GoBackCircle(RectTransform row, LifePresenter presenter, int charges)
        {
            RectTransform rect = UiFactory.Child(row, "Volver al pasado");
            LayoutElement size = rect.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 138;
            size.minHeight = size.preferredHeight = 138;
            Image rim = rect.gameObject.AddComponent<Image>();
            rim.sprite = Icons.Get(Icons.Disc);
            rim.color = UiFactory.Hex(0x6C7898);
            RectTransform inside = UiFactory.Child(rect, "Fondo");
            UiFactory.Stretch(inside);
            inside.offsetMin = new Vector2(6f, 6f);
            inside.offsetMax = new Vector2(-6f, -6f);
            Image fill = inside.gameObject.AddComponent<Image>();
            fill.sprite = Icons.Get(Icons.Disc);
            fill.color = Panel;
            fill.raycastTarget = false;
            Image clock = Icons.Place(rect, Icons.GoBack, 70f, UiFactory.MutedInk);
            clock.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform badge = UiFactory.Child(rect, "Cargas");
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = Icons.Get(Icons.Disc);
            badgeImage.color = UiFactory.Hex(0x8FA0C4);
            badgeImage.raycastTarget = false;
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(56f, 56f);
            badge.anchoredPosition = new Vector2(-6f, -6f);
            TextMeshProUGUI count = UiFactory.Label(badge, charges.ToString(), UiFactory.MinFontSize, UiFactory.Background, FontStyles.Bold);
            UiFactory.Stretch(count.rectTransform);
            count.alignment = TextAlignmentOptions.Center;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rim;
            button.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PageTurn);
                PickMoment(true);
            });
        }

        /// <summary>"Seguir un mes": hasta el mes que viene, o antes si pasa algo importante.</summary>
        private void AdvanceMonth() => Advance(false);

        /// <summary>"Un año": un año entero, o menos si pasa algo importante.</summary>
        private void AdvanceYear() => Advance(true);

        /// <summary>Avanza un mes o, con <paramref name="year"/>, un año; los dos se detienen si pasa algo importante.</summary>
        private void Advance(bool year)
        {
            Sounds.Play(Sound.WoodKnock);
            LifeSnapshot before = _presenter!.Snapshot();
            StudyMark studiesBefore = StudyMark.Of(_presenter);
            int weeks = year ? _presenter.AdvanceYear() : _presenter.AdvanceMonth();
            _chapter = _presenter.TakeClosedChapter() ?? _chapter;
            RememberStep(before, weeks);
            studiesBefore.PlayChange(StudyMark.Of(_presenter));
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
            _chapter = null;
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
