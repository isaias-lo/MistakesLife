#nullable enable

using System;
using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// El celular del personaje (docs/diseno-app.md, "Redes"). Se abre encima de la vida y se ve
    /// como un celular de verdad: marco oscuro con su cámara arriba, barra de estado con la fecha,
    /// la señal y la batería, el fondo de pantalla, las apps como íconos de color con su globo
    /// rojo de avisos, y abajo el dock y la rayita de inicio. El correo se ve como una app de
    /// correo: blanca, con la bandeja "Recibidos" (inicial de quien manda, asunto, vista previa,
    /// hace cuánto llegó y un punto azul si no está leído) y cada correo abierto como un mensaje.
    /// Mensajito se ve como una app de mensajes: la lista de chats (foto con inicial, nombre, último
    /// mensaje, hace cuánto y el globo verde de no vistos) y cada conversación con sus burbujas
    /// (blancas las de la otra persona, verdes las tuyas) sobre el fondo beige, y abajo lo que
    /// puedes contestar. Regla: aquí no se decide nada; se dibuja lo que da LifePresenter (Phone,
    /// Mail, Chats, Chat) y se le pasan los toques (ReadMail, ReadChat, ReplyChat).
    /// </summary>
    public sealed class PhoneScreen
    {
        /// <summary>El material del celular (docs/diseno-app.md, colores de cada tema): el marco.</summary>
        public static readonly Color PhoneBody = UiFactory.Hex(0x0B0F1A);

        /// <summary>El fondo de pantalla, un poco más claro que el marco.</summary>
        private static readonly Color Wallpaper = UiFactory.Hex(0x1C2B4D);

        /// <summary>La app de correo, blanca como las de verdad.</summary>
        private static readonly Color MailWhite = UiFactory.Hex(0xFAFAFC);
        private static readonly Color MailLine = UiFactory.Hex(0xE3E5EC);
        private static readonly Color MailBlue = UiFactory.Hex(0x1A73E8);

        /// <summary>Mensajito: la barra verde, el fondo de la conversación y las burbujas.</summary>
        private static readonly Color ChatGreen = UiFactory.Hex(0x1F7A4A);
        private static readonly Color ChatWallpaper = UiFactory.Hex(0xECE5DD);
        private static readonly Color BubbleMine = UiFactory.Hex(0xD9F7C4);
        private static readonly Color BubbleTheirs = UiFactory.Hex(0xFFFFFF);
        private static readonly Color ChatBadge = UiFactory.Hex(0x25A35A);

        private const string ChatAppId = "app.mensajito";

        /// <summary>Cuántas apps van por fila (y en el dock).</summary>
        private const int AppsPerRow = 4;

        /// <summary>Radio de las esquinas de <see cref="UiFactory.Rounded"/> (para dibujar círculos).</summary>
        private const float RoundedCorner = 28f;

        private const float IconSize = 150f;

        private readonly Transform _parent;
        private RectTransform? _layer;
        private RectTransform _screen = null!;
        private Image _screenImage = null!;
        private TextMeshProUGUI _clock = null!;
        private RectTransform _appBar = null!;
        private Image _appBarImage = null!;
        private TextMeshProUGUI _appTitle = null!;
        private Button _back = null!;
        private Image _scrollImage = null!;
        private RectTransform _body = null!;
        private RectTransform _dock = null!;
        private TextMeshProUGUI _closeLabel = null!;
        private LifePresenter? _presenter;

        public PhoneScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama cuando se lee un correo, para que la vida actualice el punto rojo.</summary>
        public Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>Abre el celular en la pantalla de inicio (las apps), o directo en la bandeja del correo o en Mensajito.</summary>
        public void Show(LifePresenter presenter, bool mail = false, bool chats = false, bool openFirstChat = false)
        {
            _presenter = presenter;
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            if (mail)
            {
                DrawInbox();
            }
            else if (chats)
            {
                IReadOnlyList<ChatSummaryView> list = presenter.Chats();
                if (openFirstChat && list.Count > 0)
                {
                    // Para las capturas: la conversación de más arriba abierta.
                    DrawConversation(list[0].OtherId);
                }
                else
                {
                    DrawChats();
                }
            }
            else
            {
                DrawHome();
            }
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>El punto rojo de "algo nuevo" (docs/diseno-app.md, "tus cosas"), en la esquina de un botón.</summary>
        public static void RedDot(Transform button)
        {
            RectTransform dot = Corner(button, "Punto rojo", 28f, new Vector2(-10f, -10f));
            Circle(dot.gameObject, UiFactory.Bad, 28f);
        }

        /// <summary>
        /// Arma una sola vez la capa: fondo oscuro, el celular al centro (marco, cámara, pantalla con
        /// barra de estado, barra de la app, lo que se ve, dock y rayita) y Cerrar debajo.
        /// </summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Celular", UiFactory.Shade);

            RectTransform device = UiFactory.Child(_layer, "Marco del celular");
            device.anchorMin = new Vector2(0.07f, 0.11f);
            device.anchorMax = new Vector2(0.93f, 0.97f);
            device.offsetMin = Vector2.zero;
            device.offsetMax = Vector2.zero;
            UiFactory.RoundedImage(device.gameObject, PhoneBody).pixelsPerUnitMultiplier = 0.5f;

            _screen = UiFactory.Child(device, "Pantalla");
            _screen.anchorMin = Vector2.zero;
            _screen.anchorMax = Vector2.one;
            _screen.offsetMin = new Vector2(20f, 20f);
            _screen.offsetMax = new Vector2(-20f, -20f);
            _screenImage = UiFactory.RoundedImage(_screen.gameObject, Wallpaper);
            _screenImage.pixelsPerUnitMultiplier = 0.6f;
            _screen.gameObject.AddComponent<RectMask2D>();
            VerticalLayoutGroup column = _screen.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(0, 0, 0, 14);
            column.spacing = 0;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            BuildStatusBar(_screen);
            BuildAppBar(_screen);

            RectTransform content = UiFactory.ScrollColumn(_screen, "Lo que se ve", 28, 20);
            _body = content;
            _scrollImage = ((RectTransform)content.parent!.parent!).GetComponent<Image>();
            _scrollImage.color = Color.clear;

            _dock = UiFactory.Child(_screen, "Dock");
            HorizontalLayoutGroup dock = _dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            dock.padding = new RectOffset(28, 28, 18, 18);
            dock.spacing = 12;
            dock.childControlWidth = true;
            dock.childControlHeight = true;
            dock.childForceExpandWidth = true;
            dock.childForceExpandHeight = false;
            UiFactory.RoundedImage(_dock.gameObject, new Color(1f, 1f, 1f, 0.12f));

            // La rayita de abajo de los celulares sin botón.
            RectTransform homeLine = UiFactory.Child(_screen, "Rayita de inicio");
            homeLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
            RectTransform bar = UiFactory.Child(homeLine, "Rayita");
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(260f, 12f);
            UiFactory.RoundedImage(bar.gameObject, new Color(1f, 1f, 1f, 0.6f)).pixelsPerUnitMultiplier = 4f;

            // La cámara de adelante, una pastilla negra arriba al centro (encima de la pantalla).
            RectTransform notch = UiFactory.Child(device, "Camara");
            notch.anchorMin = notch.anchorMax = new Vector2(0.5f, 1f);
            notch.pivot = new Vector2(0.5f, 1f);
            notch.sizeDelta = new Vector2(190f, 44f);
            notch.anchoredPosition = new Vector2(0f, -30f);
            UiFactory.RoundedImage(notch.gameObject, Color.black).pixelsPerUnitMultiplier = 1.3f;

            // Cerrar, debajo del celular.
            RectTransform under = UiFactory.Child(_layer, "Debajo del celular");
            under.anchorMin = new Vector2(0.25f, 0.02f);
            under.anchorMax = new Vector2(0.75f, 0.095f);
            under.offsetMin = Vector2.zero;
            under.offsetMax = Vector2.zero;
            VerticalLayoutGroup underLayout = under.gameObject.AddComponent<VerticalLayoutGroup>();
            underLayout.childControlWidth = true;
            underLayout.childControlHeight = true;
            underLayout.childForceExpandWidth = true;
            underLayout.childForceExpandHeight = true;
            Button close = UiFactory.MakeButton(under, string.Empty, UiFactory.Option, UiFactory.Ink, Close, UiFactory.MinFontSize, 0, 12);
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>Arriba de la pantalla: la fecha a la izquierda, la señal y la batería a la derecha.</summary>
        private void BuildStatusBar(RectTransform screen)
        {
            RectTransform status = UiFactory.Child(screen, "Barra de estado");
            HorizontalLayoutGroup row = status.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(44, 40, 26, 10);
            row.spacing = 14;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            _clock = UiFactory.Label(status, string.Empty, UiFactory.MinFontSize, UiFactory.Ink, FontStyles.Bold);
            NoWrap(_clock);
            _clock.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // La señal: cuatro rayitas de alto creciente.
            RectTransform signal = UiFactory.Child(status, "Senal");
            LayoutElement signalSize = signal.gameObject.AddComponent<LayoutElement>();
            signalSize.preferredWidth = signalSize.minWidth = 52;
            signalSize.preferredHeight = 30;
            for (int i = 0; i < 4; i++)
            {
                RectTransform line = UiFactory.Child(signal, "Rayita de senal");
                line.anchorMin = line.anchorMax = new Vector2(0f, 0f);
                line.pivot = new Vector2(0f, 0f);
                line.sizeDelta = new Vector2(9f, 9f + (i * 7f));
                line.anchoredPosition = new Vector2(i * 13f, 0f);
                line.gameObject.AddComponent<Image>().color = UiFactory.Ink;
            }

            // La batería: un borde, el relleno y la puntita.
            RectTransform battery = UiFactory.Child(status, "Bateria");
            LayoutElement batterySize = battery.gameObject.AddComponent<LayoutElement>();
            batterySize.preferredWidth = batterySize.minWidth = 62;
            batterySize.preferredHeight = 30;
            RectTransform shell = UiFactory.Child(battery, "Borde");
            shell.anchorMin = new Vector2(0f, 0f);
            shell.anchorMax = new Vector2(0.9f, 1f);
            shell.offsetMin = shell.offsetMax = Vector2.zero;
            UiFactory.RoundedImage(shell.gameObject, new Color(1f, 1f, 1f, 0.45f)).pixelsPerUnitMultiplier = 3f;
            RectTransform fill = UiFactory.Child(shell, "Carga");
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0.8f, 1f);
            fill.offsetMin = new Vector2(4f, 4f);
            fill.offsetMax = new Vector2(0f, -4f);
            UiFactory.RoundedImage(fill.gameObject, UiFactory.Ink).pixelsPerUnitMultiplier = 4f;
            RectTransform tip = UiFactory.Child(battery, "Puntita");
            tip.anchorMin = new Vector2(0.93f, 0.33f);
            tip.anchorMax = new Vector2(1f, 0.67f);
            tip.offsetMin = tip.offsetMax = Vector2.zero;
            tip.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.45f);
        }

        /// <summary>La barra de una app abierta: Volver a la izquierda y el nombre de la app.</summary>
        private void BuildAppBar(RectTransform screen)
        {
            _appBar = UiFactory.Child(screen, "Barra de la app");
            _appBarImage = _appBar.gameObject.AddComponent<Image>();
            HorizontalLayoutGroup row = _appBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 28, 8, 8);
            row.spacing = 12;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            _back = UiFactory.MakeButton(_appBar, string.Empty, Color.clear, MailBlue, DrawHome, UiFactory.MinFontSize, 0, 12);
            LayoutElement backSize = _back.GetComponent<LayoutElement>();
            backSize.minWidth = backSize.preferredWidth = 190;
            _appTitle = UiFactory.Label(_appBar, string.Empty, 44, UiFactory.PaperInk, FontStyles.Bold);
            NoWrap(_appTitle);
            _appTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        /// <summary>
        /// La pantalla de inicio: la fecha grande sobre el fondo, las apps en filas de cuatro
        /// y las primeras cuatro en el dock, cada una con su globo rojo si tiene algo nuevo.
        /// </summary>
        private void DrawHome()
        {
            LifePresenter presenter = _presenter!;
            PhoneView phone = presenter.Phone();
            Prepare(home: true, title: string.Empty, back: null);

            TextMeshProUGUI date = UiFactory.Label(_body, presenter.Snapshot().DateLabel, 54, UiFactory.Ink, FontStyles.Normal, Face.Title);
            date.alignment = TextAlignmentOptions.Center;
            Space(_body, 40f);

            if (!phone.HasPhone)
            {
                TextMeshProUGUI note = UiFactory.Label(_body, presenter.Text(phone.HasMail ? "ui.celular.solo_correo" : "ui.celular.sin_nada"), 38, UiFactory.Ink);
                note.alignment = TextAlignmentOptions.Center;
                if (phone.HasMail)
                {
                    Icon(_dock, AppView.MailAppId, presenter.Text(AppView.MailAppId), phone.UnreadMail, DrawInbox);
                    FillRow(_dock, 1);
                }
                else
                {
                    _dock.gameObject.SetActive(false);
                }

                return;
            }

            // Las primeras cuatro van al dock; las demás, en filas sobre el fondo.
            RectTransform? line = null;
            int inGrid = 0;
            for (int i = AppsPerRow; i < phone.Apps.Count; i++)
            {
                if (inGrid % AppsPerRow == 0)
                {
                    line = AppRow(_body);
                }

                AddApp(line!, phone.Apps[i]);
                inGrid++;
            }

            if (line != null)
            {
                FillRow(line, inGrid);
            }

            int inDock = Math.Min(AppsPerRow, phone.Apps.Count);
            for (int i = 0; i < inDock; i++)
            {
                AddApp(_dock, phone.Apps[i]);
            }

            FillRow(_dock, inDock);
        }

        private void AddApp(RectTransform row, AppView app)
        {
            Action open = app.IsMail ? (Action)DrawInbox
                : app.AppId == ChatAppId ? (Action)DrawChats
                : () => DrawEmptyApp(app);
            Icon(row, app.AppId, app.Name, app.Unread, open);
        }

        /// <summary>Una app que todavía no tiene nada adentro: su ícono grande, su nombre y el aviso.</summary>
        private void DrawEmptyApp(AppView app)
        {
            Prepare(home: false, title: app.Name, back: DrawHome);
            _appBarImage.color = Wallpaper;
            _appTitle.color = UiFactory.Ink;
            Space(_body, 120f);
            RectTransform holder = UiFactory.Child(_body, "Icono grande");
            HorizontalLayoutGroup center = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
            center.childAlignment = TextAnchor.MiddleCenter;
            center.childControlWidth = false;
            center.childControlHeight = false;
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = 220;
            RectTransform icon = UiFactory.Child(holder, "Icono");
            icon.sizeDelta = new Vector2(220f, 220f);
            UiFactory.RoundedImage(icon.gameObject, AppColor(app.AppId)).pixelsPerUnitMultiplier = 0.55f;
            Letter(icon, app.Name, 110);
            Space(_body, 20f);
            TextMeshProUGUI name = UiFactory.Label(_body, app.Name, 48, UiFactory.Ink, FontStyles.Bold, Face.Title);
            name.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI empty = UiFactory.Label(_body, _presenter!.Text("ui.celular.app_vacia"), 38, UiFactory.MutedInk);
            empty.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// La bandeja, como una app de correo: "Recibidos", cuántos sin leer, y cada correo en una
        /// fila con la inicial de quien manda, el asunto, la vista previa y hace cuánto llegó.
        /// </summary>
        private void DrawInbox()
        {
            LifePresenter presenter = _presenter!;
            PhoneView phone = presenter.Phone();
            Prepare(home: false, title: presenter.Text("ui.correo.titulo"), back: phone.HasPhone ? DrawHome : (Action?)null);
            MailTheme();

            TextMeshProUGUI title = UiFactory.Label(_body, presenter.Text("ui.correo.recibidos"), 56, UiFactory.PaperInk, FontStyles.Bold, Face.Title);
            NoWrap(title);
            if (phone.UnreadMail > 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.correo.sin_leer", phone.UnreadMail), UiFactory.MinFontSize, MailBlue, FontStyles.Bold);
            }

            IReadOnlyList<MailView> mail = presenter.Mail();
            if (mail.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.correo.vacio"), 38, UiFactory.PaperMutedInk);
                return;
            }

            foreach (MailView message in mail)
            {
                MailRow(message);
            }
        }

        /// <summary>Una fila de la bandeja; tocarla abre el correo.</summary>
        private void MailRow(MailView message)
        {
            MailView chosen = message;
            RectTransform row = UiFactory.Child(_body, "Correo");
            row.gameObject.AddComponent<Image>().color = MailWhite;
            Button open = row.gameObject.AddComponent<Button>();
            open.targetGraphic = row.GetComponent<Image>();
            open.onClick.AddListener(() => DrawMessage(chosen));
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 16, 22);
            layout.spacing = 22;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Avatar(row, message.Sender, message.Topic, 96f);

            RectTransform text = UiFactory.Column(row, "Texto del correo", 0, 4);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            FontStyles weight = message.Read ? FontStyles.Normal : FontStyles.Bold;

            RectTransform top = UiFactory.Child(text, "Quien y cuando");
            HorizontalLayoutGroup topRow = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topRow.spacing = 12;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;
            TextMeshProUGUI sender = UiFactory.Label(top, message.Sender, 38, UiFactory.PaperInk, weight);
            NoWrap(sender);
            sender.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI when = UiFactory.Label(top, message.WhenLabel, UiFactory.MinFontSize, message.Read ? UiFactory.PaperMutedInk : MailBlue, weight);
            NoWrap(when);
            when.alignment = TextAlignmentOptions.Right;

            TextMeshProUGUI subject = UiFactory.Label(text, message.Subject, 36, UiFactory.PaperInk, weight);
            NoWrap(subject);
            TextMeshProUGUI preview = UiFactory.Label(text, message.Preview, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            preview.textWrappingMode = TextWrappingModes.Normal;
            preview.maxVisibleLines = 2;
            preview.overflowMode = TextOverflowModes.Ellipsis;

            if (!message.Read)
            {
                // El punto azul de "sin leer", a la derecha como en los correos de verdad.
                RectTransform dot = UiFactory.Child(row, "Sin leer");
                LayoutElement dotSize = dot.gameObject.AddComponent<LayoutElement>();
                dotSize.minWidth = dotSize.preferredWidth = 24;
                dotSize.minHeight = dotSize.preferredHeight = 24;
                Circle(dot.gameObject, MailBlue, 24f);
            }

            Divider(_body);
        }

        /// <summary>Un correo abierto, como un mensaje: el asunto, quién lo manda y para quién, y el texto. Al abrirlo queda leído.</summary>
        private void DrawMessage(MailView message)
        {
            LifePresenter presenter = _presenter!;
            if (!message.Read)
            {
                presenter.ReadMail(message.Index);
                Changed?.Invoke();
            }

            Prepare(home: false, title: presenter.Text("ui.correo.titulo"), back: DrawInbox);
            MailTheme();

            UiFactory.Label(_body, message.Subject, 50, UiFactory.PaperInk, FontStyles.Normal, Face.Title);

            RectTransform from = UiFactory.Child(_body, "De");
            HorizontalLayoutGroup fromRow = from.gameObject.AddComponent<HorizontalLayoutGroup>();
            fromRow.spacing = 22;
            fromRow.childAlignment = TextAnchor.MiddleLeft;
            fromRow.childControlWidth = true;
            fromRow.childControlHeight = true;
            fromRow.childForceExpandWidth = false;
            fromRow.childForceExpandHeight = false;
            Avatar(from, message.Sender, message.Topic, 96f);
            RectTransform who = UiFactory.Column(from, "Quien", 0, 2);
            who.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI sender = UiFactory.Label(who, message.Sender, 38, UiFactory.PaperInk, FontStyles.Bold);
            NoWrap(sender);
            TextMeshProUGUI to = UiFactory.Label(who, $"{presenter.Text("ui.correo.para_ti")} · {message.WhenLabel}", UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            NoWrap(to);

            Divider(_body);
            UiFactory.Label(_body, message.Body, 38, UiFactory.PaperInk);
            Space(_body, 12f);
            UiFactory.Label(_body, message.DateLabel, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Normal, Face.Mono);
        }

        /// <summary>
        /// Mensajito, la lista de chats: cada persona que te ha escrito, del chat más reciente al
        /// más viejo, con su foto (la inicial), su nombre, el último mensaje y el globo verde.
        /// </summary>
        private void DrawChats()
        {
            LifePresenter presenter = _presenter!;
            Prepare(home: false, title: presenter.Text(ChatAppId), back: DrawHome);
            ChatTheme(conversation: false);

            IReadOnlyList<ChatSummaryView> chats = presenter.Chats();
            if (chats.Count == 0)
            {
                Space(_body, 60f);
                TextMeshProUGUI empty = UiFactory.Label(_body, presenter.Text("ui.mensajito.vacio"), 38, UiFactory.PaperMutedInk);
                empty.alignment = TextAlignmentOptions.Center;
                return;
            }

            foreach (ChatSummaryView chat in chats)
            {
                ChatRow(chat);
            }
        }

        /// <summary>Una fila de la lista de chats; tocarla abre la conversación.</summary>
        private void ChatRow(ChatSummaryView chat)
        {
            int otherId = chat.OtherId;
            RectTransform row = UiFactory.Child(_body, "Chat");
            row.gameObject.AddComponent<Image>().color = MailWhite;
            Button open = row.gameObject.AddComponent<Button>();
            open.targetGraphic = row.GetComponent<Image>();
            open.onClick.AddListener(() => DrawConversation(otherId));
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 16, 16);
            layout.spacing = 22;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ProfilePhoto(row, chat.Name, chat.OtherId, 104f);

            RectTransform text = UiFactory.Column(row, "Texto del chat", 0, 4);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            FontStyles weight = chat.Unread > 0 ? FontStyles.Bold : FontStyles.Normal;

            RectTransform top = UiFactory.Child(text, "Quien y cuando");
            HorizontalLayoutGroup topRow = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topRow.spacing = 12;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;
            TextMeshProUGUI name = UiFactory.Label(top, chat.Name, 40, UiFactory.PaperInk, FontStyles.Bold);
            NoWrap(name);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI when = UiFactory.Label(top, chat.When, UiFactory.MinFontSize, chat.Unread > 0 ? ChatBadge : UiFactory.PaperMutedInk, weight);
            NoWrap(when);
            when.alignment = TextAlignmentOptions.Right;

            RectTransform bottom = UiFactory.Child(text, "Ultimo mensaje");
            HorizontalLayoutGroup bottomRow = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
            bottomRow.spacing = 12;
            bottomRow.childAlignment = TextAnchor.MiddleLeft;
            bottomRow.childControlWidth = true;
            bottomRow.childControlHeight = true;
            bottomRow.childForceExpandWidth = false;
            bottomRow.childForceExpandHeight = false;
            string last = chat.LastFromPlayer ? _presenter!.Text("ui.mensajito.tu", chat.LastMessage) : chat.LastMessage;
            TextMeshProUGUI preview = UiFactory.Label(bottom, last, UiFactory.MinFontSize, chat.Unread > 0 ? UiFactory.PaperInk : UiFactory.PaperMutedInk, weight);
            NoWrap(preview);
            preview.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            if (chat.Unread > 0)
            {
                RectTransform badge = UiFactory.Child(bottom, "No vistos");
                LayoutElement size = badge.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = 50;
                size.minHeight = size.preferredHeight = 50;
                Circle(badge.gameObject, ChatBadge, 50f);
                TextMeshProUGUI count = UiFactory.Label(badge, chat.Unread > 99 ? "99+" : chat.Unread.ToString(), UiFactory.MinFontSize, Color.white, FontStyles.Bold);
                count.alignment = TextAlignmentOptions.Center;
                UiFactory.Stretch(count.rectTransform);
                NoWrap(count);
            }

            Divider(_body);
        }

        /// <summary>
        /// Una conversación abierta: arriba qué es esa persona para ti, los mensajes en burbujas con
        /// el día como separador, y abajo lo que puedes contestar si te preguntó algo. Al abrirla, ya
        /// viste todo.
        /// </summary>
        private void DrawConversation(int otherId)
        {
            LifePresenter presenter = _presenter!;
            presenter.ReadChat(otherId);
            Changed?.Invoke();
            ChatView? chat = presenter.Chat(otherId);
            if (chat == null)
            {
                DrawChats();
                return;
            }

            Prepare(home: false, title: chat.Name, back: DrawChats);
            ChatTheme(conversation: true);

            if (chat.Relation.Length > 0 && chat.Relation != chat.Name)
            {
                Pill(_body, chat.Relation);
            }

            string day = string.Empty;
            foreach (ChatBubbleView message in chat.Messages)
            {
                if (message.Day != day)
                {
                    day = message.Day;
                    Pill(_body, day);
                }

                Bubble(message);
            }

            Space(_body, 16f);
            if (chat.Replies.Count == 0)
            {
                string note = chat.ReplyAfterEvent ? presenter.Text("ui.mensajito.despues_del_evento") : presenter.Text("ui.mensajito.nada_pendiente");
                TextMeshProUGUI nothing = UiFactory.Label(_body, note, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
                nothing.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                UiFactory.Label(_body, presenter.Text("ui.mensajito.responder"), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
                for (int i = 0; i < chat.Replies.Count; i++)
                {
                    int reply = i;
                    Button button = UiFactory.MakeButton(_body, chat.Replies[i], MailWhite, ChatGreen, () =>
                    {
                        presenter.ReplyChat(otherId, reply);
                        Changed?.Invoke();
                        DrawConversation(otherId);
                    }, UiFactory.MinFontSize, 0, 18);
                    TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                    label.fontStyle = FontStyles.Normal;
                }
            }

            ScrollToBottom();
        }

        /// <summary>Una burbuja: la de la otra persona a la izquierda y blanca; la tuya a la derecha y verde.</summary>
        private void Bubble(ChatBubbleView message)
        {
            RectTransform row = UiFactory.Child(_body, message.FromPlayer ? "Mensaje tuyo" : "Mensaje");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // El hueco del otro lado: la burbuja crece con el texto hasta dejarle ese mínimo.
            if (message.FromPlayer)
            {
                Gap(row);
            }

            RectTransform bubble = UiFactory.Column(row, "Burbuja", 20, 0, message.FromPlayer ? BubbleMine : BubbleTheirs);
            bubble.GetComponent<Image>().pixelsPerUnitMultiplier = 1.4f;
            bubble.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0;
            TextMeshProUGUI text = UiFactory.Label(bubble, message.Text, 38, UiFactory.PaperInk);
            text.textWrappingMode = TextWrappingModes.Normal;

            if (!message.FromPlayer)
            {
                Gap(row);
            }
        }

        private static void Gap(RectTransform row)
        {
            LayoutElement gap = UiFactory.Child(row, "Hueco").gameObject.AddComponent<LayoutElement>();
            gap.minWidth = 150;
            gap.flexibleWidth = 1;
        }

        /// <summary>Un cartelito redondo al centro (el día, o qué es esa persona para ti).</summary>
        private static void Pill(RectTransform parent, string text)
        {
            RectTransform holder = UiFactory.Child(parent, "Cartelito");
            HorizontalLayoutGroup center = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
            center.childAlignment = TextAnchor.MiddleCenter;
            center.childControlWidth = true;
            center.childControlHeight = true;
            center.childForceExpandWidth = false;
            center.childForceExpandHeight = false;
            RectTransform pill = UiFactory.Column(holder, "Fondo del cartelito", 12, 0, UiFactory.Hex(0xE1F2FB));
            pill.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(26, 26, 8, 8);
            TextMeshProUGUI label = UiFactory.Label(pill, text, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            label.alignment = TextAlignmentOptions.Center;
            NoWrap(label);
        }

        /// <summary>Lleva el scroll al final, donde están los mensajes nuevos (como en las apps de verdad).</summary>
        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_body);
            ScrollRect? scroll = _scrollImage.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>Los colores de Mensajito: barra verde con letras blancas, la lista blanca y la conversación beige.</summary>
        private void ChatTheme(bool conversation)
        {
            Color back = conversation ? ChatWallpaper : MailWhite;
            _screenImage.color = ChatGreen;
            _scrollImage.color = back;
            _appBarImage.color = ChatGreen;
            _appTitle.color = Color.white;
            _clock.color = Color.white;
            _back.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
        }

        /// <summary>La foto de perfil: un círculo con la inicial, de un color que no cambia para cada persona.</summary>
        private static void ProfilePhoto(RectTransform parent, string name, int personId, float diameter)
        {
            Color[] colors =
            {
                UiFactory.Hex(0xE07A3F), UiFactory.Hex(0x3D7EDB), UiFactory.Hex(0x8E5BD8), UiFactory.Hex(0x1F9D74),
                UiFactory.Hex(0xD04F8F), UiFactory.Hex(0x607089), UiFactory.Hex(0xC2862E),
            };
            RectTransform face = UiFactory.Child(parent, "Foto de perfil");
            LayoutElement size = face.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = diameter;
            size.minHeight = size.preferredHeight = diameter;
            Circle(face.gameObject, colors[Math.Abs(personId) % colors.Length], diameter);
            Letter(face, name, 46);
        }

        /// <summary>
        /// Prepara la pantalla para lo que se va a dibujar: la fecha arriba, el fondo de pantalla o
        /// el de la app, la barra de la app (con Volver si hay adónde) y el dock solo en el inicio.
        /// </summary>
        private void Prepare(bool home, string title, Action? back)
        {
            LifePresenter presenter = _presenter!;
            _clock.text = presenter.Snapshot().DateLabel;
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            _screenImage.color = Wallpaper;
            _scrollImage.color = Color.clear;
            _clock.color = UiFactory.Ink;

            _appBar.gameObject.SetActive(!home);
            _appTitle.text = title;
            TextMeshProUGUI backLabel = _back.GetComponentInChildren<TextMeshProUGUI>();
            backLabel.text = presenter.Text("ui.boton.volver");
            backLabel.color = MailBlue;
            Action target = back ?? new Action(DrawHome);
            _back.onClick.RemoveAllListeners();
            _back.onClick.AddListener(() => target());
            _back.gameObject.SetActive(back != null);

            _dock.gameObject.SetActive(home);
            UiFactory.Clear(_dock);
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
        }

        /// <summary>Los colores de la app de correo: blanca, con letras oscuras.</summary>
        private void MailTheme()
        {
            _screenImage.color = MailWhite;
            _scrollImage.color = MailWhite;
            _appBarImage.color = MailWhite;
            _appTitle.color = UiFactory.PaperInk;
            _clock.color = UiFactory.PaperInk;
        }

        private static RectTransform AppRow(RectTransform parent)
        {
            RectTransform row = UiFactory.Child(parent, "Fila de apps");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return row;
        }

        /// <summary>Huecos vacíos al final de una fila, para que cada app ocupe lo mismo.</summary>
        private static void FillRow(RectTransform row, int used)
        {
            for (int i = used; i < AppsPerRow; i++)
            {
                UiFactory.Child(row, "Hueco").gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
        }

        /// <summary>
        /// El ícono de una app: un cuadrado redondeado de su color con su inicial, el nombre debajo
        /// y, si tiene algo nuevo, el globo rojo con cuántos.
        /// </summary>
        private static void Icon(RectTransform parent, string appId, string name, int unread, Action open)
        {
            RectTransform cell = UiFactory.Child(parent, "App");
            LayoutElement share = cell.gameObject.AddComponent<LayoutElement>();
            share.flexibleWidth = 1;
            // Todas las apps de una fila del mismo ancho, aunque un nombre sea más largo.
            share.minWidth = share.preferredWidth = 0;
            VerticalLayoutGroup layout = cell.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            RectTransform icon = UiFactory.Child(cell, "Icono");
            LayoutElement size = icon.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = IconSize;
            size.minHeight = size.preferredHeight = IconSize;
            Image image = UiFactory.RoundedImage(icon.gameObject, AppColor(appId));
            image.pixelsPerUnitMultiplier = 0.75f;
            Button button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => open());
            Letter(icon, name, 72);

            TextMeshProUGUI label = UiFactory.Label(cell, name, UiFactory.MinFontSize, UiFactory.Ink);
            label.alignment = TextAlignmentOptions.Center;
            NoWrap(label);

            if (unread > 0)
            {
                RectTransform badge = Corner(icon, "Globo de avisos", 56f, new Vector2(16f, 16f));
                Circle(badge.gameObject, UiFactory.Hex(0xE5483D), 56f);
                TextMeshProUGUI count = UiFactory.Label(badge, unread > 99 ? "99+" : unread.ToString(), UiFactory.MinFontSize, Color.white, FontStyles.Bold);
                count.alignment = TextAlignmentOptions.Center;
                UiFactory.Stretch(count.rectTransform);
                NoWrap(count);
            }
        }

        /// <summary>La inicial grande y blanca al centro de un ícono.</summary>
        private static void Letter(RectTransform icon, string name, int size)
        {
            string initial = name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "?";
            TextMeshProUGUI letter = UiFactory.Label(icon, initial, size, Color.white, FontStyles.Bold, Face.Title);
            letter.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(letter.rectTransform);
            letter.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        /// <summary>El círculo con la inicial de quien manda el correo, del color de su tema.</summary>
        private static void Avatar(RectTransform parent, string sender, string topic, float diameter)
        {
            RectTransform avatar = UiFactory.Child(parent, "Inicial de quien manda");
            LayoutElement size = avatar.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = diameter;
            size.minHeight = size.preferredHeight = diameter;
            Circle(avatar.gameObject, TopicColor(topic), diameter);
            Letter(avatar, InitialOf(sender), 44);
        }

        /// <summary>La primera letra que importa ("Tu banco" es "B", no "T").</summary>
        private static string InitialOf(string sender)
        {
            string[] words = sender.Split(' ');
            foreach (string word in words)
            {
                if (word.Length > 2 && char.IsLetter(word[0]))
                {
                    return word;
                }
            }

            return sender;
        }

        /// <summary>Un círculo de verdad con el sprite redondeado (las esquinas crecen hasta la mitad).</summary>
        private static void Circle(GameObject target, Color color, float diameter)
        {
            Image image = UiFactory.RoundedImage(target, color);
            image.pixelsPerUnitMultiplier = (RoundedCorner * 2f) / diameter;
        }

        /// <summary>Un cuadrito pegado a la esquina de arriba a la derecha, fuera del orden de su padre.</summary>
        private static RectTransform Corner(Transform parent, string name, float size, Vector2 offset)
        {
            RectTransform rect = UiFactory.Child(parent, name);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = offset;
            return rect;
        }

        private static void Divider(RectTransform parent)
        {
            RectTransform line = UiFactory.Child(parent, "Linea");
            line.gameObject.AddComponent<Image>().color = MailLine;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;
        }

        private static void Space(RectTransform parent, float height)
        {
            UiFactory.Child(parent, "Espacio").gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static void NoWrap(TextMeshProUGUI label)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>El color de cada app parodia (solo cómo se ve; el amarillo queda para lo tuyo).</summary>
        private static Color AppColor(string appId) => appId switch
        {
            "app.correo" => UiFactory.Hex(0x1A73E8),
            "app.mensajito" => UiFactory.Hex(0x25A35A),
            "app.instante" => UiFactory.Hex(0xD6336C),
            "app.flechazo" => UiFactory.Hex(0xF2545B),
            "app.mitubo" => UiFactory.Hex(0xD92B2B),
            "app.sonar" => UiFactory.Hex(0x1E9E7A),
            "app.banco" => UiFactory.Hex(0x0F5E73),
            "app.mercado" => UiFactory.Hex(0x6C4AB6),
            _ => UiFactory.Hex(0x55607A),
        };

        /// <summary>
        /// El color del círculo de quien manda, según el tema del correo. La estafa usa el mismo
        /// color del banco: en la vida real el correo falso se ve igual al de verdad.
        /// </summary>
        private static Color TopicColor(string topic) => topic switch
        {
            "trabajo" => UiFactory.Hex(0x3D7EDB),
            "estudio" => UiFactory.Hex(0x8E5BD8),
            "banco" => UiFactory.Hex(0x1F9D74),
            "estafa" => UiFactory.Hex(0x1F9D74),
            "estado" => UiFactory.Hex(0x607089),
            "juzgado" => UiFactory.Hex(0x8A5A3C),
            "personal" => UiFactory.Hex(0xE07A3F),
            "publicidad" => UiFactory.Hex(0xD04F8F),
            _ => UiFactory.Hex(0x55607A),
        };
    }
}
