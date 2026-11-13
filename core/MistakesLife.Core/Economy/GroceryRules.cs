using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// Reglas de la compra de comida: el jugador elige qué compra hace (de
    /// "economia.supermercado") y cada mes paga esa compra para todos los de su
    /// casa: él, su pareja y sus hijos menores. Con pareja se divide a medias.
    /// Solo se cobra mientras vive por su cuenta: en la casa de su familia, la
    /// comida va en el aporte a la casa.
    /// </summary>
    public static class GroceryRules
    {
        /// <summary>Las compras del país donde vive, o null si ahí la comida no se cobra aparte.</summary>
        public static GroceriesDefinition? RulesFor(ContentDatabase content, Character character) =>
            character.Residence == null ? null : content.GetCountry(character.Residence.CountryId).Economy?.Groceries;

        /// <summary>
        /// Lo que le toca pagar al mes por esa compra, en dinero del inicio del mundo:
        /// el costo por persona por los que comen en su casa (los hijos que viven con él, con el factor
        /// del país), dividido a medias si tiene pareja.
        /// </summary>
        public static decimal MonthlyShare(World world, ContentDatabase content, Character character, string basketId)
        {
            GroceryBasketDefinition basket = Basket(content, character, basketId, "compra");
            GroceriesDefinition rules = RulesFor(content, character)!;
            bool hasPartner = character.PartnerId.HasValue && world.TryGetCharacter(character.PartnerId.Value, out Character? partner) && partner.IsAlive;
            int children = Life.CustodyRules.ChildrenAtHome(world, character);
            decimal eaters = 1 + (hasPartner ? 1 : 0) + (children * rules.ChildFactor);
            decimal total = basket.PerPerson * eaters;
            return Math.Round(hasPartner ? total / 2 : total, MidpointRounding.AwayFromZero);
        }

        /// <summary>El jugador decide qué compra hace desde ahora.</summary>
        public static void Choose(ContentDatabase content, Character character, string basketId)
        {
            Basket(content, character, basketId, "jugador.elegir_compra");
            character.GroceryBasketId = basketId;
        }

        private static GroceryBasketDefinition Basket(ContentDatabase content, Character character, string basketId, string where)
        {
            GroceriesDefinition rules = RulesFor(content, character)
                ?? throw new ScriptException($"{where}('{basketId}'): en su país la comida no se elige (falta economia.supermercado).");
            return rules.Basket(basketId)
                ?? throw new ScriptException($"{where}('{basketId}'): esa compra no existe en su país.");
        }
    }
}
