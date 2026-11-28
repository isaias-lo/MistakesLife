using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Un banco donde puedes abrir tu cuenta (D6-1c): su tarjeta de papel con lo bueno y lo malo
    /// y, si no cumples lo que pide, por qué (y no se puede elegir).
    /// </summary>
    public sealed class BankOfferView
    {
        public BankOfferView(string bankId, string kind, string name, string initials, string kindLabel, string accountLabel, IReadOnlyList<string> lines, bool canOpen, string reason)
        {
            BankId = bankId;
            Kind = kind;
            Name = name;
            Initials = initials;
            KindLabel = kindLabel;
            AccountLabel = accountLabel;
            Lines = lines;
            CanOpen = canOpen;
            Reason = reason;
        }

        /// <summary>El id del banco, para <see cref="LifePresenter.OpenAccount"/>.</summary>
        public string BankId { get; }

        /// <summary>"estatal", "grande" o "digital" (para el color de su logo).</summary>
        public string Kind { get; }

        /// <summary>"Banco Pacífico".</summary>
        public string Name { get; }

        /// <summary>"BP": las iniciales para su logo.</summary>
        public string Initials { get; }

        /// <summary>"Del Estado", "Grande" o "Digital".</summary>
        public string KindLabel { get; }

        /// <summary>"Cuenta corriente".</summary>
        public string AccountLabel { get; }

        /// <summary>Lo bueno y lo malo, una frase por línea.</summary>
        public IReadOnlyList<string> Lines { get; }

        /// <summary>Si te abriría la cuenta hoy.</summary>
        public bool CanOpen { get; }

        /// <summary>Por qué no ("Pide un sueldo líquido de $600.000; ganas $450.000"); vacío si puedes.</summary>
        public string Reason { get; }
    }
}
