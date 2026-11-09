#nullable enable

using MistakesLife.Core.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// "Tu dinero" (docs/diseno-app.md): una hoja aparte, encima de la vida. Por ahora tiene
    /// la pestaña "El mes": la frase con los días de trabajo que costó el mes, el talonario
    /// (un boleto por día de sueldo, del color del gasto que se lo llevó; los días prestados
    /// van en rojo y rayados como un pagaré) y, debajo, a qué se fue cada día.
    /// Solo dibuja: los números vienen de <see cref="LifePresenter.Month"/>.
    /// </summary>
    public sealed class MoneySheet
    {
        /// <summary>Boletos por fila del talonario (22 días de sueldo = 2 filas).</summary>
        private const int TicketsPerRow = 11;

        /// <summary>Un color por gasto, en el orden de la lista (del más caro al más barato).</summary>
        private static readonly Color[] ExpenseColors =
        {
            UiFactory.Hex(0x7FA7FF), UiFactory.Hex(0x4FD1A5), UiFactory.Hex(0xF2A65A), UiFactory.Hex(0xC98BDB),
            UiFactory.Hex(0x6FC7D9), UiFactory.Hex(0xE3C27A), UiFactory.Hex(0x9AAE6B), UiFactory.Hex(0xB89A84),
        };

        private static readonly Color Borrowed = UiFactory.Hex(0xD8594A);

        private readonly RectTransform _layer;
        private readonly TextMeshProUGUI _title;
        private readonly Button _close;
        private readonly Button _monthTab;
        private readonly RectTransform _body;

        public MoneySheet(RectTransform parent)
        {
            _layer = UiFactory.Column(parent, "Tu dinero", 28, 16, UiFactory.Background);
            UiFactory.Stretch(_layer);

            RectTransform header = Row(_layer, "Titulo", 16);
            _title = UiFactory.Label(header, string.Empty, 52, UiFactory.Ink, FontStyles.Normal, Face.Title);
            _title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            _close = UiFactory.MakeButton(header, string.Empty, UiFactory.Option, UiFactory.Ink, Hide, UiFactory.MinFontSize, 0, 12);
            LayoutElement close = _close.GetComponent<LayoutElement>();
            close.minWidth = close.preferredWidth = 200;

            // Pestañas: "La mochila" y "Tu historia" llegan después (U19b y U19c).
            RectTransform tabs = Row(_layer, "Pestanas", 12);
            _monthTab = UiFactory.MakeButton(tabs, string.Empty, UiFactory.Accent, UiFactory.AccentText, () => { }, UiFactory.MinFontSize, 0, 12);
            LayoutElement tab = _monthTab.GetComponent<LayoutElement>();
            tab.minWidth = tab.preferredWidth = 240;

            _body = UiFactory.ScrollColumn(_layer, "Contenido", 28, 18);
            _layer.gameObject.SetActive(false);
        }

        public bool IsOpen => _layer.gameObject.activeSelf;

        public void Show(LifePresenter presenter)
        {
            _layer.gameObject.SetActive(true);
            _layer.SetAsLastSibling();
            Draw(presenter);
        }

        public void Hide() => _layer.gameObject.SetActive(false);

        /// <summary>Vuelve a dibujar con el último mes de la partida.</summary>
        public void Draw(LifePresenter presenter)
        {
            _title.text = presenter.Text("ui.boton.tu_dinero");
            _close.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.boton.cerrar");
            _monthTab.GetComponentInChildren<TextMeshProUGUI>().text = presenter.Text("ui.dinero.el_mes");

            MonthView month = presenter.Month();
            UiFactory.Clear(_body);
            _body.anchoredPosition = Vector2.zero;
            UiFactory.Label(_body, month.Headline, 46, UiFactory.Ink, FontStyles.Normal, Face.Title);
            if (month.PayLabel.Length > 0)
            {
                UiFactory.Label(_body, month.PayLabel, UiFactory.MinFontSize, UiFactory.MutedInk);
            }

            if (month.HasPay)
            {
                DrawTickets(month);
            }

            if (month.Footnote.Length > 0)
            {
                UiFactory.Label(_body, month.Footnote, UiFactory.MinFontSize, month.BorrowedDays > 0 ? Borrowed : UiFactory.Good, FontStyles.Bold);
            }

            for (int i = 0; i < month.Expenses.Count; i++)
            {
                DrawExpense(month.Expenses[i], ExpenseColors[i % ExpenseColors.Length]);
            }
        }

        /// <summary>El talonario: una grilla de boletos, uno por día.</summary>
        private void DrawTickets(MonthView month)
        {
            RectTransform book = UiFactory.Child(_body, "Talonario");
            GridLayoutGroup grid = book.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(76f, 104f);
            grid.spacing = new Vector2(8f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = TicketsPerRow;
            grid.childAlignment = TextAnchor.UpperCenter;

            for (int day = 0; day < month.Tickets.Count; day++)
            {
                int expense = month.Tickets[day];
                bool borrowed = day >= MonthView.WorkDaysPerMonth;
                Color color = borrowed ? Borrowed
                    : expense == MonthView.FreeTicket ? new Color(UiFactory.Paper.r, UiFactory.Paper.g, UiFactory.Paper.b, 0.18f)
                    : ExpenseColors[expense % ExpenseColors.Length];
                RectTransform ticket = UiFactory.Child(book, borrowed ? "Pagare" : "Boleto");
                UiFactory.RoundedImage(ticket.gameObject, color);
                TextMeshProUGUI number = UiFactory.Label(ticket, (day + 1).ToString(), 28, borrowed || expense != MonthView.FreeTicket ? UiFactory.AccentText : UiFactory.MutedInk, FontStyles.Bold);
                UiFactory.Stretch(number.rectTransform);
                number.alignment = TextAlignmentOptions.Center;
                if (borrowed)
                {
                    // El pagaré va rayado: una línea cruzada, como un papel anulado.
                    RectTransform stripe = UiFactory.Child(ticket, "Raya");
                    stripe.gameObject.AddComponent<Image>().color = UiFactory.Background;
                    stripe.sizeDelta = new Vector2(110f, 6f);
                    stripe.localRotation = Quaternion.Euler(0f, 0f, 55f);
                }
            }
        }

        /// <summary>Una línea por gasto: su color, el nombre, los días y lo que costó.</summary>
        private void DrawExpense(MonthExpenseView expense, Color color)
        {
            RectTransform line = Row(_body, expense.Id, 16);
            RectTransform chip = UiFactory.Child(line, "Color");
            UiFactory.RoundedImage(chip.gameObject, color);
            LayoutElement size = chip.gameObject.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = 36;
            size.minHeight = size.preferredHeight = 36;

            TextMeshProUGUI name = UiFactory.Label(line, expense.Label, UiFactory.MinFontSize, UiFactory.Ink);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            string right = expense.DaysLabel.Length > 0 ? $"{expense.DaysLabel} · {expense.AmountLabel}" : expense.AmountLabel;
            UiFactory.Label(line, right, UiFactory.MinFontSize, UiFactory.MutedInk, FontStyles.Bold, Face.Mono);
        }

        private static RectTransform Row(RectTransform parent, string name, float spacing)
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
