using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>La cara de la gente, heredada de los papás (B8 P2 en docs/roadmap.md).</summary>
    public class FaceTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        /// <summary>Un hijo más de la mamá y el papá del jugador, para ver qué puede salir.</summary>
        private static Character Sibling(World world, Sex sex)
        {
            Character player = world.Player;
            var child = new Character(world.NextCharacterId(), "Hermano", "Prueba", sex, world.Date, new Personality(50, 50, 50, 50, 50), new Stats(80, 60, 50))
            {
                MotherId = player.MotherId,
                FatherId = player.FatherId,
                Residence = player.Residence,
            };
            world.AddCharacter(child);
            return child;
        }

        [Fact]
        public void Rostro_ElHijoTraeUnColorDeOjosDeCadaPapa()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var session = new LifeSession(Content, seed);
                session.Advance(1);
                World world = session.World;
                Character player = world.Player;
                Face face = player.Face!;
                Face mother = world.GetCharacter(player.MotherId!.Value).Face!;
                Face father = world.GetCharacter(player.FatherId!.Value).Face!;
                Assert.Contains(face.EyesFromMother, new[] { mother.EyesFromMother, mother.EyesFromFather });
                Assert.Contains(face.EyesFromFather, new[] { father.EyesFromMother, father.EyesFromFather });
                Assert.Contains(face.HairFromMother, new[] { mother.HairFromMother, mother.HairFromFather });
                Assert.Contains(face.HairFromFather, new[] { father.HairFromMother, father.HairFromFather });
                int skin = (mother.SkinTone + father.SkinTone) / 2;
                Assert.InRange(face.SkinTone, skin - FaceRules.SkinSpread, skin + FaceRules.SkinSpread);
                int width = (mother.Width + father.Width) / 2;
                Assert.InRange(face.Width, width - FaceRules.ShapeSpread, width + FaceRules.ShapeSpread);
            }
        }

        [Fact]
        public void Rostro_DosPapasDeOjosCafeQueTraenAzul_PuedenTenerHijosDeOjosAzules()
        {
            var session = new LifeSession(Content, 4);
            World world = session.World;
            Character player = world.Player;
            Face mother = session.FaceOf(world.GetCharacter(player.MotherId!.Value));
            Face father = session.FaceOf(world.GetCharacter(player.FatherId!.Value));
            mother.EyesFromMother = EyeColor.DarkBrown;
            mother.EyesFromFather = EyeColor.Blue;
            father.EyesFromMother = EyeColor.Blue;
            father.EyesFromFather = EyeColor.DarkBrown;
            Assert.Equal(EyeColor.DarkBrown, mother.Eyes);
            Assert.Equal(EyeColor.DarkBrown, father.Eyes);

            List<EyeColor> children = Enumerable.Range(0, 400).Select(_ => session.FaceOf(Sibling(world, Sex.Female)).Eyes).ToList();
            double blue = children.Count(e => e == EyeColor.Blue) / (double)children.Count;
            Assert.InRange(blue, 0.15, 0.35); // uno de cada cuatro, como en la vida real
            Assert.All(children, e => Assert.True(e == EyeColor.Blue || e == EyeColor.DarkBrown));
        }

        [Fact]
        public void Rostro_LaMismaPartidaDaLasMismasCaras_SinImportarCuandoSeCalculan()
        {
            var a = new LifeSession(Content, 9);
            var b = new LifeSession(Content, 9);
            a.Advance(2);
            Character fatherB = b.World.GetCharacter(b.World.Player.FatherId!.Value);
            b.FaceOf(fatherB); // en b, el papá se calcula antes que el jugador
            b.Advance(2);
            Assert.Equal(Numbers(a.World.Player.Face!), Numbers(b.World.Player.Face!));
            Assert.Equal(Numbers(a.World.GetCharacter(fatherB.Id).Face!), Numbers(fatherB.Face!));
        }

        [Fact]
        public void Rostro_AlArmarlaPiezaPorPieza_SusPapasCalzanConElla()
        {
            for (ulong seed = 1; seed <= 15; seed++)
            {
                var session = new LifeSession(Content, seed);
                Assert.True(session.CanChooseFace);
                Face chosen = session.RandomFace(1);
                chosen.SetEyes(EyeColor.Green);
                chosen.SetHair(HairColor.Red);
                chosen.SkinTone = 90;
                session.ChooseFace(chosen);

                World world = session.World;
                Face mother = world.GetCharacter(world.Player.MotherId!.Value).Face!;
                Face father = world.GetCharacter(world.Player.FatherId!.Value).Face!;
                Assert.True(world.Player.Face!.Chosen);
                Assert.Contains(EyeColor.Green, new[] { mother.EyesFromMother, mother.EyesFromFather });
                Assert.Contains(EyeColor.Green, new[] { father.EyesFromMother, father.EyesFromFather });
                Assert.Contains(HairColor.Red, new[] { mother.HairFromMother, mother.HairFromFather });
                Assert.Contains(HairColor.Red, new[] { father.HairFromMother, father.HairFromFather });
                Assert.InRange(mother.SkinTone, 90 - FaceRules.SkinSpread, 100);
                Assert.InRange(father.SkinTone, 90 - FaceRules.SkinSpread, 100);
            }
        }

        [Fact]
        public void Rostro_AlJugarConUnHijo_SoloSeCambiaElEstilo()
        {
            var session = new LifeSession(Content, 3);
            session.Advance(GameDate.WeeksPerYear * 2);
            Assert.False(session.CanChooseFace);
            Assert.Throws<System.InvalidOperationException>(() => session.ChooseFace(new Face()));

            session.ChooseFaceStyle(HairStyle.Bun, BeardStyle.None, true);
            Face face = session.World.Player.Face!;
            Assert.Equal(HairStyle.Bun, face.HairStyle);
            Assert.True(face.WearsGlasses(2));
        }

        [Fact]
        public void Rostro_LaGenteDelPaisSaleComoDicenSusPorcentajes()
        {
            // De cada cien chilenos, 7 de ojos azules según country.json: 400 personas sin papás.
            var session = new LifeSession(Content, 11, "pais.chile");
            List<Face> faces = Enumerable.Range(0, 400).Select(i => session.RandomFace(i)).ToList();
            double blue = faces.Count(f => f.Eyes == EyeColor.Blue) / (double)faces.Count;
            double darkBrown = faces.Count(f => f.Eyes == EyeColor.DarkBrown) / (double)faces.Count;
            Assert.InRange(blue, 0.03, 0.12);
            Assert.InRange(darkBrown, 0.5, 0.7);
        }

        [Fact]
        public void Rostro_LaBarbaNoSeVeAntesDeLos16()
        {
            var face = new Face { Beard = BeardStyle.Full };
            Assert.Equal(BeardStyle.None, FaceRules.BeardAt(face, 12));
            Assert.Equal(BeardStyle.Full, FaceRules.BeardAt(face, 30));
        }

        [Fact]
        public void Guardado_RecuerdaLaCara()
        {
            var session = new LifeSession(Content, 5);
            session.Advance(3);
            session.ChooseFaceStyle(HairStyle.Long, BeardStyle.Mustache, false);
            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());
            foreach (Character person in session.World.Characters.Where(c => c.Face != null))
            {
                Assert.Equal(Numbers(person.Face!), Numbers(loaded.World.GetCharacter(person.Id).Face!));
            }

            Assert.Equal(BeardStyle.Mustache, loaded.World.Player.Face!.Beard);
            Assert.False(loaded.World.Player.Face!.GlassesChoice);
        }

        private static string Numbers(Face f) => string.Join(",", Saving.WorldWriter.FaceOf(f)!);
    }
}
