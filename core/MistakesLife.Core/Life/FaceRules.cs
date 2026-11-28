using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Las reglas de la cara (acuerdo con Isaias del 28-11-2026, "La gente y su cara" en
    /// docs/diseno-juego.md). Cada hijo saca rasgos de su mamá y su papá:
    /// <list type="bullet">
    /// <item>Ojos y color de pelo: trae un color de cada uno y se ve el más oscuro, así dos papás
    /// de ojos café pueden tener un hijo de ojos azules si los dos lo traen escondido.</item>
    /// <item>Piel, forma de la cara, cuándo salen las canas: cerca del promedio de los dos.</item>
    /// <item>Cejas gruesas: de uno de los dos. La miopía es más común si un papá es miope.</item>
    /// </list>
    /// Quien no tiene papás en el mundo saca su cara de cómo es la gente de su país
    /// (campo "rostros" de country.json). El peinado, la barba y los lentes se eligen.
    ///
    /// Cada persona usa su propio azar (<c>rostro.{id}</c>, a partir de la semilla de la
    /// partida): su cara no depende de cuándo se calcula ni cambia el azar del resto del juego.
    /// </summary>
    public static class FaceRules
    {
        /// <summary>Cuántos tonos de piel tiene la pieza de Blender (de muy claro a muy oscuro).</summary>
        public const int SkinBands = 6;

        /// <summary>Cuánto puede alejarse la piel de un hijo del promedio de sus papás (más o menos).</summary>
        public const int SkinSpread = 8;

        /// <summary>Cuánto puede alejarse la forma de la cara de un hijo del promedio de sus papás.</summary>
        public const int ShapeSpread = 15;

        /// <summary>Cuántos años pueden separar las canas de un hijo del promedio de sus papás.</summary>
        public const int GraySpread = 4;

        /// <summary>Entre qué edades salen las canas a quien no tiene papás en el mundo (estimación).</summary>
        public const int GrayMin = 35;
        public const int GrayMax = 60;

        /// <summary>Probabilidad de cejas gruesas en quien no tiene papás en el mundo (estimación).</summary>
        public const double ThickBrowsChance = 0.35;

        /// <summary>
        /// Cuánto sube la miopía con papás miopes (estimación de estudios de familias: cerca del
        /// doble con uno y el triple con los dos). Ver SOURCES.md.
        /// </summary>
        public const double MyopiaPerMyopicParent = 1.0;

        /// <summary>Lo más probable que llega a ser la miopía, aun con los dos papás miopes.</summary>
        public const double MaxMyopiaChance = 0.8;

        /// <summary>Un papá "miope" es el que necesitó lentes antes de esta edad.</summary>
        public const int MyopicBefore = 30;

        /// <summary>
        /// Quienes no son miopes igual necesitan lentes cerca de los 45 por la vista cansada; muchos
        /// los usan solo para leer. Esta es la parte que se los deja puestos (estimación).
        /// </summary>
        public const double ReadingGlassesChance = 0.5;

        /// <summary>Desde qué edad se le ve la barba a quien la eligió.</summary>
        public const int BeardFromAge = 16;

        /// <summary>
        /// La cara de una persona, calculándola si todavía no la tiene (primero la de sus papás).
        /// </summary>
        public static Face Ensure(World world, ContentDatabase content, Character person)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (person.Face == null)
            {
                person.Face = Create(world, content, person);
            }

            return person.Face;
        }

        /// <summary>
        /// Si el jugador puede armar su cara pieza por pieza: solo en la primera vida y antes de
        /// cumplir un año. Al jugar con un hijo, su cara ya viene de sus papás y solo cambia el estilo.
        /// </summary>
        public static bool CanChooseFeatures(World world) =>
            world.Generation == 1 && world.Player.AgeInYears(world.Date) == 0;

        /// <summary>
        /// El jugador arma su cara pieza por pieza. Sus papás y hermanos se vuelven a armar para
        /// que esta cara pueda haber salido de ellos (cada papá trae uno de sus colores de ojos y
        /// de pelo, y su piel y su forma se parecen).
        /// </summary>
        public static void ChooseFeatures(World world, ContentDatabase content, Face face)
        {
            if (!CanChooseFeatures(world))
            {
                throw new InvalidOperationException("La cara solo se arma pieza por pieza al empezar la primera vida.");
            }

            Character player = world.Player;
            Face chosen = face.Copy();
            chosen.Chosen = true;
            player.Face = chosen;

            foreach (Character relative in BloodFamily(world, player))
            {
                relative.Face = null;
            }

            foreach (Character relative in BloodFamily(world, player))
            {
                Ensure(world, content, relative);
            }
        }

        /// <summary>El peinado, la barba y los lentes del jugador (se pueden cambiar en cualquier vida).</summary>
        public static void ChooseStyle(World world, ContentDatabase content, HairStyle hair, BeardStyle beard, bool? glasses)
        {
            Face face = Ensure(world, content, world.Player);
            face.HairStyle = hair;
            face.Beard = beard;
            face.GlassesChoice = glasses;
        }

        /// <summary>
        /// Una cara al azar como las de la gente del país del jugador, para el botón "Al azar" del
        /// editor. <paramref name="roll"/> es cuántas veces se apretó el botón: cada vez sale otra.
        /// </summary>
        public static Face RandomFace(World world, ContentDatabase content, int roll)
        {
            Character player = world.Player;
            GameRandom random = new GameRandom(world.Seed).Fork("rostro.azar." + roll);
            FacePopulationDefinition? people = PeopleOf(world, content, player);
            Face face = FromPeople(people, random, 0);
            PickStyle(face, player, random);
            return face;
        }

        /// <summary>La barba que se le ve a esta edad (antes de los 16, ninguna).</summary>
        public static BeardStyle BeardAt(Face face, int ageInYears) =>
            ageInYears < BeardFromAge ? BeardStyle.None : face.Beard;

        /// <summary>
        /// Qué parte de quienes traen cada color (de ojos o de pelo) se espera en el país para que
        /// se vea lo que dicen sus porcentajes. Como se ve el más oscuro de dos, un color claro
        /// que se ve en el 9% de la gente lo trae escondido cerca del 30%.
        /// </summary>
        public static double[] CarrierShares(IReadOnlyList<int> seen)
        {
            double total = seen.Sum();
            double[] shares = new double[seen.Count];
            double tailFromNext = 0;
            for (int i = seen.Count - 1; i >= 0; i--)
            {
                double tail = 0;
                for (int j = i; j < seen.Count; j++)
                {
                    tail += seen[j];
                }

                double carriers = Math.Sqrt(tail / total);
                shares[i] = carriers - tailFromNext;
                tailFromNext = carriers;
            }

            return shares;
        }

        private static Face Create(World world, ContentDatabase content, Character person)
        {
            GameRandom random = new GameRandom(world.Seed).Fork("rostro." + person.Id);
            FacePopulationDefinition? people = PeopleOf(world, content, person);

            // Un hijo adoptado no hereda de quienes lo adoptaron: sus papás biológicos no están en el mundo.
            Face? mother = person.Adopted ? null : ParentFace(world, content, person.MotherId);
            Face? father = person.Adopted ? null : ParentFace(world, content, person.FatherId);

            Face face;
            Face? child = ChosenChild(world, person);
            if (mother == null && father == null && child != null)
            {
                face = FromChild(child, person.Id == world.Player.MotherId, people, random);
            }
            else if (mother == null && father == null)
            {
                face = FromPeople(people, random, MyopicParents(null, null));
            }
            else
            {
                face = FromParents(mother, father, people, random);
            }

            PickStyle(face, person, random);
            return face;
        }

        private static Face? ParentFace(World world, ContentDatabase content, int? parentId)
        {
            if (!parentId.HasValue || !world.TryGetCharacter(parentId.Value, out Character? parent))
            {
                return null;
            }

            return Ensure(world, content, parent);
        }

        /// <summary>La cara que eligió el jugador, si esta persona es su mamá o su papá biológico.</summary>
        private static Face? ChosenChild(World world, Character person)
        {
            Character player = world.Player;
            if (player.Face == null || !player.Face.Chosen || player.Adopted || player.Id == person.Id)
            {
                return null;
            }

            return player.MotherId == person.Id || player.FatherId == person.Id ? player.Face : null;
        }

        private static Face FromPeople(FacePopulationDefinition? people, GameRandom random, int myopicParents)
        {
            Face face = new Face
            {
                SkinTone = PeopleSkin(people, random),
                EyesFromMother = (EyeColor)PeopleColor(people?.Eyes, 4, random),
                EyesFromFather = (EyeColor)PeopleColor(people?.Eyes, 4, random),
                HairFromMother = (HairColor)PeopleColor(people?.Hair, 5, random),
                HairFromFather = (HairColor)PeopleColor(people?.Hair, 5, random),
                Width = PeopleShape(random),
                Chin = PeopleShape(random),
                Nose = PeopleShape(random),
                Lips = PeopleShape(random),
                Cheekbones = PeopleShape(random),
                EyeSize = PeopleShape(random),
                ThickBrows = random.Chance(ThickBrowsChance),
                GrayFromAge = (random.NextInt(GrayMin, GrayMax + 1) + random.NextInt(GrayMin, GrayMax + 1)) / 2,
            };
            face.GlassesFromAge = Glasses(people, random, myopicParents);
            return face;
        }

        private static Face FromParents(Face? mother, Face? father, FacePopulationDefinition? people, GameRandom random)
        {
            // Si uno de los dos no está en el mundo, su parte sale de cómo es la gente del país.
            Face mom = mother ?? FromPeople(people, random.Fork("mama"), 0);
            Face dad = father ?? FromPeople(people, random.Fork("papa"), 0);
            Face face = new Face
            {
                SkinTone = Near((mom.SkinTone + dad.SkinTone) / 2, SkinSpread, random),
                EyesFromMother = random.Chance(0.5) ? mom.EyesFromMother : mom.EyesFromFather,
                EyesFromFather = random.Chance(0.5) ? dad.EyesFromMother : dad.EyesFromFather,
                HairFromMother = random.Chance(0.5) ? mom.HairFromMother : mom.HairFromFather,
                HairFromFather = random.Chance(0.5) ? dad.HairFromMother : dad.HairFromFather,
                Width = Near((mom.Width + dad.Width) / 2, ShapeSpread, random),
                Chin = Near((mom.Chin + dad.Chin) / 2, ShapeSpread, random),
                Nose = Near((mom.Nose + dad.Nose) / 2, ShapeSpread, random),
                Lips = Near((mom.Lips + dad.Lips) / 2, ShapeSpread, random),
                Cheekbones = Near((mom.Cheekbones + dad.Cheekbones) / 2, ShapeSpread, random),
                EyeSize = Near((mom.EyeSize + dad.EyeSize) / 2, ShapeSpread, random),
                ThickBrows = random.Chance(0.5) ? mom.ThickBrows : dad.ThickBrows,
                GrayFromAge = Math.Max(25, Math.Min(75, ((mom.GrayFromAge + dad.GrayFromAge) / 2) + random.NextInt(-GraySpread, GraySpread + 1))),
            };
            face.GlassesFromAge = Glasses(people, random, MyopicParents(mother, father));
            return face;
        }

        /// <summary>
        /// La cara de una mamá o un papá armada "al revés", desde la cara que eligió su hijo:
        /// le pasó uno de sus colores, y su piel y su forma se le parecen.
        /// </summary>
        private static Face FromChild(Face child, bool isMother, FacePopulationDefinition? people, GameRandom random)
        {
            Face face = FromPeople(people, random, 0);
            EyeColor eyes = isMother ? child.EyesFromMother : child.EyesFromFather;
            HairColor hair = isMother ? child.HairFromMother : child.HairFromFather;
            if (random.Chance(0.5))
            {
                face.EyesFromMother = eyes;
            }
            else
            {
                face.EyesFromFather = eyes;
            }

            if (random.Chance(0.5))
            {
                face.HairFromMother = hair;
            }
            else
            {
                face.HairFromFather = hair;
            }

            face.SkinTone = Near(child.SkinTone, SkinSpread, random);
            face.Width = Near(child.Width, ShapeSpread / 2, random);
            face.Chin = Near(child.Chin, ShapeSpread / 2, random);
            face.Nose = Near(child.Nose, ShapeSpread / 2, random);
            face.Lips = Near(child.Lips, ShapeSpread / 2, random);
            face.Cheekbones = Near(child.Cheekbones, ShapeSpread / 2, random);
            face.EyeSize = Near(child.EyeSize, ShapeSpread / 2, random);

            // Las cejas del hijo vienen de su mamá: así siempre calzan.
            if (isMother)
            {
                face.ThickBrows = child.ThickBrows;
            }

            face.GrayFromAge = Math.Max(25, Math.Min(75, child.GrayFromAge + random.NextInt(-GraySpread, GraySpread + 1)));
            return face;
        }

        /// <summary>El peinado y la barba de alguien que no los ha elegido, según cómo vive su género.</summary>
        private static void PickStyle(Face face, Character person, GameRandom random)
        {
            GameRandom style = random.Fork("estilo");
            bool man = person.Gender == Gender.Male;
            bool woman = person.Gender == Gender.Female;
            if (woman)
            {
                face.HairStyle = Pick(style, (HairStyle.Long, 55), (HairStyle.Short, 25), (HairStyle.Bun, 20));
            }
            else if (man)
            {
                face.HairStyle = Pick(style, (HairStyle.Short, 65), (HairStyle.Buzzed, 25), (HairStyle.Long, 7), (HairStyle.Bald, 3));
            }
            else
            {
                face.HairStyle = Pick(style, (HairStyle.Short, 45), (HairStyle.Long, 35), (HairStyle.Buzzed, 10), (HairStyle.Bun, 10));
            }

            face.Beard = man
                ? Pick(style, (BeardStyle.None, 55), (BeardStyle.Short, 25), (BeardStyle.Full, 15), (BeardStyle.Mustache, 5))
                : BeardStyle.None;
        }

        private static T Pick<T>(GameRandom random, params (T Value, int Weight)[] options) =>
            options[random.PickWeightedIndex(options.Select(o => o.Weight).ToList())].Value;

        private static int MyopicParents(Face? mother, Face? father) =>
            (IsMyopic(mother) ? 1 : 0) + (IsMyopic(father) ? 1 : 0);

        private static bool IsMyopic(Face? face) =>
            face != null && face.GlassesFromAge.HasValue && face.GlassesFromAge.Value < MyopicBefore;

        private static int? Glasses(FacePopulationDefinition? people, GameRandom random, int myopicParents)
        {
            double myopia = (people?.MyopiaPerThousand ?? 250) / 1000.0;
            myopia = Math.Min(MaxMyopiaChance, myopia * (1 + (MyopiaPerMyopicParent * myopicParents)));
            if (random.Chance(myopia))
            {
                return random.NextInt(8, 26);
            }

            return random.Chance(ReadingGlassesChance) ? random.NextInt(43, 51) : (int?)null;
        }

        private static int PeopleSkin(FacePopulationDefinition? people, GameRandom random)
        {
            int band = people != null && people.Skin.Count == SkinBands
                ? random.PickWeightedIndex(people.Skin)
                : random.NextInt(SkinBands);
            int width = 100 / SkinBands;
            return Math.Min(100, (band * width) + random.NextInt(width + 1));
        }

        private static int PeopleColor(IReadOnlyList<int>? seen, int count, GameRandom random)
        {
            if (seen == null || seen.Count != count)
            {
                return random.NextInt(count);
            }

            double[] shares = CarrierShares(seen);
            double roll = random.NextDouble();
            for (int i = 0; i < shares.Length; i++)
            {
                roll -= shares[i];
                if (roll < 0)
                {
                    return i;
                }
            }

            return count - 1;
        }

        /// <summary>Dos tiradas promediadas: lo más común es lo del medio.</summary>
        private static int PeopleShape(GameRandom random) =>
            (random.NextInt(0, 101) + random.NextInt(0, 101)) / 2;

        private static int Near(int value, int spread, GameRandom random) =>
            Math.Max(0, Math.Min(100, value + random.NextInt(-spread, spread + 1)));

        private static FacePopulationDefinition? PeopleOf(World world, ContentDatabase content, Character person)
        {
            string? countryId = person.Residence?.CountryId ?? world.Player.Residence?.CountryId;
            return countryId == null ? null : content.Countries.FirstOrDefault(c => c.Id == countryId)?.Faces;
        }

        /// <summary>Los papás biológicos del jugador y todos sus otros descendientes (hermanos, sobrinos).</summary>
        private static List<Character> BloodFamily(World world, Character player)
        {
            HashSet<int> family = new HashSet<int>();
            if (!player.Adopted)
            {
                if (player.MotherId.HasValue)
                {
                    family.Add(player.MotherId.Value);
                }

                if (player.FatherId.HasValue)
                {
                    family.Add(player.FatherId.Value);
                }
            }

            bool grew = family.Count > 0;
            while (grew)
            {
                grew = false;
                foreach (Character c in world.Characters)
                {
                    if (c.Id != player.Id && !c.Adopted && !family.Contains(c.Id)
                        && ((c.MotherId.HasValue && family.Contains(c.MotherId.Value)) || (c.FatherId.HasValue && family.Contains(c.FatherId.Value))))
                    {
                        family.Add(c.Id);
                        grew = true;
                    }
                }
            }

            return world.Characters.Where(c => family.Contains(c.Id)).ToList();
        }
    }
}
