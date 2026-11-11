using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Reglas del arriendo: firmar un contrato con el precio de la comuna, dejar la
    /// garantía, renovarlo, irse (y recuperar la garantía) y pagar cada mes con su
    /// reajuste. Todo lo decide el jugador en los eventos; aquí solo se aplica.
    /// </summary>
    public static class HousingRules
    {
        /// <summary>Marca de quien vive fuera de la casa de su familia (la usan los gastos y los eventos).</summary>
        public const string LivesOnOwnFlag = "vive_independiente";

        /// <summary>Las reglas de arriendo del país donde vive, o null si ahí no se arrienda.</summary>
        public static HousingDefinition? RulesFor(ContentDatabase content, Character character) =>
            character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Economy?.Housing;

        /// <summary>Arriendo típico de un contrato nuevo de ese tipo en la comuna donde vive, en dinero del inicio del mundo.</summary>
        public static decimal MarketRent(ContentDatabase content, Character character, string typeId)
        {
            if (character.Residence == null)
            {
                throw new ScriptException($"arriendo('{typeId}'): el personaje no vive en ninguna parte.");
            }

            string communeId = character.Residence.CommuneId;
            CommuneDefinition commune = content.GetCountry(character.Residence.CountryId).Cities
                .SelectMany(c => c.Communes)
                .FirstOrDefault(c => c.Id == communeId)
                ?? throw new ScriptException($"arriendo('{typeId}'): la comuna {communeId} no existe.");
            return commune.RentOf(typeId) ?? throw new ScriptException($"arriendo('{typeId}'): la comuna {communeId} no tiene ese tipo de vivienda.");
        }

        /// <summary>
        /// Firma un contrato nuevo en la comuna donde vive: deja un contrato anterior
        /// (y recupera su garantía), paga la garantía del nuevo y queda viviendo por su cuenta.
        /// </summary>
        public static Lease Sign(World world, ContentDatabase content, Character character, string typeId)
        {
            HousingDefinition rules = RulesFor(content, character)
                ?? throw new ScriptException($"jugador.arrendar('{typeId}'): en su país no se arrienda (falta economia.vivienda).");
            if (rules.Type(typeId) == null)
            {
                throw new ScriptException($"jugador.arrendar('{typeId}'): ese tipo de vivienda no existe en su país.");
            }

            decimal baseRent = MarketRent(content, character, typeId);
            double priceIndex = world.EconomyOf(character.Residence!.CountryId)?.PriceIndex ?? 1;
            Leave(character);
            decimal deposit = PayrollCalculator.ToToday(baseRent, priceIndex) * rules.DepositMonths;
            character.Money -= deposit;
            character.Lease = new Lease(typeId, character.Residence.CommuneId, baseRent, priceIndex, rules.TermMonths, deposit);
            character.SetFlag(LivesOnOwnFlag);
            return character.Lease;
        }

        /// <summary>
        /// Contrato que ya tenía alguien que el jugador no vivió de cerca (vida_previa
        /// en el legado): al precio de hoy de su comuna, sin cobrar garantía.
        /// </summary>
        public static Lease AssumeLease(World world, ContentDatabase content, Character character, string typeId)
        {
            HousingDefinition rules = RulesFor(content, character)
                ?? throw new ScriptException($"vida_previa: en su país no se arrienda (falta economia.vivienda).");
            decimal baseRent = MarketRent(content, character, typeId);
            double priceIndex = world.EconomyOf(character.Residence!.CountryId)?.PriceIndex ?? 1;
            character.Lease = new Lease(typeId, character.Residence.CommuneId, baseRent, priceIndex, rules.TermMonths, 0);
            character.SetFlag(LivesOnOwnFlag);
            return character.Lease;
        }

        /// <summary>Renueva el contrato por otro plazo, con el mismo arriendo (sigue reajustándose).</summary>
        public static void Renew(Character character, HousingDefinition rules)
        {
            if (character.Lease == null)
            {
                throw new ScriptException("jugador.renovar_arriendo(): no tiene contrato de arriendo (revisa jugador.vivienda).");
            }

            character.Lease.MonthsLeft = rules.TermMonths;
        }

        /// <summary>Deja el arriendo: recupera la garantía y ya no vive por su cuenta.</summary>
        public static void Leave(Character character)
        {
            if (character.Lease == null)
            {
                return;
            }

            character.Money += character.Lease.Deposit;
            character.Lease = null;
            character.ClearFlag(LivesOnOwnFlag);
        }

        /// <summary>
        /// El arriendo de este mes: reajusta con la inflación cuando toca, descuenta
        /// un mes del contrato y devuelve lo que se paga (a precios de hoy).
        /// </summary>
        public static decimal MonthlyRent(ContentDatabase content, Character character, HousingDefinition rules, double priceIndex)
        {
            Lease lease = character.Lease!;
            if (lease.BaseRent == 0)
            {
                // Contrato de un guardado viejo: toma el precio de la comuna de hoy.
                lease.BaseRent = MarketRent(content, character, lease.TypeId);
                lease.AdjustedPriceIndex = priceIndex;
            }
            else if (++lease.MonthsSinceAdjustment >= rules.AdjustEveryMonths)
            {
                lease.AdjustedPriceIndex = priceIndex;
                lease.MonthsSinceAdjustment = 0;
            }

            if (lease.MonthsLeft > 0)
            {
                lease.MonthsLeft--;
            }

            return lease.MonthlyRent;
        }
    }
}
