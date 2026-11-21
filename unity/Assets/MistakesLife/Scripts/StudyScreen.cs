#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Estudiar de verdad (D5a, bocetos D5aRamosA, D5aTexto, D5aPregunta, D5aError,
    /// D5aConCompanera y D5aFin). Con el tiempo parado, en Estudios: tus ramos en filas, cada uno
    /// con lo que se pasa ahora, su barra de dominio, tu promedio y la prueba que viene. Al tocar
    /// uno se lee el texto breve de la unidad y se elige con quién estudiar (sin nadie, alguien de
    /// tu curso o tu profe jefe); después vienen tres preguntas reales del programa de tu curso,
    /// que suenan al responder, y al final el resultado de la tarde. Todo lo que dice lo da el
    /// núcleo (<see cref="LifePresenter.StudySubjects"/> y los demás); aquí solo se dibuja.
    /// </summary>
    public sealed class StudyScreen
    {
        private static readonly Color BarGood = UiFactory.Hex(0x3BAE7A);
        private static readonly Color BarMiddle = UiFactory.Hex(0xE0A93B);
        private static readonly Color BarLow = UiFactory.Hex(0xD9483B);
        private static readonly Color BarBackPaper = UiFactory.Hex(0xDCD3C2);
        private static readonly Color TestChip = UiFactory.Hex(0xE6ECF7);
        private static readonly Color TestInk = UiFactory.Hex(0x24407A);
        private static readonly Color MakeupChip = UiFactory.Hex(0xFBE9E6);
        private static readonly Color MakeupInk = UiFactory.Hex(0x8A2A20);
        private static readonly Color Reading = UiFactory.Hex(0xFBF8F1);
        private static readonly Color OptionLine = UiFactory.Hex(0xD6CDBC);
        private static readonly Color RightTint = UiFactory.Hex(0xDCEFE3);
        private static readonly Color RightInk = UiFactory.Hex(0x1D6B4F);
        private static readonly Color WrongTint = UiFactory.Hex(0xF8E1DC);
        private static readonly Color WrongInk = UiFactory.Hex(0x8A2A20);
        private static readonly Color Selected = UiFactory.Accent;

        private readonly Transform _parent;
        private RectTransform? _layer;
        private TextMeshProUGUI _title = null!;
        private TextMeshProUGUI _caption = null!;
        private RectTransform _body = null!;
        private RectTransform _footer = null!;
        private LifePresenter _presenter = null!;
        private System.Action? _openReport;
        private System.Action? _openOthers;

        /// <summary>La última prueba por la que sonó la campanita (suena una sola vez por prueba).</summary>
        private static string? _bellFor;

        public StudyScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama después de estudiar, para que la pantalla principal muestre las barras y las tardes nuevas.</summary>
        public System.Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>
        /// Abre tus ramos. <paramref name="openReport"/> abre tu libreta, tu curso y tus notas;
        /// <paramref name="openOthers"/>, lo demás que puedes hacer en Estudios (null si no hay nada más).
        /// </summary>
        public void Show(LifePresenter presenter, System.Action openReport, System.Action? openOthers)
        {
            _presenter = presenter;
            _openReport = openReport;
            _openOthers = openOthers;
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            DrawList();
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

            _layer = UiFactory.Overlay(_parent, "Estudiar", UiFactory.Background);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            RectTransform band = UiFactory.Child(_layer, "Franja");
            band.gameObject.AddComponent<Image>().color = StudiesScreen.SchoolBand;
            VerticalLayoutGroup bandLayout = band.gameObject.AddComponent<VerticalLayoutGroup>();
            bandLayout.padding = new RectOffset(56, 56, 48, 40);
            bandLayout.spacing = 6;
            bandLayout.childControlWidth = true;
            bandLayout.childControlHeight = true;
            bandLayout.childForceExpandWidth = true;
            bandLayout.childForceExpandHeight = false;
            _title = UiFactory.Label(band, string.Empty, 64, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _caption = UiFactory.Label(band, string.Empty, 36, UiFactory.MutedInk, FontStyles.Bold);

            _body = UiFactory.ScrollColumn(_layer, "Ramos", 40, 26);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = Color.clear;

            _footer = UiFactory.Column(_layer, "Abajo", 0, 16);
            _footer.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 8, 40);
        }

        private void Begin(string title, string caption)
        {
            _title.text = title;
            _caption.text = caption;
            _caption.gameObject.SetActive(caption.Length > 0);
            UiFactory.Clear(_body);
            UiFactory.Clear(_footer);
            _body.anchoredPosition = Vector2.zero;
        }

        // ---------- Tus ramos (boceto D5aRamosA) ----------

        private void DrawList()
        {
            LifePresenter presenter = _presenter;
            HeaderView header = presenter.Header();
            Begin(presenter.Text("ui.grupo.estudios"), header.Role);
            FreeTimeView? free = presenter.FreeTime();
            if (free != null)
            {
                ActionsScreen.Counter(_body, free);
            }

            Section(presenter.Text("ui.estudio.titulo_lista"));
            IReadOnlyList<StudySubjectView> subjects = presenter.StudySubjects();
            string? bell = null;
            foreach (StudySubjectView subject in subjects)
            {
                SubjectRow(subject);
                if (subject.Upcoming != null)
                {
                    bell = subject.SubjectId + subject.Upcoming;
                }
            }

            UiFactory.Label(_body, presenter.Text("ui.estudio.explica_barra"), UiFactory.MinFontSize, UiFactory.MutedInk);
            if (bell != null && bell != _bellFor)
            {
                _bellFor = bell;
                Sounds.Play(Sound.SchoolBell);
            }

            if (_openOthers != null)
            {
                UiFactory.MakeButton(_footer, presenter.Text("ui.estudio.otras"), UiFactory.Option, UiFactory.Ink, () =>
                {
                    Sounds.Play(Sound.PageTurn);
                    Close();
                    _openOthers?.Invoke();
                });
            }

            UiFactory.MakeButton(_footer, presenter.Text("ui.estudio.ver_libreta"), UiFactory.Option, UiFactory.Ink, () =>
            {
                Sounds.Play(Sound.PageTurn);
                Close();
                _openReport?.Invoke();
            });
            Button close = UiFactory.MakeButton(_footer, presenter.Text("ui.boton.cerrar"), UiFactory.Accent, UiFactory.AccentText, () =>
            {
                Sounds.Play(Sound.WoodKnock);
                Close();
            });
            close.GetComponent<LayoutElement>().minHeight = 156;
        }

        /// <summary>Un ramo: nombre y promedio, lo que se pasa ahora, la barra con su palabra y la prueba que viene.</summary>
        private void SubjectRow(StudySubjectView subject)
        {
            RectTransform card = UiFactory.Column(_body, subject.Name, 0, 12, UiFactory.Paper);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 28, 30);
            CanvasGroup fade = card.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = subject.CanStudy ? 1f : 0.5f;

            RectTransform head = Row(card, "Nombre y promedio", 20);
            TextMeshProUGUI name = UiFactory.Label(head, subject.Name, 46, UiFactory.PaperInk, FontStyles.Bold);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            if (subject.Average != null)
            {
                TextMeshProUGUI average = UiFactory.Label(head, subject.Average, 36, UiFactory.PaperMutedInk, FontStyles.Bold);
                average.alignment = TextAlignmentOptions.TopRight;
                average.textWrappingMode = TextWrappingModes.NoWrap;
            }

            UiFactory.Label(card, subject.Topic, 36, UiFactory.PaperMutedInk);
            MasteryBar(card, subject.Mastery, subject.Level, subject.MasteryLabel, BarBackPaper, UiFactory.PaperInk, 26);
            if (subject.Upcoming != null)
            {
                Tag(card, subject.Upcoming, subject.Makeup ? MakeupChip : TestChip, subject.Makeup ? MakeupInk : TestInk);
            }

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = subject.CanStudy;
            string id = subject.SubjectId;
            button.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PageTurn);
                DrawIntro(id);
            });
        }

        // ---------- Antes de estudiar (boceto D5aTexto) ----------

        private void DrawIntro(string subjectId)
        {
            LifePresenter presenter = _presenter;
            StudyIntroView intro = presenter.StudyIntro(subjectId);
            Begin(intro.Name, intro.Caption);
            MasteryBar(_body, intro.Mastery, intro.Level, intro.Mastery + "%", UiFactory.BarBack, UiFactory.Ink, 30, presenter.Text("ui.estudio.dominio_corto"));

            RectTransform card = UiFactory.Column(_body, "Lo que vas a estudiar", 0, 18, Reading);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 36, 40);
            UiFactory.Label(card, presenter.Text("ui.estudio.lo_que_vas").ToUpperInvariant(), UiFactory.MinFontSize, TestInk, FontStyles.Bold)
                .characterSpacing = 2f;
            foreach (string paragraph in intro.Reading.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                TextMeshProUGUI text = UiFactory.Label(card, paragraph, 40, UiFactory.PaperInk);
                text.lineSpacing = 8f;
            }

            Section(presenter.Text("ui.estudio.con_quien"));
            int chosen = 0;
            var tiles = new List<Image>();
            RectTransform row = Row(_body, "Con quien", 16);
            for (int i = 0; i < intro.Partners.Count; i++)
            {
                StudyPartnerView partner = intro.Partners[i];
                int index = i;
                tiles.Add(PartnerTile(row, partner, i == 0, () =>
                {
                    chosen = index;
                    for (int t = 0; t < tiles.Count; t++)
                    {
                        tiles[t].color = t == index ? Selected : UiFactory.Option;
                    }

                    Sounds.Play(Sound.PhotoClick);
                }));
            }

            UiFactory.Label(_body, intro.Note, UiFactory.MinFontSize, UiFactory.MutedInk);
            UiFactory.MakeButton(_footer, presenter.Text("ui.estudio.empezar"), UiFactory.Accent, UiFactory.AccentText, () =>
            {
                StudyPartnerView partner = intro.Partners[chosen];
                StudyQuestionView question = presenter.StartStudy(subjectId, partner.PersonId, partner.Teacher);
                Sounds.Play(Sound.WoodKnock);
                Changed?.Invoke();
                DrawQuestion(intro.Name, question);
            }).GetComponent<LayoutElement>().minHeight = 156;
            UiFactory.MakeButton(_footer, presenter.Text("ui.boton.volver"), UiFactory.Option, UiFactory.Ink, () =>
            {
                Sounds.Play(Sound.PageTurn);
                DrawList();
            });
        }

        /// <summary>Una persona con quien estudiar: su círculo con iniciales, su nombre y en una línea quién es. Se marca en amarillo la elegida.</summary>
        private static Image PartnerTile(RectTransform row, StudyPartnerView partner, bool selected, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform tile = UiFactory.Column(row, partner.Name, 0, 8);
            Image frame = UiFactory.RoundedImage(tile.gameObject, selected ? Selected : UiFactory.Option);
            VerticalLayoutGroup layout = tile.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            tile.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            RectTransform inner = UiFactory.Column(tile, "Dentro", 0, 6, UiFactory.Card);
            VerticalLayoutGroup innerLayout = inner.GetComponent<VerticalLayoutGroup>();
            innerLayout.padding = new RectOffset(14, 14, 20, 20);
            innerLayout.childAlignment = TextAnchor.UpperCenter;
            TextMeshProUGUI initials = UiFactory.Label(inner, partner.Initials, 40, UiFactory.Accent, FontStyles.Bold);
            initials.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI name = UiFactory.Label(inner, partner.Name, 36, UiFactory.Ink, FontStyles.Bold);
            name.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI caption = UiFactory.Label(inner, partner.Caption, UiFactory.MinFontSize, UiFactory.MutedInk);
            caption.alignment = TextAlignmentOptions.Center;
            Button button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(onClick);
            return frame;
        }

        // ---------- Las preguntas (bocetos D5aPregunta, D5aError y D5aConCompanera) ----------

        private void DrawQuestion(string subjectName, StudyQuestionView question)
        {
            LifePresenter presenter = _presenter;
            Begin(subjectName, question.Caption);
            Steps(question.Number - 1, question.Total);
            MasteryBar(_body, question.Mastery, question.Level, question.Mastery + "%", UiFactory.BarBack, UiFactory.Ink, 30, presenter.Text("ui.estudio.dominio_corto"));
            UiFactory.Label(_body, question.Text, 54, UiFactory.Ink, FontStyles.Normal, Face.Title).lineSpacing = 6f;

            var buttons = new List<Button>();
            for (int i = 0; i < question.Options.Count; i++)
            {
                int position = i;
                buttons.Add(OptionButton(_body, "ABCD".Substring(i, 1), question.Options[i], () =>
                {
                    foreach (Button b in buttons)
                    {
                        b.interactable = false;
                    }

                    StudyAnswerView answer = presenter.AnswerStudy(position);
                    Sounds.Play(answer.Correct ? Sound.Correct : Sound.RedMark);
                    ShowAnswer(subjectName, question, answer, buttons);
                    Changed?.Invoke();
                }));
            }
        }

        /// <summary>Una alternativa: su letra en un círculo y el texto, grande y fácil de tocar.</summary>
        private static Button OptionButton(RectTransform parent, string letter, string text, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = UiFactory.Child(parent, "Alternativa " + letter);
            Image image = UiFactory.RoundedImage(rect.gameObject, Reading);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 26, 26);
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            rect.gameObject.AddComponent<LayoutElement>().minHeight = UiFactory.MinButtonHeight;
            TextMeshProUGUI letterLabel = UiFactory.Label(rect, letter, 38, UiFactory.PaperMutedInk, FontStyles.Bold);
            letterLabel.gameObject.AddComponent<LayoutElement>().minWidth = 44;
            TextMeshProUGUI label = UiFactory.Label(rect, text, 44, UiFactory.PaperInk, FontStyles.Bold);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return button;
        }

        private void ShowAnswer(string subjectName, StudyQuestionView question, StudyAnswerView answer, List<Button> buttons)
        {
            LifePresenter presenter = _presenter;
            for (int i = 0; i < buttons.Count; i++)
            {
                Image image = (Image)buttons[i].targetGraphic;
                if (i == answer.CorrectPosition)
                {
                    image.color = RightTint;
                }
                else if (i == answer.Chosen)
                {
                    image.color = WrongTint;
                }
            }

            RectTransform box = UiFactory.Column(_body, "Por que", 0, 10, answer.Correct ? RightTint : WrongTint);
            box.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 28, 30);
            UiFactory.Label(box, answer.Verdict, 42, answer.Correct ? RightInk : WrongInk, FontStyles.Bold);
            UiFactory.Label(box, answer.Explanation, 40, answer.Correct ? RightInk : WrongInk);
            if (answer.PartnerLine != null)
            {
                RectTransform partner = UiFactory.Column(_body, "Tu compañera", 0, 6, UiFactory.Paper);
                partner.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(36, 36, 24, 26);
                UiFactory.Label(partner, answer.PartnerLine, 40, UiFactory.PaperInk);
            }

            if (!answer.Correct)
            {
                UiFactory.Label(_body, presenter.Text("ui.estudio.error_no_baja"), UiFactory.MinFontSize, UiFactory.MutedInk);
            }

            UiFactory.Clear(_footer);
            string next = presenter.Text(answer.HasNext ? "ui.estudio.siguiente" : "ui.estudio.terminar");
            UiFactory.MakeButton(_footer, next, UiFactory.Accent, UiFactory.AccentText, () =>
            {
                if (answer.HasNext && presenter.StudyQuestion() is StudyQuestionView following)
                {
                    Sounds.Play(Sound.PageTurn);
                    DrawQuestion(subjectName, following);
                    return;
                }

                StudyResultView result = presenter.FinishStudy();
                Sounds.Play(result.MasteryAfter > result.MasteryBefore ? Sound.Approved : Sound.PageTurn);
                Changed?.Invoke();
                DrawResult(result);
            }).GetComponent<LayoutElement>().minHeight = 156;
        }

        /// <summary>Tres rayitas arriba: verde lo acertado, rojo lo fallado, amarillo la que va.</summary>
        private void Steps(int current, int total)
        {
            RectTransform row = Row(_body, "Avance", 12);
            for (int i = 0; i < total; i++)
            {
                RectTransform step = UiFactory.Child(row, "Paso");
                UiFactory.RoundedImage(step.gameObject, i == current ? UiFactory.Accent : i < current ? UiFactory.MutedInk : UiFactory.BarBack);
                LayoutElement size = step.gameObject.AddComponent<LayoutElement>();
                size.flexibleWidth = 1;
                size.minHeight = size.preferredHeight = 14;
            }
        }

        // ---------- El resultado (boceto D5aFin) ----------

        private void DrawResult(StudyResultView result)
        {
            LifePresenter presenter = _presenter;
            Begin(result.Title, string.Empty);
            RectTransform card = UiFactory.Column(_body, "Resultado", 0, 16, Reading);
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 36, 40);
            UiFactory.Label(card, result.Score, 44, UiFactory.PaperInk, FontStyles.Bold);
            MasteryBar(card, result.MasteryAfter, result.Level, result.MasteryAfter + "%", BarBackPaper, UiFactory.PaperInk, 34);
            UiFactory.Label(card, result.MasteryText, 38, result.MasteryAfter > result.MasteryBefore ? RightInk : UiFactory.PaperMutedInk, FontStyles.Bold);
            if (result.TestTitle != null)
            {
                UiFactory.Label(card, result.TestTitle, 36, UiFactory.PaperMutedInk, FontStyles.Bold);
                UiFactory.Label(card, result.TestText ?? string.Empty, 40, UiFactory.PaperInk);
            }

            ActionsScreen.Chips(_body, result.Changes);
            UiFactory.Label(_body, result.Note, UiFactory.MinFontSize, UiFactory.MutedInk);
            ActionsScreen.Counter(_body, result.FreeTime);

            UiFactory.MakeButton(_footer, presenter.Text("ui.estudio.listo"), UiFactory.Accent, UiFactory.AccentText, () =>
            {
                Sounds.Play(Sound.WoodKnock);
                DrawList();
            }).GetComponent<LayoutElement>().minHeight = 156;
        }

        // ---------- Piezas ----------

        private void Section(string text)
        {
            UiFactory.Label(_body, text.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.MutedInk, FontStyles.Bold).characterSpacing = 2f;
        }

        /// <summary>La barra de dominio: (su nombre), la barra verde, amarilla o roja, y el porcentaje con su palabra.</summary>
        private static void MasteryBar(RectTransform parent, int value, LifeBarLevel level, string label, Color back, Color ink, int height, string? title = null)
        {
            RectTransform row = Row(parent, "Dominio", 18);
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            if (title != null)
            {
                UiFactory.Label(row, title, 36, ink, FontStyles.Bold);
            }

            RectTransform bar = UiFactory.Child(row, "Barra");
            UiFactory.RoundedImage(bar.gameObject, back);
            LayoutElement size = bar.gameObject.AddComponent<LayoutElement>();
            size.flexibleWidth = 1;
            size.minHeight = size.preferredHeight = height;
            RectTransform fill = UiFactory.Child(bar, "Relleno");
            UiFactory.RoundedImage(fill.gameObject, level == LifeBarLevel.Good ? BarGood : level == LifeBarLevel.Middle ? BarMiddle : BarLow);
            UiFactory.Stretch(fill);
            fill.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            fill.gameObject.SetActive(value > 0);

            TextMeshProUGUI percent = UiFactory.Label(row, label, 36, ink, FontStyles.Bold);
            percent.alignment = TextAlignmentOptions.Right;
            percent.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>Una etiqueta chica: "Prueba el jueves 14" (azul) o "Recuperativa el viernes 22" (roja).</summary>
        private static void Tag(RectTransform parent, string text, Color background, Color ink)
        {
            RectTransform holder = Row(parent, "Etiqueta", 0);
            RectTransform chip = UiFactory.Child(holder, text);
            UiFactory.RoundedImage(chip.gameObject, background);
            HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            TextMeshProUGUI label = UiFactory.Label(chip, text, UiFactory.MinFontSize, ink, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static RectTransform Row(RectTransform parent, string name, int spacing)
        {
            RectTransform row = UiFactory.Child(parent, name);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }
    }
}
