using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Reglas del banco: quién puede abrir una cuenta, qué pasa al abrirla y cuánto cobra al
    /// mes. Los bancos y sus requisitos vienen de "economia.bancos" del país.
    /// </summary>
    public static class BankRules
    {
        /// <summary>El movimiento con que la plata que tenía en efectivo entra a la cuenta nueva.</summary>
        public const string OpeningDeposit = "movimiento.deposito_inicial";

        /// <summary>El movimiento de lo que cobra el banco al mes.</summary>
        public const string MonthlyFeeMovement = "movimiento.mantencion";

        /// <summary>Por qué el banco no abre la cuenta (null si sí la abre).</summary>
        public enum Refusal
        {
            None,
            Age,
            Contract,
            Income,
        }

        /// <summary>Los bancos del país donde vive, o null si ahí no hay (se maneja efectivo).</summary>
        public static BanksDefinition? BanksFor(ContentDatabase content, Character character) =>
            character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Economy?.Banks;

        /// <summary>
        /// Si maneja efectivo: no tiene cuenta. En un país sin bancos cargados, maneja efectivo
        /// solo de niño (hasta los 18 y sin trabajo), como antes de que existiera el banco.
        /// </summary>
        public static bool UsesCash(ContentDatabase content, Character character, GameDate date)
        {
            if (character.Account != null)
            {
                return false;
            }

            return BanksFor(content, character) != null || (character.AgeInYears(date) < 18 && character.Job == null);
        }

        /// <summary>Qué le diría ese banco si pide abrir una cuenta hoy.</summary>
        public static Refusal Check(World world, BanksDefinition banks, BankDefinition bank, Character character)
        {
            if (character.AgeInYears(world.Date) < banks.MinAge)
            {
                return Refusal.Age;
            }

            if (bank.NeedsContract && character.Job?.Kind != JobKind.Employee)
            {
                return Refusal.Contract;
            }

            double index = character.Residence == null ? 1 : world.EconomyOf(character.Residence.CountryId)?.PriceIndex ?? 1;
            decimal net = character.Job != null ? character.LastPayslip?.Net ?? 0 : 0;
            if (bank.MinIncome > 0 && net < PayrollCalculator.ToToday(bank.MinIncome, index))
            {
                return Refusal.Income;
            }

            return Refusal.None;
        }

        /// <summary>
        /// Pide abrir una cuenta en ese banco. Si el banco dice que sí, la plata que tenía en
        /// efectivo entra como primer movimiento; si dice que no, queda en el diario con el motivo.
        /// Devuelve si se abrió.
        /// </summary>
        public static bool Open(World world, ContentDatabase content, Character character, string bankId)
        {
            BanksDefinition banks = BanksFor(content, character)
                ?? throw new ScriptException($"jugador.abrir_cuenta('{bankId}'): en su país no hay bancos (falta economia.bancos).");
            BankDefinition bank = banks.Bank(bankId)
                ?? throw new ScriptException($"jugador.abrir_cuenta('{bankId}'): ese banco no existe en su país.");
            if (character.Account != null)
            {
                return false;
            }

            Refusal refusal = Check(world, banks, bank, character);
            if (refusal != Refusal.None)
            {
                string? journal = refusal == Refusal.Contract ? banks.RefusedContractJournal
                    : refusal == Refusal.Income ? banks.RefusedIncomeJournal
                    : banks.RefusedAgeJournal;
                if (journal != null)
                {
                    world.WriteJournal(character.Id, journal);
                }

                return false;
            }

            character.Account = new BankAccount(bank.Id, world.Date);
            if (character.Money != 0)
            {
                // La plata ya la tenía: solo queda anotada como la primera línea de la cartola.
                character.Account.Add(new BankMovement(world.Date, OpeningDeposit, character.Money, ""));
            }

            if (banks.OpenedJournal != null)
            {
                world.WriteJournal(character.Id, banks.OpenedJournal);
            }

            return true;
        }

        /// <summary>Lo que cobra su banco este mes por la cuenta, a los precios de hoy (0 si no tiene cuenta o no cobra).</summary>
        public static decimal MonthlyFee(ContentDatabase content, Character character, double priceIndex)
        {
            BankDefinition? bank = character.Account == null ? null : BanksFor(content, character)?.Bank(character.Account.BankId);
            return bank == null ? 0 : PayrollCalculator.ToToday(bank.MonthlyFee, priceIndex);
        }

        /// <summary>El banco de su cuenta, o null.</summary>
        public static BankDefinition? BankOf(ContentDatabase content, Character character) =>
            character.Account == null ? null : BanksFor(content, character)?.Banks.FirstOrDefault(b => b.Id == character.Account.BankId);
    }
}
