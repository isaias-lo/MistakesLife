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
    /// El celular del personaje (docs/diseno-app.md, "Redes"): se abre encima de la vida, con
    /// el material del celular, sus apps parodia y la bandeja del correo. Tocar el correo abre
    /// la bandeja; tocar un correo lo abre y queda leído. Las otras apps todavía no tienen
    /// nada adentro. Regla: aquí no se decide nada; se dibuja lo que da LifePresenter
    /// (Phone, Mail) y se le pasa el toque de leer (ReadMail).
    /// </summary>
    public sealed class PhoneScreen
    {
        /// <summary>El material del celular (docs/diseno-app.md, colores de cada tema).</summary>
        public static readonly Color PhoneBody = UiFactory.Hex(0x0B0F1A);

        /// <summary>Cuántas apps van por fila.</summary>
        private const int AppsPerRow = 3;

        private readonly Transform _parent;
        private RectTransform? _layer;
        private RectTransform _body = null!;
        private TextMeshProUGUI _title = null!;
        private Button _back = null!;
        private TextMeshProUGUI _closeLabel = null!;
        private LifePresenter? _presenter;

        public PhoneScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama cuando se lee un correo, para que la vida actualice el punto rojo.</summary>
        public Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>Abre el celular en la pantalla de inicio (las apps), o directo en la bandeja del correo.</summary>
        public void Show(LifePresenter presenter, bool mail = false)
        {
            _presenter = presenter;
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            if (mail)
            {
                DrawInbox();
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

        /// <summary>Arma una sola vez la capa: fondo oscuro, el celular al centro con su barra y su cuerpo.</summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Celular", UiFactory.Shade);
            RectTransform phone = UiFactory.Column(_layer, "Cuerpo del celular", 36, 20, PhoneBody);
            phone.anchorMin = new Vector2(0.06f, 0.06f);
            phone.anchorMax = new Vector2(0.94f, 0.94f);
            phone.offsetMin = Vector2.zero;
            phone.offsetMax = Vector2.zero;

            RectTransform header = UiFactory.Child(phone, "Barra");
            HorizontalLayoutGroup row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            _back = UiFactory.MakeButton(header, string.Empty, UiFactory.Option, UiFactory.Ink, DrawHome, UiFactory.MinFontSize, 0, 12);
            Width(_back, 180);
            _title = UiFactory.Label(header, string.Empty, 48, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Button close = UiFactory.MakeButton(header, string.Empty, UiFactory.Option, UiFactory.Ink, Close, UiFactory.MinFontSize, 0, 12);
            Width(close, 180);
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();

            _body = UiFactory.ScrollColumn(phone, "Pantalla", 24, 18);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = PhoneBody;
        }

        /// <summary>La pantalla de inicio: las apps en filas, con un punto rojo en el correo si hay sin leer.</summary>
        private void DrawHome()
        {
            LifePresenter presenter = _presenter!;
            PhoneView phone = presenter.Phone();
            Header(presenter.Text("ui.celular.titulo"), showBack: false);

            if (!phone.HasPhone)
            {
                UiFactory.Label(_body, presenter.Text(phone.HasMail ? "ui.celular.solo_correo" : "ui.celular.sin_nada"), 38, UiFactory.Ink);
                if (phone.HasMail)
                {
                    AppButton(_body, presenter.Text("app.correo"), phone.UnreadMail, DrawInbox);
                }

                return;
            }

            RectTransform? line = null;
            for (int i = 0; i < phone.Apps.Count; i++)
            {
                if (i % AppsPerRow == 0)
                {
                    line = AppRow(_body);
                }

                AppView app = phone.Apps[i];
                Action open = app.IsMail ? (Action)DrawInbox : () => DrawEmptyApp(app);
                AppButton(line!, app.Name, app.Unread, () => open());
            }

            // Huecos vacíos al final, para que la última fila tenga el mismo ancho por app.
            for (int i = phone.Apps.Count; line != null && i % AppsPerRow != 0; i++)
            {
                UiFactory.Child(line, "Hueco").gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
        }

        /// <summary>Una app que todavía no tiene nada adentro.</summary>
        private void DrawEmptyApp(AppView app)
        {
            Header(app.Name, showBack: true);
            UiFactory.Label(_body, _presenter!.Text("ui.celular.app_vacia"), 38, UiFactory.MutedInk);
        }

        /// <summary>La bandeja: cada correo con quién lo manda, el asunto y cuándo llegó; los sin leer, en negrita y con punto rojo.</summary>
        private void DrawInbox()
        {
            LifePresenter presenter = _presenter!;
            PhoneView phone = presenter.Phone();
            Header(presenter.Text("ui.correo.titulo"), showBack: phone.HasPhone);
            IReadOnlyList<MailView> mail = presenter.Mail();
            if (mail.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.correo.vacio"), 38, UiFactory.MutedInk);
                return;
            }

            if (phone.UnreadMail > 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.correo.sin_leer", phone.UnreadMail), UiFactory.MinFontSize, UiFactory.Bad);
            }

            foreach (MailView message in mail)
            {
                MailView chosen = message;
                RectTransform item = UiFactory.Column(_body, "Correo", 24, 6, UiFactory.Card);
                Button open = item.gameObject.AddComponent<Button>();
                open.targetGraphic = item.GetComponent<Image>();
                open.onClick.AddListener(() => DrawMessage(chosen));
                FontStyles weight = message.Read ? FontStyles.Normal : FontStyles.Bold;
                UiFactory.Label(item, message.Sender, UiFactory.MinFontSize, message.Read ? UiFactory.MutedInk : UiFactory.Ink, weight);
                UiFactory.Label(item, message.Subject, 36, UiFactory.Ink, weight);
                UiFactory.Label(item, message.AgeLabel, UiFactory.MinFontSize, UiFactory.MutedInk);
                if (!message.Read)
                {
                    RedDot(item);
                }
            }
        }

        /// <summary>Un correo abierto, en papel; al abrirlo queda leído.</summary>
        private void DrawMessage(MailView message)
        {
            LifePresenter presenter = _presenter!;
            if (!message.Read)
            {
                presenter.ReadMail(message.Index);
                Changed?.Invoke();
            }

            Header(presenter.Text("ui.correo.titulo"), showBack: true, back: DrawInbox);
            RectTransform paper = UiFactory.Column(_body, "Hoja del correo", 32, 12, UiFactory.Paper);
            UiFactory.Label(paper, message.Sender, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            UiFactory.Label(paper, message.Subject, 42, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(paper, $"{message.DateLabel} · {message.AgeLabel}", UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Normal, Face.Mono);
            UiFactory.Label(paper, message.Body, 36, UiFactory.PaperInk);
        }

        /// <summary>Pone el título, muestra o esconde "Volver" y vacía la pantalla del celular.</summary>
        private void Header(string title, bool showBack, Action? back = null)
        {
            LifePresenter presenter = _presenter!;
            _title.text = title;
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            _back.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.boton.volver");
            _back.gameObject.SetActive(showBack);
            _back.onClick.RemoveAllListeners();
            Action target = back ?? new Action(DrawHome);
            _back.onClick.AddListener(() => target());
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
        }

        private static RectTransform AppRow(RectTransform parent)
        {
            RectTransform row = UiFactory.Child(parent, "Fila de apps");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return row;
        }

        /// <summary>El ícono de una app: un botón cuadrado con su nombre y, si hay algo nuevo, cuántas.</summary>
        private static void AppButton(RectTransform parent, string name, int unread, Action open)
        {
            string label = unread > 0 ? $"{name}\n({unread})" : name;
            Button button = UiFactory.MakeButton(parent, label, UiFactory.Card, UiFactory.Ink, () => open(), UiFactory.MinFontSize, 200, 16);
            LayoutElement size = button.GetComponent<LayoutElement>();
            size.flexibleWidth = 1;
            size.preferredWidth = 0;
            if (unread > 0)
            {
                RedDot(button.transform);
            }
        }

        /// <summary>El punto rojo de "algo nuevo" (docs/diseno-app.md, "tus cosas"), en la esquina de un botón.</summary>
        public static void RedDot(Transform button)
        {
            RectTransform dot = UiFactory.Child(button, "Punto rojo");
            dot.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiFactory.RoundedImage(dot.gameObject, UiFactory.Bad);
            dot.anchorMin = dot.anchorMax = new Vector2(1f, 1f);
            dot.pivot = new Vector2(1f, 1f);
            dot.sizeDelta = new Vector2(28, 28);
            dot.anchoredPosition = new Vector2(-10, -10);
        }

        private static void Width(Button button, float width)
        {
            LayoutElement element = button.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
        }
    }
}
