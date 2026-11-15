#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tus estudios (docs/diseno-app.md, versión 4): la libreta de notas, tu curso (o tu sección en
    /// la U) y, si estudias una carrera, tu malla. Arriba una franja de color con el título y las
    /// pestañas; abajo, en tarjetas de papel derechas, lo que da el núcleo, tal cual:
    /// <list type="bullet">
    /// <item>Notas: cada ramo con su nota final en una ficha (roja bajo la nota de aprobar).</item>
    /// <item>Curso: cada compañero con su cara, su nombre, qué son y lo último que vivieron juntos.</item>
    /// <item>Malla: los semestres aprobados en verde, el que cursas con borde amarillo, el que repites en rojo.</item>
    /// </list>
    /// Cada acción suena (<see cref="Sounds"/>). Regla: aquí no se decide nada.
    /// </summary>
    public sealed class StudiesScreen
    {
        private enum Tab
        {
            Notes,
            Class,
            Plan,
        }

        /// <summary>Las franjas de cada pantalla: azul libreta (colegio), azul U y el del curso.</summary>
        public static readonly Color SchoolBand = UiFactory.Hex(0x24407A);

        private static readonly Color UniversityBand = UiFactory.Hex(0x2B4E73);
        private static readonly Color ClassBand = UiFactory.Hex(0x3E5C8A);
        private static readonly Color MarkChip = UiFactory.Hex(0xE6DECF);
        private static readonly Color Red = UiFactory.Hex(0xD9483B);
        private static readonly Color RedTint = UiFactory.Hex(0xF8E1DC);
        private static readonly Color RedInk = UiFactory.Hex(0x8A2A20);
        private static readonly Color Green = UiFactory.Hex(0x4FA36B);
        private static readonly Color GreenTint = UiFactory.Hex(0xDCEFE3);
        private static readonly Color Rule = UiFactory.Hex(0xE2D9C8);
        private static readonly Color Gone = UiFactory.Hex(0xB7B0A2);
        private static readonly Color TabIdle = new Color(1f, 1f, 1f, 0.14f);

        /// <summary>Colores de las caras (tonos apagados, uno por persona según su número).</summary>
        private static readonly Color[] Faces =
        {
            UiFactory.Hex(0x7E9DBA), UiFactory.Hex(0xC9906C), UiFactory.Hex(0x8FAF8A),
            UiFactory.Hex(0xB58DB0), UiFactory.Hex(0xD1B36A), UiFactory.Hex(0x6F8F9E),
        };

        private readonly Transform _parent;
        private RectTransform? _layer;
        private Image _band = null!;
        private TextMeshProUGUI _title = null!;
        private TextMeshProUGUI _subtitle = null!;
        private RectTransform _tabs = null!;
        private RectTransform _body = null!;
        private TextMeshProUGUI _closeLabel = null!;
        private LifePresenter? _presenter;
        private Tab _tab;

        public StudiesScreen(Transform parent)
        {
            _parent = parent;
        }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>Si hay algo que mostrar: notas, un curso o una carrera.</summary>
        public static bool HasStudies(LifePresenter presenter) =>
            presenter.ReportCard() != null || presenter.SchoolClass() != null || presenter.StudyPlan() != null;

        public void Show(LifePresenter presenter)
        {
            Show(presenter, presenter.ReportCard() != null ? 0 : presenter.SchoolClass() != null ? 1 : 2);
        }

        /// <summary>Abre en una hoja: 0 las notas, 1 el curso o la sección, 2 la malla.</summary>
        public void Show(LifePresenter presenter, int page)
        {
            Ensure();
            _presenter = presenter;
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            _tab = page <= 0 ? Tab.Notes : page == 1 ? Tab.Class : Tab.Plan;
            Draw();
            Sounds.Play(Sound.PageTurn);
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Arma una sola vez: fondo noche, la franja con título y pestañas, la lista con scroll y el botón amarillo.</summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Estudios", UiFactory.Background);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            RectTransform band = UiFactory.Child(_layer, "Franja");
            _band = band.gameObject.AddComponent<Image>();
            VerticalLayoutGroup bandLayout = band.gameObject.AddComponent<VerticalLayoutGroup>();
            bandLayout.padding = new RectOffset(56, 56, 56, 40);
            bandLayout.spacing = 12;
            bandLayout.childControlWidth = true;
            bandLayout.childControlHeight = true;
            bandLayout.childForceExpandWidth = true;
            bandLayout.childForceExpandHeight = false;
            _title = UiFactory.Label(band, string.Empty, 64, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _subtitle = UiFactory.Label(band, string.Empty, 38, UiFactory.MutedInk);
            _tabs = UiFactory.Child(band, "Pestanas");
            HorizontalLayoutGroup tabs = _tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabs.spacing = 16;
            tabs.padding = new RectOffset(0, 0, 20, 0);
            tabs.childControlWidth = true;
            tabs.childControlHeight = true;
            tabs.childForceExpandWidth = false;
            tabs.childForceExpandHeight = false;

            _body = UiFactory.ScrollColumn(_layer, "Hojas", 40, 28);
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

        private void Draw()
        {
            LifePresenter presenter = _presenter!;
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            DrawTabs(presenter);
            switch (_tab)
            {
                case Tab.Notes:
                    DrawNotes(presenter, presenter.ReportCard());
                    break;
                case Tab.Class:
                    DrawClass(presenter, presenter.SchoolClass());
                    break;
                default:
                    DrawPlan(presenter, presenter.StudyPlan());
                    break;
            }
        }

        /// <summary>Las pestañas que tienen algo: notas, curso (o sección) y malla.</summary>
        private void DrawTabs(LifePresenter presenter)
        {
            UiFactory.Clear(_tabs);
            SchoolClassView? cls = presenter.SchoolClass();
            bool university = presenter.StudyPlan() != null;
            if (presenter.ReportCard() != null)
            {
                TabButton(presenter.Text("ui.estudios.pestana.notas"), Tab.Notes);
            }

            if (cls != null)
            {
                TabButton(presenter.Text(cls.Section ? "ui.estudios.pestana.seccion" : "ui.estudios.pestana.curso"), Tab.Class);
            }

            if (university)
            {
                TabButton(presenter.Text("ui.estudios.pestana.malla"), Tab.Plan);
            }

            _band.color = _tab == Tab.Class ? ClassBand : university ? UniversityBand : SchoolBand;
        }

        private void TabButton(string text, Tab tab)
        {
            bool selected = _tab == tab;
            Button button = UiFactory.MakeButton(_tabs, text, selected ? UiFactory.Paper : TabIdle, selected ? UiFactory.PaperInk : UiFactory.Ink, () =>
            {
                if (_tab != tab)
                {
                    _tab = tab;
                    Draw();
                    Sounds.Play(Sound.PageTurn);
                }
            }, 36, 104, 30);
            button.GetComponent<LayoutElement>().minWidth = 200;
        }

        /// <summary>La libreta: una tarjeta de papel con cada ramo y su nota final en una ficha.</summary>
        private void DrawNotes(LifePresenter presenter, ReportCardView? card)
        {
            _title.text = presenter.Text("ui.notas.titulo");
            if (card == null)
            {
                _subtitle.text = presenter.Text("ui.notas.sin_notas");
                return;
            }

            _subtitle.text = presenter.Text(card.Semester
                ? (card.Finals ? "ui.notas.fin_semestre" : "ui.notas.ramos_semestre")
                : (card.Finals ? "ui.notas.fin_ano" : "ui.notas.titulo"));

            RectTransform sheet = UiFactory.Column(_body, "Libreta", 36, 0, UiFactory.Paper);
            bool anyRed = false;
            for (int i = 0; i < card.Subjects.Count; i++)
            {
                SubjectGradesView subject = card.Subjects[i];
                anyRed |= subject.Failing;
                if (i > 0)
                {
                    Line(sheet);
                }

                SubjectRow(sheet, subject);
            }

            if (card.Subjects.Count == 0)
            {
                UiFactory.Label(sheet, presenter.Text("ui.notas.sin_notas"), 38, UiFactory.PaperMutedInk);
            }

            if (card.General != null)
            {
                Line(sheet);
                UiFactory.Label(sheet, presenter.Text("ui.notas.promedio", card.General), 52, UiFactory.PaperInk, FontStyles.Normal, Face.Title)
                    .margin = new Vector4(0f, 24f, 0f, 8f);
            }

            if (card.Semester)
            {
                RectTransform note = UiFactory.Column(sheet, "Aviso", 28, 0, RedTint);
                UiFactory.Label(note, presenter.Text("ui.notas.aviso_u"), UiFactory.MinFontSize, RedInk);
            }

            if (card.Finals && card.Subjects.Count > 0)
            {
                Sounds.Play(anyRed ? Sound.RedMark : Sound.PencilTap);
            }
        }

        /// <summary>Un ramo: su nombre (y sus notas del año, si hay) a la izquierda y la nota final en una ficha.</summary>
        private static void SubjectRow(RectTransform sheet, SubjectGradesView subject)
        {
            RectTransform row = HorizontalRow(sheet, "Ramo", 20);
            if (subject.Failing)
            {
                UiFactory.RoundedImage(row.gameObject, RedTint).pixelsPerUnitMultiplier = 2f;
            }

            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 22, 22);
            RectTransform text = UiFactory.Column(row, "Nombre", 0, 4);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(text, subject.Name, 40, UiFactory.PaperInk, FontStyles.Bold);
            if (subject.Marks.Count > 0)
            {
                UiFactory.Label(text, string.Join("   ", subject.Marks), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Normal, Face.Mono);
            }

            if (subject.Average.Length == 0)
            {
                return;
            }

            RectTransform chip = UiFactory.Child(row, "Ficha");
            LayoutElement size = chip.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 150;
            size.minHeight = size.preferredHeight = 92;
            UiFactory.RoundedImage(chip.gameObject, subject.Failing ? Red : MarkChip).pixelsPerUnitMultiplier = 1.6f;
            TextMeshProUGUI mark = UiFactory.Label(chip, subject.Average, 44, subject.Failing ? Color.white : UiFactory.PaperInk, FontStyles.Bold, Face.Title);
            UiFactory.Stretch(mark.rectTransform);
            mark.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>Tu curso o tu sección: el profe arriba y cada compañero en una fila con su cara.</summary>
        private void DrawClass(LifePresenter presenter, SchoolClassView? cls)
        {
            if (cls == null)
            {
                _title.text = string.Empty;
                _subtitle.text = string.Empty;
                return;
            }

            _title.text = cls.Name;
            _subtitle.text = cls.Teacher == null ? string.Empty : presenter.Text(cls.Section ? "ui.seccion.profe" : "ui.curso.profe_jefe", cls.Teacher);
            RectTransform sheet = UiFactory.Column(_body, "Curso", 28, 0, UiFactory.Paper);
            UiFactory.Label(sheet, presenter.Text(cls.Section ? "ui.seccion.titulo" : "ui.curso.titulo", cls.Name, cls.Students), UiFactory.MinFontSize, UiFactory.PaperMutedInk)
                .margin = new Vector4(16f, 8f, 16f, 16f);
            foreach (ClassmateView mate in cls.Classmates)
            {
                Line(sheet);
                PersonRow(sheet, mate, false);
            }

            // Quienes dejaron la sección el último semestre, al final y en gris.
            foreach (ClassmateView mate in cls.Left)
            {
                Line(sheet);
                PersonRow(sheet, mate, true);
            }
        }

        /// <summary>
        /// Una persona: su cara (con sus iniciales), su nombre, qué son (en una ficha) y lo último
        /// que vivieron juntos. Nunca barras ni números. Al tocarla se lee completo lo que vivieron.
        /// </summary>
        private static void PersonRow(RectTransform sheet, ClassmateView mate, bool gone)
        {
            RectTransform row = HorizontalRow(sheet, mate.Name, 24);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(16, 16, 20, 20);
            Image hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = row.gameObject.AddComponent<Button>();

            RectTransform face = UiFactory.Child(row, "Cara");
            LayoutElement faceSize = face.gameObject.AddComponent<LayoutElement>();
            faceSize.minWidth = faceSize.preferredWidth = 112;
            faceSize.minHeight = faceSize.preferredHeight = 112;
            UiFactory.RoundedImage(face.gameObject, gone ? Gone : Faces[Mathf.Abs(mate.Id) % Faces.Length]).pixelsPerUnitMultiplier = 0.5f;
            TextMeshProUGUI initials = UiFactory.Label(face, Initials(mate.Name), 40, Color.white, FontStyles.Bold);
            UiFactory.Stretch(initials.rectTransform);
            initials.alignment = TextAlignmentOptions.Center;

            RectTransform text = UiFactory.Column(row, "Nombre y recuerdo", 0, 6);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(text, mate.Name, 42, gone ? UiFactory.PaperMutedInk : UiFactory.PaperInk, FontStyles.Bold);
            TextMeshProUGUI? memory = null;
            if (!string.IsNullOrEmpty(mate.Memory))
            {
                memory = UiFactory.Label(text, mate.Memory!, UiFactory.MinFontSize, UiFactory.PaperMutedInk);
                memory.maxVisibleLines = 1;
                memory.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (mate.Bond != null)
            {
                Tag(row, mate.Bond, gone ? Rule : UiFactory.PaperOption, gone ? UiFactory.PaperMutedInk : UiFactory.PaperInk);
            }

            button.onClick.AddListener(() =>
            {
                Sounds.Play(gone ? Sound.DoorFar : Sound.PhotoClick);
                if (memory != null)
                {
                    memory.maxVisibleLines = memory.maxVisibleLines == 1 ? 99 : 1;
                }
            });
        }

        /// <summary>La malla: cada semestre en una tarjeta, del primero al último.</summary>
        private void DrawPlan(LifePresenter presenter, StudyPlanView? plan)
        {
            _title.text = presenter.Text("ui.malla.titulo");
            if (plan == null)
            {
                _subtitle.text = string.Empty;
                return;
            }

            _subtitle.text = plan.Career + "\n" + plan.Progress;
            foreach (PlanSemesterView semester in plan.Semesters)
            {
                SemesterCard(presenter, semester);
            }
        }

        /// <summary>
        /// Un semestre: aprobado en verde, el que cursas en papel con borde amarillo, el que repites
        /// en rojo suave (con los ramos que quedaron rojos) y los que faltan apagados, sin rellenar.
        /// </summary>
        private void SemesterCard(LifePresenter presenter, PlanSemesterView semester)
        {
            Color background = semester.State switch
            {
                PlanSemesterState.Approved => GreenTint,
                PlanSemesterState.Retaking => RedTint,
                PlanSemesterState.Current => UiFactory.Paper,
                _ => Color.clear,
            };
            bool future = semester.State == PlanSemesterState.Future;
            Color ink = future ? UiFactory.MutedInk : UiFactory.PaperInk;
            RectTransform card = UiFactory.Column(_body, semester.Name, 32, 10, background);
            if (semester.State == PlanSemesterState.Current)
            {
                Outline border = card.gameObject.AddComponent<Outline>();
                border.effectColor = UiFactory.Accent;
                border.effectDistance = new Vector2(5f, -5f);
            }
            else if (future)
            {
                // Lo que falta: sin rellenar, solo el borde.
                Outline border = card.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(UiFactory.MutedInk.r, UiFactory.MutedInk.g, UiFactory.MutedInk.b, 0.45f);
                border.effectDistance = new Vector2(3f, -3f);
                border.useGraphicAlpha = false;
            }

            RectTransform top = HorizontalRow(card, "Semestre", 16);
            TextMeshProUGUI name = UiFactory.Label(top, semester.Name, 44, ink, FontStyles.Normal, Face.Title);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            switch (semester.State)
            {
                case PlanSemesterState.Approved:
                    Tag(top, presenter.Text("ui.malla.aprobado"), Green, Color.white);
                    break;
                case PlanSemesterState.Current:
                    Tag(top, presenter.Text("ui.malla.cursando"), UiFactory.Accent, UiFactory.AccentText);
                    break;
                case PlanSemesterState.Retaking:
                    Tag(top, presenter.Text("ui.malla.repites"), Red, Color.white);
                    break;
            }

            UiFactory.Label(card, string.Join(" · ", semester.Courses), UiFactory.MinFontSize, future ? UiFactory.MutedInk : UiFactory.PaperMutedInk);
            if (semester.FailedCourses.Count > 0)
            {
                UiFactory.Label(card, presenter.Text("ui.malla.rojos", string.Join(", ", semester.FailedCourses)), UiFactory.MinFontSize, RedInk, FontStyles.Bold);
            }
        }

        /// <summary>Una ficha chica con una palabra (el lazo, el estado de un semestre).</summary>
        private static void Tag(RectTransform parent, string text, Color background, Color ink)
        {
            RectTransform tag = UiFactory.Child(parent, "Ficha");
            HorizontalLayoutGroup layout = tag.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(26, 26, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;
            UiFactory.RoundedImage(tag.gameObject, background).pixelsPerUnitMultiplier = 1.2f;
            TextMeshProUGUI label = UiFactory.Label(tag, text, UiFactory.MinFontSize, ink, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static RectTransform HorizontalRow(Transform parent, string name, int spacing)
        {
            RectTransform row = UiFactory.Child(parent, name);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        /// <summary>Una raya fina entre filas.</summary>
        private static void Line(RectTransform parent)
        {
            RectTransform line = UiFactory.Child(parent, "Raya");
            line.gameObject.AddComponent<Image>().color = Rule;
            LayoutElement size = line.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 3;
        }

        /// <summary>Las iniciales de un nombre ("Camila Rojas" → "CR").</summary>
        private static string Initials(string name)
        {
            var letters = new List<char>();
            foreach (string part in name.Split(' '))
            {
                if (part.Length > 0 && letters.Count < 2)
                {
                    letters.Add(char.ToUpperInvariant(part[0]));
                }
            }

            return new string(letters.ToArray());
        }
    }
}
