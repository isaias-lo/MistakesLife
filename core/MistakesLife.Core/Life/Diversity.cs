using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Reparte la orientación sexual y la identidad de género de cada persona
    /// nueva, según el campo "diversidad" de su país, y decide de qué género es
    /// la pareja que encuentra. Todo es oculto: el jugador lo va descubriendo.
    ///
    /// Usa un azar propio por persona (<c>diversidad.{id}</c>), así que agregar
    /// esto no cambia el resto de lo que pasa en una partida.
    /// </summary>
    public static class Diversity
    {
        /// <summary>
        /// Probabilidad de que la pareja de una persona bisexual sea de otro
        /// género. La mayoría lo es: 88% en EE.UU. (Pew Research, 2019).
        /// </summary>
        public const double BisexualOtherGenderChance = 0.88;

        /// <summary>Al nacer: orientación e identidad según las proporciones del país.</summary>
        public static void AssignAtBirth(Character person, CountryDefinition country, GameRandom random)
        {
            DiversityDefinition? weights = country.Diversity;
            if (weights == null)
            {
                return;
            }

            GameRandom own = OwnRandom(person, random);
            person.Orientation = PickOrientation(weights, own, DiversityDefinition.OrientationKeys);
            switch (Pick(weights.Identities, own, DiversityDefinition.IdentityKeys))
            {
                case "trans":
                    person.GenderIdentity = person.Gender == Gender.Female ? Gender.Male : Gender.Female;
                    break;
                case "no_binario":
                    person.GenderIdentity = Gender.NonBinary;
                    break;
            }
        }

        /// <summary>
        /// Para los padres del primer jugador, que lo tuvieron juntos: solo
        /// heterosexual o bisexual. Su identidad queda la de su sexo.
        /// </summary>
        public static void AssignToBirthParent(Character person, CountryDefinition country, GameRandom random)
        {
            if (country.Diversity != null)
            {
                person.Orientation = PickOrientation(country.Diversity, OwnRandom(person, random), "heterosexual", "bisexual");
            }
        }

        /// <summary>
        /// De qué género es la pareja que encuentra <paramref name="person"/>,
        /// según su orientación y su identidad.
        /// </summary>
        public static Gender PartnerGender(Character person, GameRandom random)
        {
            GameRandom own = random.Fork("diversidad.pareja");
            Gender other = person.GenderIdentity == Gender.Female ? Gender.Male
                : person.GenderIdentity == Gender.Male ? Gender.Female
                : own.Chance(0.5) ? Gender.Female : Gender.Male;
            switch (person.Orientation)
            {
                case Orientation.Homosexual:
                    return person.GenderIdentity == Gender.NonBinary ? other : person.GenderIdentity;
                case Orientation.Bisexual:
                    return own.Chance(BisexualOtherGenderChance) || person.GenderIdentity == Gender.NonBinary ? other : person.GenderIdentity;
                default:
                    return other;
            }
        }

        /// <summary>
        /// La orientación de una pareja nueva tiene que calzar con la persona:
        /// si son del mismo género, homosexual o bisexual; si no, heterosexual o bisexual.
        /// </summary>
        public static void AssignToPartner(Character partner, Character person, CountryDefinition country, GameRandom random)
        {
            bool sameGender = partner.GenderIdentity == person.GenderIdentity;
            if (person.GenderIdentity == Gender.NonBinary)
            {
                partner.Orientation = Orientation.Bisexual;
                return;
            }

            DiversityDefinition weights = country.Diversity ?? new DiversityDefinition();
            partner.Orientation = sameGender
                ? PickOrientation(weights, OwnRandom(partner, random), "homosexual", "bisexual")
                : PickOrientation(weights, OwnRandom(partner, random), "heterosexual", "bisexual");
        }

        /// <summary>Clave del lenguaje para una orientación (jugador.orientacion).</summary>
        public static string Key(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Homosexual:
                    return "homosexual";
                case Orientation.Bisexual:
                    return "bisexual";
                case Orientation.Asexual:
                    return "asexual";
                default:
                    return "heterosexual";
            }
        }

        /// <summary>Clave del lenguaje para un género (jugador.genero, jugador.identidad).</summary>
        public static string Key(Gender gender)
        {
            switch (gender)
            {
                case Gender.Female:
                    return "femenino";
                case Gender.Male:
                    return "masculino";
                default:
                    return "no_binario";
            }
        }

        /// <summary>Lee una clave de género escrita en un evento ('femenino', 'masculino', 'no_binario').</summary>
        public static bool TryParseGender(string key, out Gender gender)
        {
            switch (key)
            {
                case "femenino":
                    gender = Gender.Female;
                    return true;
                case "masculino":
                    gender = Gender.Male;
                    return true;
                case "no_binario":
                    gender = Gender.NonBinary;
                    return true;
                default:
                    gender = Gender.Female;
                    return false;
            }
        }

        private static GameRandom OwnRandom(Character person, GameRandom random) => random.Fork("diversidad." + person.Id);

        private static Orientation PickOrientation(DiversityDefinition weights, GameRandom random, params string[] allowed) =>
            PickOrientation(weights, random, (IReadOnlyList<string>)allowed);

        private static Orientation PickOrientation(DiversityDefinition weights, GameRandom random, IReadOnlyList<string> allowed)
        {
            switch (Pick(weights.Orientations, random, allowed))
            {
                case "homosexual":
                    return Orientation.Homosexual;
                case "bisexual":
                    return Orientation.Bisexual;
                case "asexual":
                    return Orientation.Asexual;
                default:
                    return Orientation.Heterosexual;
            }
        }

        /// <summary>Elige una de las claves permitidas según su peso. Si ninguna tiene peso, la primera.</summary>
        private static string Pick(Dictionary<string, int> weights, GameRandom random, IReadOnlyList<string> allowed)
        {
            List<string> keys = allowed.Where(k => weights.TryGetValue(k, out int w) && w > 0).ToList();
            return keys.Count == 0 ? allowed[0] : keys[random.PickWeightedIndex(keys.Select(k => weights[k]).ToList())];
        }
    }
}
