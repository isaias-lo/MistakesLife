#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La carpeta del juzgado (docs/diseno-app.md, "Juzgado"): una carpeta de cartulina que se abre
    /// encima de la vida. Adentro, cada causa en su hoja: el RIT en letra de máquina, el tribunal,
    /// el nombre de la causa, cuándo ingresó, un timbre (EN TRÁMITE o TERMINADA) y la línea del
    /// proceso con cada etapa y su plazo: las que ya pasaron, la actual (con en qué semana va) y
    /// las que faltan. Las abiertas van primero. Regla: aquí no se decide nada; se dibuja lo que
    /// da <see cref="LifePresenter.CourtCases"/>.
    /// </summary>
    public sealed class CourtScreen
    {
        /// <summary>El material del juzgado: cartulina (docs/diseno-app.md).</summary>
        public static readonly Color Cardboard = UiFactory.Hex(0xD9B36E);

        private static readonly Color CardboardDark = UiFactory.Hex(0xB48D4C);
        private static readonly Color StampOpen = UiFactory.Hex(0xB5462B);
        private static readonly Color StampClosed = UiFactory.Hex(0x3E5A8A);
        private static readonly Color LineDone = UiFactory.Hex(0x5A6178);
        private static readonly Color LinePending = UiFactory.Hex(0xC9C1AE);

        private readonly Transform _parent;
        private RectTransform? _layer;
        private RectTransform _body = null!;
        private TextMeshProUGUI _closeLabel = null!;

        public CourtScreen(Transform parent)
        {
            _parent = parent;
        }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        public void Show(LifePresenter presenter)
        {
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Draw(presenter, presenter.CourtCases());
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Arma una sola vez: fondo oscuro, la carpeta de cartulina con su pestaña y adentro las hojas con scroll.</summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Carpeta del juzgado", UiFactory.Shade);

            // La pestaña de la carpeta, asomada arriba a la izquierda.
            RectTransform tab = UiFactory.Child(_layer, "Pestana");
            tab.anchorMin = new Vector2(0.05f, 0.93f);
            tab.anchorMax = new Vector2(0.42f, 0.965f);
            tab.offsetMin = Vector2.zero;
            tab.offsetMax = new Vector2(0f, 20f);
            UiFactory.RoundedImage(tab.gameObject, CardboardDark);

            RectTransform folder = UiFactory.Column(_layer, "Carpeta de cartulina", 28, 20, Cardboard);
            folder.anchorMin = new Vector2(0.05f, 0.05f);
            folder.anchorMax = new Vector2(0.95f, 0.95f);
            folder.offsetMin = Vector2.zero;
            folder.offsetMax = Vector2.zero;

            _body = UiFactory.ScrollColumn(folder, "Hojas", 8, 28);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = Color.clear;
            Button close = UiFactory.MakeButton(folder, string.Empty, CardboardDark, UiFactory.PaperInk, Close);
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Draw(LifePresenter presenter, IReadOnlyList<CourtCaseView> cases)
        {
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;

            UiFactory.Label(_body, presenter.Text("ui.juzgado.titulo"), 52, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            if (cases.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.juzgado.sin_causas"), 38, UiFactory.PaperInk);
                return;
            }

            foreach (CourtCaseView courtCase in cases)
            {
                Sheet(presenter, courtCase);
            }
        }

        /// <summary>La hoja de una causa: arriba el RIT y el timbre, luego tribunal, causa, ingreso y la línea del proceso.</summary>
        private void Sheet(LifePresenter presenter, CourtCaseView courtCase)
        {
            RectTransform sheet = UiFactory.Column(_body, "Hoja de la causa", 32, 10, UiFactory.Paper);

            RectTransform top = UiFactory.Child(sheet, "RIT y timbre");
            HorizontalLayoutGroup topRow = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topRow.spacing = 16;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.childControlWidth = true;
            topRow.childControlHeight = true;
            topRow.childForceExpandWidth = false;
            topRow.childForceExpandHeight = false;
            TextMeshProUGUI rit = UiFactory.Label(top, presenter.Text("ui.juzgado.rit", courtCase.Rit), 40, UiFactory.PaperInk, FontStyles.Bold, Face.Mono);
            rit.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Stamp(top, presenter.Text(courtCase.IsOpen ? "ui.juzgado.en_tramite" : "ui.juzgado.terminada"), courtCase.IsOpen ? StampOpen : StampClosed);

            UiFactory.Label(sheet, courtCase.Court, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold);
            UiFactory.Label(sheet, courtCase.Name, 44, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(sheet, presenter.Text("ui.juzgado.ingreso", courtCase.OpenedLabel), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Normal, Face.Mono);
            if (courtCase.Outcome != null)
            {
                UiFactory.Label(sheet, presenter.Text("ui.juzgado.resultado", courtCase.Outcome), 36, UiFactory.PaperInk, FontStyles.Italic);
            }

            Gap(sheet, 8f);
            for (int i = 0; i < courtCase.Stages.Count; i++)
            {
                Stage(presenter, sheet, courtCase, courtCase.Stages[i], i == courtCase.Stages.Count - 1);
            }
        }

        /// <summary>
        /// Una etapa de la línea del proceso: a la izquierda un punto (lleno si ya pasó, con anillo
        /// si es la de ahora, vacío si falta) y el trazo hacia la siguiente; a la derecha su nombre y su plazo.
        /// </summary>
        private static void Stage(LifePresenter presenter, RectTransform sheet, CourtCaseView courtCase, CourtStageView stage, bool last)
        {
            RectTransform row = UiFactory.Child(sheet, "Etapa");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // La columna del punto y el trazo hacia abajo.
            RectTransform rail = UiFactory.Child(row, "Linea del proceso");
            LayoutElement railSize = rail.gameObject.AddComponent<LayoutElement>();
            railSize.minWidth = railSize.preferredWidth = 40;
            Color color = stage.Done || stage.Current ? LineDone : LinePending;
            if (!last)
            {
                RectTransform line = UiFactory.Child(rail, "Trazo");
                line.anchorMin = new Vector2(0.5f, 0f);
                line.anchorMax = new Vector2(0.5f, 1f);
                line.offsetMin = new Vector2(-2f, 0f);
                line.offsetMax = new Vector2(2f, -30f);
                line.gameObject.AddComponent<Image>().color = stage.Done ? LineDone : LinePending;
            }

            RectTransform dot = UiFactory.Child(rail, "Punto");
            dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 1f);
            dot.pivot = new Vector2(0.5f, 1f);
            dot.sizeDelta = new Vector2(32f, 32f);
            dot.anchoredPosition = new Vector2(0f, -4f);
            Image dotImage = UiFactory.RoundedImage(dot.gameObject, stage.Current ? StampOpen : color);
            dotImage.pixelsPerUnitMultiplier = 56f / 32f;
            if (!stage.Done && !stage.Current)
            {
                // Lo que falta: un punto hueco (el centro del color del papel).
                RectTransform hole = UiFactory.Child(dot, "Hueco");
                hole.anchorMin = hole.anchorMax = new Vector2(0.5f, 0.5f);
                hole.sizeDelta = new Vector2(20f, 20f);
                UiFactory.RoundedImage(hole.gameObject, UiFactory.Paper).pixelsPerUnitMultiplier = 56f / 20f;
            }

            RectTransform text = UiFactory.Column(row, "Etapa y plazo", 0, 2);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            text.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, last ? 0 : 22);
            UiFactory.Label(text, stage.Name, 36, stage.Done || stage.Current ? UiFactory.PaperInk : UiFactory.PaperMutedInk, stage.Current ? FontStyles.Bold : FontStyles.Normal);
            if (stage.Current && stage.Weeks > 0)
            {
                string progress = courtCase.WeeksInStage > stage.Weeks
                    ? presenter.Text("ui.juzgado.atrasada", courtCase.WeeksInStage)
                    : presenter.Text("ui.juzgado.vas", System.Math.Max(1, courtCase.WeeksInStage), stage.Weeks);
                UiFactory.Label(text, progress, UiFactory.MinFontSize, StampOpen, FontStyles.Bold);
            }
            else if (stage.Weeks > 0)
            {
                UiFactory.Label(text, presenter.Text("ui.juzgado.plazo", stage.Weeks), UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            }
        }

        /// <summary>El timbre de goma: un recuadro chueco con el borde y las letras del mismo color.</summary>
        private static void Stamp(RectTransform parent, string text, Color color)
        {
            RectTransform holder = UiFactory.Child(parent, "Timbre");
            LayoutElement size = holder.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 270;
            size.minHeight = size.preferredHeight = 76;

            RectTransform stamp = UiFactory.Child(holder, "Borde del timbre");
            UiFactory.Stretch(stamp);
            stamp.localRotation = Quaternion.Euler(0f, 0f, -6f);
            UiFactory.RoundedImage(stamp.gameObject, color).pixelsPerUnitMultiplier = 3f;
            RectTransform inside = UiFactory.Child(stamp, "Adentro del timbre");
            UiFactory.Stretch(inside);
            inside.offsetMin = new Vector2(5f, 5f);
            inside.offsetMax = new Vector2(-5f, -5f);
            UiFactory.RoundedImage(inside.gameObject, UiFactory.Paper).pixelsPerUnitMultiplier = 3.5f;
            TextMeshProUGUI label = UiFactory.Label(inside, text, UiFactory.MinFontSize, color, FontStyles.Bold, Face.Mono);
            UiFactory.Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void Gap(RectTransform parent, float height)
        {
            UiFactory.Child(parent, "Espacio").gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
