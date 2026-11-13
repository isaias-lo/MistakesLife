using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// La herencia, con las reglas del país donde vivía quien murió ("economia.herencia"):
    /// - Los bienes son su dinero y lo ahorrado para la pensión; las deudas que no
    ///   se extinguen al morir se restan.
    /// - Heredan los hijos vivos y la pareja (que recibe "porciones_pareja" partes de
    ///   un hijo). Sin hijos ni pareja, heredan los padres vivos. Sin nadie, lo
    ///   recibe el Estado (se pierde).
    /// - Si las deudas son más que los bienes: con beneficio de inventario, nadie
    ///   hereda ni paga; sin él, cada heredero paga su parte de lo que falta.
    /// - Si el país pide un trámite ("posesion_efectiva"), lo heredado queda pendiente
    ///   (Character.PendingInheritance) hasta que un evento lo entregue.
    /// </summary>
    public static class InheritanceSettlement
    {
        public const string ReceivedJournal = "diario.herencia.recibida";
        public const string DebtsJournal = "diario.herencia.deudas";
        public const string PendingJournal = "diario.herencia.pendiente";

        /// <summary>Reparte lo que dejó alguien que acaba de morir (antes de que termine su pareja).</summary>
        public static void Settle(World world, ContentDatabase content, Character deceased)
        {
            if (deceased.Residence == null)
            {
                return;
            }

            EconomyDefinition? rules = content.GetCountry(deceased.Residence.CountryId).Economy;
            CountryEconomy? economy = world.EconomyOf(deceased.Residence.CountryId);
            double priceIndex = economy?.PriceIndex ?? 1;
            decimal debts = 0;
            foreach (Debt debt in deceased.Debts.ToList())
            {
                DebtTypeDefinition? type = rules?.Debts?.Type(debt.TypeId);
                if (type == null || !type.EndsAtDeath)
                {
                    debts += debt.Balance;
                }

                deceased.RemoveDebt(debt);
            }

            decimal estate = deceased.Money + PayrollCalculator.ToToday(deceased.PensionFund, priceIndex) - debts;
            deceased.Money = 0;
            deceased.PensionFund = 0;
            deceased.Delinquent = false;
            InheritanceDefinition? inheritance = rules?.Inheritance;
            if (inheritance == null || estate == 0 || (estate < 0 && inheritance.LimitedToEstate))
            {
                return;
            }

            List<KeyValuePair<Character, double>> heirs = Heirs(world, deceased, inheritance);
            double totalShares = heirs.Sum(h => h.Value);
            if (totalShares <= 0)
            {
                return;
            }

            foreach (KeyValuePair<Character, double> heir in heirs)
            {
                decimal share = Math.Round(estate * (decimal)(heir.Value / totalShares), MidpointRounding.AwayFromZero);
                if (share > 0 && inheritance.NeedsProceedings)
                {
                    // Queda pendiente (en dinero del inicio, así no pierde valor) hasta el trámite.
                    heir.Key.PendingInheritance += share / (decimal)(priceIndex > 0 ? priceIndex : 1);
                    world.WriteJournal(heir.Key.Id, PendingJournal);
                    continue;
                }

                heir.Key.Money += share;
                world.WriteJournal(heir.Key.Id, share >= 0 ? ReceivedJournal : DebtsJournal);
            }
        }

        /// <summary>Los herederos y cuántas partes recibe cada uno.</summary>
        public static List<KeyValuePair<Character, double>> Heirs(World world, Character deceased, InheritanceDefinition rules)
        {
            var heirs = new List<KeyValuePair<Character, double>>();
            foreach (Character child in world.ChildrenOf(deceased.Id).Where(c => c.IsAlive).OrderBy(c => c.Id))
            {
                heirs.Add(new KeyValuePair<Character, double>(child, 1));
            }

            if (deceased.PartnerId.HasValue && world.TryGetCharacter(deceased.PartnerId.Value, out Character? partner) && partner.IsAlive)
            {
                heirs.Add(new KeyValuePair<Character, double>(partner, heirs.Count == 0 ? 1 : rules.PartnerShares));
            }

            if (heirs.Count == 0)
            {
                foreach (int? parentId in new[] { deceased.MotherId, deceased.FatherId })
                {
                    if (parentId.HasValue && world.TryGetCharacter(parentId.Value, out Character? parent) && parent.IsAlive)
                    {
                        heirs.Add(new KeyValuePair<Character, double>(parent, 1));
                    }
                }
            }

            return heirs;
        }
    }
}
