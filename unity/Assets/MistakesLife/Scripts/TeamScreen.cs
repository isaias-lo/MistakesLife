#nullable enable

using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tu equipo del trabajo (estilo de la versión 4, como el curso): sobre el fondo noche, la
    /// franja color corcho con el título y, en una tarjeta de papel, la jefatura arriba y los
    /// colegas abajo, cada uno con su cara, su nombre, qué son y lo último que vivieron juntos.
    /// Quienes se fueron este año, al final y en gris. Lo da <see cref="LifePresenter.Team"/>.
    /// Abajo, "Lo que decides tú": tus vacaciones ganadas y un botón por cada cosa que puedes
    /// pedir (aumento, vacaciones, renunciar). El botón solo pide el momento al núcleo
    /// (<c>Session.Request</c>); la decisión se toma después en la carta, como cualquier momento.
    /// </summary>
    public sealed class TeamScreen
    {
        /// <summary>El material del trabajo: corcho (docs/archivo/diseno-app.md), un poco más oscuro para leer la franja.</summary>
        public static readonly Color Band = UiFactory.Hex(0x7E5A3A);

        private readonly Transform _parent;
        private RectTransform? _layer;
        private TextMeshProUGUI _title = null!;
        private RectTransform _body = null!;
        private TextMeshProUGUI _closeLabel = null!;

        public TeamScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama después de pedir un momento, para que la vida dibuje su carta.</summary>
        public System.Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        public void Show(LifePresenter presenter)
        {
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Draw(presenter, presenter.Team());
            Sounds.Play(Sound.PageTurn);
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

            _layer = UiFactory.Overlay(_parent, "Tu equipo", UiFactory.Background);
            VerticalLayoutGroup page = _layer.gameObject.AddComponent<VerticalLayoutGroup>();
            page.childControlWidth = true;
            page.childControlHeight = true;
            page.childForceExpandWidth = true;
            page.childForceExpandHeight = false;

            RectTransform band = UiFactory.Child(_layer, "Franja");
            band.gameObject.AddComponent<Image>().color = Band;
            VerticalLayoutGroup bandLayout = band.gameObject.AddComponent<VerticalLayoutGroup>();
            bandLayout.padding = new RectOffset(56, 56, 56, 48);
            bandLayout.childControlWidth = true;
            bandLayout.childControlHeight = true;
            bandLayout.childForceExpandWidth = true;
            bandLayout.childForceExpandHeight = false;
            _title = UiFactory.Label(band, string.Empty, 64, UiFactory.Ink, FontStyles.Normal, Face.Title);

            _body = UiFactory.ScrollColumn(_layer, "Equipo", 40, 28);
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

        /// <summary>La barra "cómo te ve tu jefa" con su frase (D5b), en grande arriba de todo.</summary>
        private void DrawBossView(TeamView team)
        {
            if (team.BossView == null)
            {
                return;
            }

            RectTransform card = UiFactory.Column(_body, "Cómo te ve tu jefa", 28, 8, UiFactory.Paper);
            UiFactory.Label(card, team.BossTitle ?? string.Empty, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold)
                .margin = new Vector4(16f, 0f, 16f, 0f);
            RectTransform fill = UiFactory.Bar(card, "Barra de la jefa", team.BossView >= 70 ? UiFactory.Good : team.BossView < 45 ? UiFactory.Bad : UiFactory.Accent, out TextMeshProUGUI label);
            label.text = team.BossView + " %";
            label.color = UiFactory.PaperInk;
            UiFactory.SetBar(fill, team.BossView.Value);
            UiFactory.Label(card, team.BossPhrase ?? string.Empty, 40, UiFactory.PaperInk, FontStyles.Bold)
                .margin = new Vector4(16f, 8f, 16f, 0f);
        }

        /// <summary>La agenda del mes (D5b): cada tarea con su plazo y cómo va; lo hecho, tachado.</summary>
        private void DrawTasks(LifePresenter presenter, TeamView team)
        {
            if (team.Tasks.Count == 0)
            {
                return;
            }

            RectTransform agenda = UiFactory.Column(_body, "Tareas del mes", 28, 0, UiFactory.Paper);
            UiFactory.Label(agenda, presenter.Text("ui.tarea.titulo"), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold)
                .margin = new Vector4(16f, 0f, 16f, 8f);
            foreach (WorkTaskView task in team.Tasks)
            {
                StudiesScreen.Line(agenda);
                TextMeshProUGUI name = UiFactory.Label(agenda, task.Text, 40, task.Done ? UiFactory.PaperMutedInk : UiFactory.PaperInk, task.Done ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold);
                name.margin = new Vector4(16f, 16f, 16f, 0f);
                UiFactory.Label(agenda, task.Due + " · " + task.State, UiFactory.MinFontSize, task.Late ? UiFactory.KeyInk : UiFactory.PaperMutedInk)
                    .margin = new Vector4(16f, 4f, 16f, 16f);
            }
        }

        private void Draw(LifePresenter presenter, TeamView? team)
        {
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            _title.text = presenter.Text("ui.pega.titulo");
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            if (team == null)
            {
                UiFactory.Label(_body, presenter.Text("ui.pega.sin_equipo"), 38, UiFactory.MutedInk);
                return;
            }

            DrawBossView(team);
            DrawTasks(presenter, team);
            RectTransform sheet = UiFactory.Column(_body, "Hoja del equipo", 28, 0, UiFactory.Paper);
            if (team.Boss != null)
            {
                StudiesScreen.PersonRow(sheet, team.Boss, false);
            }

            UiFactory.Label(sheet, presenter.Text("ui.pega.colegas"), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold)
                .margin = new Vector4(16f, 24f, 16f, 8f);
            foreach (ClassmateView mate in team.Colleagues)
            {
                StudiesScreen.Line(sheet);
                StudiesScreen.PersonRow(sheet, mate, false);
            }

            foreach (ClassmateView mate in team.Left)
            {
                StudiesScreen.Line(sheet);
                StudiesScreen.PersonRow(sheet, mate, true);
            }

            if (team.Status == null && team.Actions.Count == 0)
            {
                return;
            }

            RectTransform yours = UiFactory.Column(_body, "Lo que decides tú", 28, 16, UiFactory.Paper);
            UiFactory.Label(yours, presenter.Text("ui.pega.decides_tu"), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold)
                .margin = new Vector4(16f, 8f, 16f, 0f);
            if (team.Status != null)
            {
                UiFactory.Label(yours, team.Status, 36, UiFactory.PaperInk).margin = new Vector4(16f, 0f, 16f, 8f);
            }

            foreach (ActionView action in team.Actions)
            {
                string eventId = action.EventId;
                UiFactory.MakeButton(yours, action.Label, UiFactory.PaperOption, UiFactory.PaperInk, () =>
                {
                    Sounds.Play(Sound.PencilTap);
                    if (presenter.Session.Request(eventId))
                    {
                        Close();
                        Changed?.Invoke();
                    }
                });
            }
        }
    }
}
