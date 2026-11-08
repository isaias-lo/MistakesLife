#nullable enable

using MistakesLife.Core.Content;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tutorial corto: algunas tarjetas con lo básico, encima de la pantalla de la vida.
    /// Sale solo la primera vez (se recuerda en el teléfono) y se puede ver de nuevo
    /// desde "Cómo se juega". Es solo interfaz: no toca la partida.
    /// </summary>
    public sealed class Tutorial
    {
        /// <summary>Cuántas tarjetas tiene (claves ui.tutorial.1 a ui.tutorial.N).</summary>
        public const int Steps = 5;

        /// <summary>Preferencia del teléfono que recuerda que ya se vio.</summary>
        private const string SeenKey = "tutorial_visto";

        private readonly Transform _parent;
        private readonly ContentDatabase _content;
        private RectTransform? _layer;

        public Tutorial(Transform parent, ContentDatabase content)
        {
            _parent = parent;
            _content = content;
        }

        public static bool AlreadySeen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        /// <summary>Muestra el tutorial solo si nunca se vio.</summary>
        public void ShowFirstTime()
        {
            if (!AlreadySeen)
            {
                Show();
            }
        }

        /// <summary>Muestra el tutorial desde la primera tarjeta.</summary>
        public void Show() => ShowStep(1);

        private void ShowStep(int step)
        {
            Close();
            _layer = UiFactory.Overlay(_parent, "Tutorial", UiFactory.Shade);

            RectTransform card = UiFactory.Column(_layer, "Tarjeta", 48, 28, UiFactory.Card);
            card.anchorMin = new Vector2(0.06f, 0.5f);
            card.anchorMax = new Vector2(0.94f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = Vector2.zero;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.Label(card, $"{step} / {Steps}", 30, UiFactory.MutedInk);
            UiFactory.Label(card, _content.Text("ui.tutorial." + step), 42, UiFactory.Ink);
            bool last = step == Steps;
            UiFactory.MakeButton(card, _content.Text(last ? "ui.boton.entendido" : "ui.boton.siguiente"), UiFactory.Accent, UiFactory.AccentText, () =>
            {
                if (last)
                {
                    PlayerPrefs.SetInt(SeenKey, 1);
                    PlayerPrefs.Save();
                    Close();
                }
                else
                {
                    ShowStep(step + 1);
                }
            });
        }

        private void Close()
        {
            if (_layer != null)
            {
                Object.Destroy(_layer.gameObject);
                _layer = null;
            }
        }
    }
}
