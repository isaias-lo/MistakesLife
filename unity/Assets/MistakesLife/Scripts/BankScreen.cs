#nullable enable

using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Tu banco (D6-1c, bocetos D6-1 aprobados por Isaias el 22-11-2026): se abre desde Tu dinero.
    /// Sin cuenta, muestra los bancos de tu país en tarjetas de papel para elegir dónde abrirla, y el
    /// banco contesta al tiro. Con cuenta, dos pestañas: "Cuenta" (el saldo, lo que te entra en el
    /// mes o el año, lo que sale y tu Seguro de Cesantía) y "Cartola" (cada movimiento con su fecha,
    /// mes por mes). Solo dibuja lo que da el núcleo (LifePresenter.Bank(), BankOffers(), Income() y
    /// Money()): nada se inventa aquí.
    /// </summary>
    public sealed class BankScreen
    {
        /// <summary>Verde oscuro para lo que entra (sobre papel se lee mejor que el verde claro).</summary>
        private static readonly Color IncomeInk = UiFactory.Hex(0x1D6B4F);

        private readonly Transform _parent;
        private RectTransform? _layer;
        private RectTransform _frame = null!;
        private RectTransform _body = null!;
        private RectTransform _tabs = null!;
        private TextMeshProUGUI _closeLabel = null!;
        private LifePresenter? _presenter;

        // Lo que está mirando: la pestaña, el mes de la cartola (0 = el más nuevo), si ve el año y el detalle de lo que sale.
        private bool _statement;
        private int _month;
        private bool _year;
        private bool _details;
        private string? _chosenBank;

        public BankScreen(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>Se llama cuando abriste tu cuenta, para que la cabecera deje de mostrar efectivo.</summary>
        public System.Action? Changed { get; set; }

        public bool IsOpen => _layer != null && _layer.gameObject.activeSelf;

        /// <summary>El color del logo de cada tipo de banco.</summary>
        public static Color KindColor(string kind) => kind switch
        {
            "estatal" => UiFactory.Hex(0x2E6B4F),
            "grande" => UiFactory.Hex(0x1F3A68),
            "digital" => UiFactory.Hex(0x6B3FA0),
            _ => UiFactory.Hex(0x5A6178),
        };

        /// <summary>Abre tu cuenta (si tienes) o los bancos para elegir (si no).</summary>
        public void Show(LifePresenter presenter, bool statement = false)
        {
            Ensure();
            _presenter = presenter;
            _statement = statement;
            _month = 0;
            _year = false;
            _details = false;
            _chosenBank = null;
            _layer!.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Redraw();
        }

        public void Close()
        {
            if (_layer != null)
            {
                _layer.gameObject.SetActive(false);
            }
        }

        /// <summary>Para las capturas: elige el primer banco que te abriría la cuenta y lo pide, para ver la respuesta.</summary>
        public void OpenFirstForCapture()
        {
            BankOfferView? offer = _presenter?.BankOffers().FirstOrDefault(o => o.CanOpen);
            if (offer != null)
            {
                Open(offer);
            }
        }

        /// <summary>Arma una sola vez: fondo oscuro, el marco del color del banco, el papel con scroll y los botones de abajo.</summary>
        private void Ensure()
        {
            if (_layer != null)
            {
                return;
            }

            _layer = UiFactory.Overlay(_parent, "Tu banco", UiFactory.Shade);
            _frame = UiFactory.Column(_layer, "Marco del banco", 28, 20, KindColor(string.Empty));
            _frame.anchorMin = new Vector2(0.05f, 0.05f);
            _frame.anchorMax = new Vector2(0.95f, 0.95f);
            _frame.offsetMin = Vector2.zero;
            _frame.offsetMax = Vector2.zero;

            _body = UiFactory.ScrollColumn(_frame, "Papel del banco", 40, 16);
            ((RectTransform)_body.parent!.parent!).GetComponent<Image>().color = UiFactory.Paper;
            _tabs = Row(_frame, "Pestañas", 16);
            Button close = UiFactory.MakeButton(_frame, string.Empty, UiFactory.Paper, UiFactory.PaperInk, Close);
            _closeLabel = close.GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Redraw()
        {
            LifePresenter presenter = _presenter!;
            _closeLabel.text = presenter.Text("ui.boton.cerrar");
            UiFactory.Clear(_body);
            UiFactory.Clear(_tabs);
            _body.anchoredPosition = Vector2.zero;

            BankView? bank = presenter.Bank();
            _frame.GetComponent<Image>().color = KindColor(bank?.Kind ?? string.Empty);
            _tabs.gameObject.SetActive(bank != null);
            if (bank == null)
            {
                DrawOffers(presenter);
                return;
            }

            Tab(presenter.Text("ui.banco.cuenta"), !_statement, () => _statement = false);
            Tab(presenter.Text("ui.banco.cartola"), _statement, () => _statement = true);
            if (_statement)
            {
                DrawStatement(presenter, bank);
            }
            else
            {
                DrawAccount(presenter, bank);
            }
        }

        /// <summary>La pestaña "Cuenta": el banco, el saldo, lo que te entra, lo que sale y el Seguro de Cesantía.</summary>
        private void DrawAccount(LifePresenter presenter, BankView bank)
        {
            BankTitle(bank.Kind, bank.Initials, bank.BankName, bank.AccountLabel);
            Caption(presenter.Text("ui.banco.saldo"));
            bool belowZero = bank.BalanceLabel.StartsWith("-");
            UiFactory.Label(_body, bank.BalanceLabel, 76, belowZero ? UiFactory.KeyInk : UiFactory.PaperInk, FontStyles.Bold, Face.Mono);
            Rule();

            // Lo que te entra: en el mes (lo que de verdad llegó el último mes) o en el año (la cartola).
            RectTransform head = Row(_body, "Lo que te entra", 12);
            TextMeshProUGUI title = UiFactory.Label(head, presenter.Text("ui.banco.entra").ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold, Face.Mono);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Toggle(head, presenter.Text("ui.banco.mes"), !_year, () => _year = false);
            Toggle(head, presenter.Text("ui.banco.ano"), _year, () => _year = true);

            IReadOnlyList<MoneyLineView> income;
            string total;
            if (_year)
            {
                income = bank.YearIncome;
                total = bank.YearIncomeLabel;
            }
            else
            {
                IncomeView month = presenter.Income();
                income = month.Lines;
                total = month.TotalLabel;
            }

            if (income.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text(_year ? "ui.banco.sin_ingresos_ano" : "ui.banco.sin_ingresos"), 36, UiFactory.PaperInk);
            }

            foreach (MoneyLineView line in income)
            {
                Line(line, IncomeInk);
            }

            if (total.Length > 0)
            {
                Line(new MoneyLineView(string.Empty, presenter.Text("ui.banco.total"), total, true, string.Empty), IncomeInk, bold: true);
            }

            Rule();

            // Lo que sale: el total del último mes en una línea que se abre.
            MoneyView money = presenter.Money();
            Caption(presenter.Text("ui.banco.sale"));
            if (money.SpentLabel.Length == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.banco.nada_sale"), 36, UiFactory.PaperInk);
            }
            else
            {
                UiFactory.Label(_body, money.SpentLabel, 56, UiFactory.KeyInk, FontStyles.Bold, Face.Mono);
                UiFactory.MakeButton(_body, presenter.Text(_details ? "ui.banco.ocultar_detalle" : "ui.banco.ver_detalle"), UiFactory.PaperOption, UiFactory.PaperInk, () =>
                {
                    Sounds.Play(Sound.PageTurn);
                    _details = !_details;
                    Redraw();
                }, 34, 0, 20);
                if (_details)
                {
                    foreach (MoneyLineView line in money.Lines.Where(l => !l.IsIncome))
                    {
                        Line(line, UiFactory.KeyInk);
                    }
                }
            }

            // El Seguro de Cesantía como un plan con lo que llevas ahorrado.
            if (bank.UnemploymentFundLabel.Length > 0)
            {
                Rule();
                RectTransform plan = UiFactory.Column(_body, "Seguro de Cesantía", 28, 6, UiFactory.PaperOption);
                RectTransform row = Row(plan, "Plan", 16);
                RectTransform words = UiFactory.Column(row, "Qué es", 0, 4);
                words.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                UiFactory.Label(words, presenter.Text("ui.dinero.seguro_cesantia"), 40, UiFactory.PaperInk, FontStyles.Bold);
                UiFactory.Label(words, presenter.Text("ui.banco.seguro_nota"), UiFactory.MinFontSize, UiFactory.PaperMutedInk);
                TextMeshProUGUI amount = UiFactory.Label(row, bank.UnemploymentFundLabel, 40, UiFactory.PaperInk, FontStyles.Bold, Face.Mono);
                amount.alignment = TextAlignmentOptions.MidlineRight;
            }

            UiFactory.Label(_body, bank.OpenedLabel, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Italic);
        }

        /// <summary>La pestaña "Cartola": un mes con cada movimiento y su fecha, y flechas para cambiar de mes.</summary>
        private void DrawStatement(LifePresenter presenter, BankView bank)
        {
            BankTitle(bank.Kind, bank.Initials, bank.BankName, bank.AccountLabel);
            if (bank.Months.Count == 0)
            {
                UiFactory.Label(_body, presenter.Text("ui.banco.sin_cartola"), 36, UiFactory.PaperInk);
                return;
            }

            _month = Mathf.Clamp(_month, 0, bank.Months.Count - 1);
            BankMonthView month = bank.Months[_month];
            UiFactory.Label(_body, presenter.Text("ui.banco.cartola_de", month.Title), 46, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(_body, presenter.Text("ui.banco.cada_movimiento"), UiFactory.MinFontSize, UiFactory.PaperMutedInk);
            Rule();
            foreach (MoneyLineView line in month.Lines)
            {
                Line(line, line.IsIncome ? IncomeInk : UiFactory.KeyInk);
            }

            Rule();
            if (month.InLabel.Length > 0)
            {
                Line(new MoneyLineView(string.Empty, presenter.Text("ui.banco.entra"), month.InLabel, true, string.Empty), IncomeInk, bold: true);
            }

            if (month.OutLabel.Length > 0)
            {
                Line(new MoneyLineView(string.Empty, presenter.Text("ui.banco.sale"), month.OutLabel, false, string.Empty), UiFactory.KeyInk, bold: true);
            }

            // ‹ el mes de antes · el mes siguiente ›
            RectTransform arrows = Row(_body, "Meses", 16);
            Arrow(arrows, presenter.Text("ui.banco.mes_anterior"), _month < bank.Months.Count - 1, () => _month++);
            Arrow(arrows, presenter.Text("ui.banco.mes_siguiente"), _month > 0, () => _month--);
        }

        /// <summary>Sin cuenta: los bancos de tu país en tarjetas, con lo bueno, lo malo y por qué uno no te abre.</summary>
        private void DrawOffers(LifePresenter presenter)
        {
            UiFactory.Label(_body, presenter.Text("ui.banco.abrir_titulo"), 52, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            IReadOnlyList<BankOfferView> offers = presenter.BankOffers();
            BankOfferView? chosen = offers.FirstOrDefault(o => o.BankId == _chosenBank && o.CanOpen);
            foreach (BankOfferView offer in offers)
            {
                OfferCard(offer, chosen == offer);
            }

            if (chosen != null)
            {
                BankOfferView pick = chosen;
                UiFactory.MakeButton(_body, presenter.Text("ui.banco.abrir_en", pick.Name), UiFactory.Accent, UiFactory.AccentText, () => Open(pick));
            }
        }

        /// <summary>La tarjeta de un banco: su logo, su nombre, tres líneas y, si no te abre, el motivo en rojo.</summary>
        private void OfferCard(BankOfferView offer, bool chosen)
        {
            Color paper = chosen ? UiFactory.Hex(0xF7E3A1) : offer.CanOpen ? UiFactory.PaperOption : UiFactory.Hex(0xDCD6CB);
            RectTransform card = UiFactory.Column(_body, "Banco " + offer.BankId, 28, 8, paper);
            Image image = card.GetComponent<Image>();
            image.raycastTarget = true;
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = offer.CanOpen;
            button.onClick.AddListener(() =>
            {
                Sounds.Play(Sound.PencilTap);
                _chosenBank = offer.BankId;
                Redraw();
            });

            RectTransform top = Row(card, "Quién", 20);
            Badge(top, offer.Kind, offer.Initials, 84);
            RectTransform names = UiFactory.Column(top, "Nombre", 0, 2);
            names.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(names, offer.KindLabel.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold, Face.Mono);
            UiFactory.Label(names, offer.Name, 44, UiFactory.PaperInk, FontStyles.Bold);
            UiFactory.Label(card, offer.AccountLabel, 36, UiFactory.PaperInk, FontStyles.Italic);
            foreach (string line in offer.Lines)
            {
                UiFactory.Label(card, "· " + line, 36, offer.CanOpen ? UiFactory.PaperInk : UiFactory.PaperMutedInk);
            }

            if (!offer.CanOpen && offer.Reason.Length > 0)
            {
                UiFactory.Label(card, offer.Reason, 36, UiFactory.KeyInk, FontStyles.Bold);
            }
        }

        /// <summary>Pide la cuenta y muestra lo que contesta el banco.</summary>
        private void Open(BankOfferView offer)
        {
            LifePresenter presenter = _presenter!;
            BankAnswerView answer = presenter.OpenAccount(offer.BankId);
            Sounds.Play(answer.Opened ? Sound.Register : Sound.RedMark);
            if (answer.Opened)
            {
                Changed?.Invoke();
            }

            UiFactory.Clear(_body);
            _tabs.gameObject.SetActive(false);
            _body.anchoredPosition = Vector2.zero;
            UiFactory.Label(_body, presenter.Text("ui.banco.abrir_titulo"), 52, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            RectTransform card = UiFactory.Column(_body, "Respuesta del banco", 32, 14, UiFactory.PaperOption);
            RectTransform top = Row(card, "Quién", 20);
            Badge(top, offer.Kind, offer.Initials, 72);
            TextMeshProUGUI from = UiFactory.Label(top, presenter.Text("ui.banco.respuesta", offer.Name), 36, UiFactory.PaperMutedInk);
            from.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(card, answer.Title, 52, answer.Opened ? IncomeInk : UiFactory.KeyInk, FontStyles.Bold);
            UiFactory.Label(card, answer.Message, 38, UiFactory.PaperInk);

            if (answer.Opened)
            {
                UiFactory.MakeButton(_body, presenter.Text("ui.banco.ver_cuenta"), UiFactory.Accent, UiFactory.AccentText, () =>
                {
                    Sounds.Play(Sound.PageTurn);
                    _statement = false;
                    Redraw();
                });
            }
            else
            {
                _chosenBank = null;
                UiFactory.MakeButton(_body, presenter.Text("ui.boton.volver"), UiFactory.PaperOption, UiFactory.PaperInk, () =>
                {
                    Sounds.Play(Sound.PageTurn);
                    Redraw();
                });
            }
        }

        /// <summary>El logo del banco, su nombre y el tipo de cuenta, arriba del papel.</summary>
        private void BankTitle(string kind, string initials, string name, string account)
        {
            RectTransform top = Row(_body, "Tu banco", 24);
            Badge(top, kind, initials, 96);
            RectTransform names = UiFactory.Column(top, "Nombre", 0, 2);
            names.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UiFactory.Label(names, name, 52, UiFactory.PaperInk, FontStyles.Normal, Face.Title);
            UiFactory.Label(names, account, 36, UiFactory.PaperMutedInk);
        }

        /// <summary>El logo: un cuadrado redondeado del color del banco con sus iniciales.</summary>
        private static void Badge(Transform parent, string kind, string initials, int size)
        {
            RectTransform badge = UiFactory.Child(parent, "Logo");
            UiFactory.RoundedImage(badge.gameObject, KindColor(kind));
            LayoutElement layout = badge.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = size;
            layout.minHeight = layout.preferredHeight = size;
            TextMeshProUGUI text = UiFactory.Label(badge, initials, size * 2 / 5, Color.white, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(text.rectTransform);
        }

        /// <summary>Un movimiento: el nombre a la izquierda, el monto a la derecha y, debajo, la nota chica (la fecha).</summary>
        private void Line(MoneyLineView line, Color amountColor, bool bold = false)
        {
            RectTransform block = UiFactory.Column(_body, "Movimiento", 0, 2);
            RectTransform row = Row(block, "Fila", 16);
            TextMeshProUGUI name = UiFactory.Label(row, line.Label, 36, UiFactory.PaperInk, bold ? FontStyles.Bold : FontStyles.Normal);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            TextMeshProUGUI amount = UiFactory.Label(row, line.AmountLabel, 36, amountColor, FontStyles.Bold, Face.Mono);
            amount.alignment = TextAlignmentOptions.TopRight;
            // El monto nunca se corta en dos líneas: el nombre es el que se acomoda.
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            LayoutElement amountSize = amount.gameObject.AddComponent<LayoutElement>();
            amountSize.minWidth = amount.GetPreferredValues(line.AmountLabel).x;
            if (line.Note.Length > 0)
            {
                UiFactory.Label(block, line.Note, UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Italic);
            }
        }

        private void Caption(string text) =>
            UiFactory.Label(_body, text.ToUpperInvariant(), UiFactory.MinFontSize, UiFactory.PaperMutedInk, FontStyles.Bold, Face.Mono);

        /// <summary>Una raya fina entre una parte y otra.</summary>
        private void Rule()
        {
            RectTransform rule = UiFactory.Child(_body, "Raya");
            rule.gameObject.AddComponent<Image>().color = UiFactory.PaperOption;
            LayoutElement size = rule.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 3;
        }

        /// <summary>Una pestaña de abajo (Cuenta, Cartola): la que estás mirando va en amarillo.</summary>
        private void Tab(string text, bool selected, System.Action select)
        {
            Button tab = UiFactory.MakeButton(_tabs, text, selected ? UiFactory.Accent : UiFactory.Paper, selected ? UiFactory.AccentText : UiFactory.PaperInk, () =>
            {
                Sounds.Play(Sound.PageTurn);
                select();
                _month = 0;
                Redraw();
            });
            tab.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        /// <summary>Un botón chico para elegir entre mes y año.</summary>
        private void Toggle(Transform parent, string text, bool selected, System.Action select)
        {
            UiFactory.MakeButton(parent, text, selected ? UiFactory.PaperInk : UiFactory.PaperOption, selected ? UiFactory.Paper : UiFactory.PaperInk, () =>
            {
                Sounds.Play(Sound.PencilTap);
                select();
                Redraw();
            }, 34, 0, 18);
        }

        /// <summary>Una flecha para cambiar de mes; si no hay más meses para ese lado, queda apagada.</summary>
        private void Arrow(Transform parent, string text, bool enabled, System.Action move)
        {
            Button arrow = UiFactory.MakeButton(parent, text, UiFactory.PaperOption, enabled ? UiFactory.PaperInk : UiFactory.PaperMutedInk, () =>
            {
                Sounds.Play(Sound.PageTurn);
                move();
                Redraw();
            }, 34, 0, 20);
            arrow.interactable = enabled;
            arrow.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        /// <summary>Una fila horizontal que reparte a sus hijos de izquierda a derecha.</summary>
        private static RectTransform Row(Transform parent, string name, int spacing)
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
    }
}
