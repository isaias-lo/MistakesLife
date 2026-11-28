#nullable enable

using System.Collections;
using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// El aviso del banco (D6-1c, boceto D6-1 aprobado): cuando te llega plata a la cuenta (el sueldo,
    /// el Seguro de Cesantía, una devolución), baja arriba de la pantalla una tarjeta del banco con lo
    /// que recibiste y suena la caja. Se va sola en unos segundos o al tocarla. Lo que te llegó lo dice
    /// el núcleo (LifePresenter.TakeBankNotices()); los pagos que salen no avisan: se ven en la cartola.
    /// </summary>
    public sealed class BankNotice
    {
        /// <summary>Cuántos segundos queda el aviso antes de irse solo.</summary>
        private const float Seconds = 4.5f;

        /// <summary>Cuántos depósitos caben en un aviso (si llegan más, van los más grandes).</summary>
        private const int MaxLines = 3;

        private readonly Transform _parent;
        private readonly MonoBehaviour _host;
        private RectTransform? _card;
        private Coroutine? _hide;

        public BankNotice(Transform parent, MonoBehaviour host)
        {
            _parent = parent;
            _host = host;
        }

        /// <summary>Muestra lo que te llegó desde el último aviso; si no llegó nada, no hace nada.</summary>
        public void Show(LifePresenter presenter)
        {
            IReadOnlyList<MoneyLineView> notices = presenter.TakeBankNotices();
            BankView? bank = notices.Count == 0 ? null : presenter.Bank();
            if (bank == null)
            {
                return;
            }

            Close();
            _card = UiFactory.Column(_parent, "Aviso del banco", 28, 10, UiFactory.Paper);
            _card.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            _card.anchorMin = new Vector2(0.04f, 1f);
            _card.anchorMax = new Vector2(0.96f, 1f);
            _card.pivot = new Vector2(0.5f, 1f);
            _card.anchoredPosition = new Vector2(0f, -24f);
            _card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Image image = _card.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = _card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Close);

            RectTransform top = UiFactory.Child(_card, "Quién avisa");
            HorizontalLayoutGroup row = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 18;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            RectTransform badge = UiFactory.Child(top, "Logo");
            UiFactory.RoundedImage(badge.gameObject, BankScreen.KindColor(bank.Kind));
            LayoutElement size = badge.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 64;
            size.minHeight = size.preferredHeight = 64;
            TextMeshProUGUI initials = UiFactory.Label(badge, bank.Initials, 26, Color.white, FontStyles.Bold);
            initials.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(initials.rectTransform);
            UiFactory.Label(top, bank.BankName + " · " + presenter.Text("ui.banco.ahora"), 34, UiFactory.PaperMutedInk);

            var sorted = new List<MoneyLineView>(notices);
            sorted.Sort((a, b) => b.AmountLabel.Length != a.AmountLabel.Length ? b.AmountLabel.Length.CompareTo(a.AmountLabel.Length) : string.CompareOrdinal(b.AmountLabel, a.AmountLabel));
            for (int i = 0; i < sorted.Count && i < MaxLines; i++)
            {
                MoneyLineView line = sorted[i];
                TextMeshProUGUI text = UiFactory.Label(_card, presenter.Text("ui.banco.aviso", line.AmountLabel.TrimStart('+'), line.Label), 38, UiFactory.PaperInk);
                text.textWrappingMode = TextWrappingModes.Normal;
            }

            _card.SetAsLastSibling();
            Sounds.Play(Sound.Register);
            if (Application.isPlaying)
            {
                _hide = _host.StartCoroutine(HideLater());
            }
        }

        public void Close()
        {
            if (_hide != null)
            {
                _host.StopCoroutine(_hide);
                _hide = null;
            }

            if (_card != null)
            {
                Object.Destroy(_card.gameObject);
                _card = null;
            }
        }

        private IEnumerator HideLater()
        {
            yield return new WaitForSeconds(Seconds);
            _hide = null;
            Close();
        }
    }
}
