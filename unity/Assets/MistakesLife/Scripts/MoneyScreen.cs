#nullable enable

using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tu dinero (docs/diseno-app.md, "Tu dinero"): se abre encima de la vida como el papel que
    /// sale del cajero. Arriba, lo que tienes; después, cada movimiento del último mes con el
    /// nombre de lo que lo causó y cuántos días de sueldo es; al final, lo que debes.
    /// Regla de Isaias, nada se inventa: solo se dibuja lo que da LifePresenter.Money()
    /// (lo que el núcleo de verdad cobró y pagó). Aquí no se decide nada.
    /// </summary>
    public sealed class MoneyScreen
    {
        /// <summary>Verde oscuro para lo que entra (sobre papel se lee mejor que el verde claro).</summary>
        private static readonly Color IncomeInk = UiFactory.Hex(0x1D6B4F);

        private readonly Transform _parent;
        private RectTransform? _layer;
        private RectTransform _body = null!;
        private TextMeshProUGUI _closeLabel = null!;

        public MoneyScreen(Transform parent)
        {
            _parent = parent;
        }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        public void Show(LifePresenter presenter)
        {
            Ensure();
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Draw(presenter, presenter.Money());
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Arma una sola vez: fondo oscuro, la billetera abierta (verde) y adentro el papel con scroll.</summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Tu dinero", UiFactory.Shade);
            RectTransform wallet = UiFactory.Column(_layer, "Billetera abierta", 28, 20, Things.Wallet);
            wallet.anchorMin = new Vector2(0.05f, 0.05f);
            wallet.anchorMax = new Vector2(0.95f, 0.95f);
            wallet.offsetMin = Vector2.zero;
            wallet.offsetMax = Vector2.zero;

            _body = UiFactory.ScrollColumn(wallet, "Papel del cajero", 40, 14);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = UiFactory.Paper;
            Button close = UiFactory.MakeButton(wallet, string.Empty, UiFactory.Paper, UiFactory.PaperInk, Close);
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Draw(LifePresenter presenter, MoneyView money)
        {
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;

            UiFactory.Label(_body, presenter.Text("ui.dinero.titulo"), 52, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(_body, presenter.Text("ui.dinero.tienes"), UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            UiFactory.Label(_body, money.BalanceLabel, 72, UiFactory.PaperInk, FontStyles.Bold, Face.Mono);
            Cut();

            UiFactory.Label(_body, presenter.Text("ui.dinero.movimientos").ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold, Face.Mono);
            if (money.Lines.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.dinero.sin_movimientos"), 36, UiFactory.PaperInk);
            }

            foreach (MoneyLineView line in money.Lines)
            {
                Line(line, line.IsIncome ? IncomeInk : UiFactory.KeyInk);
            }

            if (money.SpentLabel.Length > 0)
            {
                Cut();
                Line(new MoneyLineView(string.Empty, presenter.Text("ui.dinero.salio"), money.SpentLabel, false, string.Empty), UiFactory.KeyInk);
                if (money.DaysLabel.Length > 0)
                {
                    // Lo más importante del mes, con la letra a mano del jugador.
                    UiFactory.Label(_body, money.DaysLabel, 46, UiFactory.PaperInk, FontStyles.Normal, Face.Hand);
                }
            }

            if (money.Debts.Count > 0)
            {
                Cut();
                UiFactory.Label(_body, presenter.Text("ui.dinero.debes").ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold, Face.Mono);
                foreach (MoneyLineView debt in money.Debts)
                {
                    Line(debt, UiFactory.PaperInk);
                }
            }
        }

        /// <summary>Un movimiento: el nombre a la izquierda, el monto a la derecha y, debajo, la nota chica.</summary>
        private void Line(MoneyLineView line, Color amountColor)
        {
            RectTransform block = UiFactory.Column(_body, "Movimiento", 0, 2);
            RectTransform row = UiFactory.Child(block, "Fila");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            TextMeshProUGUI name = UiFactory.Label(row, line.Label, 36, UiFactory.PaperInk);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI amount = UiFactory.Label(row, line.AmountLabel, 36, amountColor, FontStyles.Bold, Face.Mono);
            amount.alignment = TextAlignmentOptions.TopRight;
            if (line.Note.Length > 0)
            {
                UiFactory.Label(block, line.Note, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Italic);
            }
        }

        /// <summary>La línea punteada del papel del cajero, entre una parte y otra.</summary>
        private void Cut()
        {
            TextMeshProUGUI cut = UiFactory.Label(_body, new string('-', 32), UiFactory.MinFontSize, UiFactory.PaperOption, FontStyles.Normal, Face.Mono);
            cut.overflowMode = TextOverflowModes.Truncate;
        }
    }
}
