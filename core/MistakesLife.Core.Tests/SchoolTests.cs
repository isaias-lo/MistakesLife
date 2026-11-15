using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Saving;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Rediseño R3a: tu curso del colegio, con compañeros con nombre y profe jefe.</summary>
    public class SchoolTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static CountryDefinition Chile => Content.GetCountry("pais.chile");

        /// <summary>Una vida en Chile que solo hace correr el colegio hasta esa edad y semana del año.</summary>
        private static World LiveUntil(int age, int weekOfYear)
        {
            World world = WorldFactory.CreateNewLife(3, Content, "pais.chile");
            int weeks = (age * GameDate.WeeksPerYear) + weekOfYear - world.Player.AgeInWeeks(world.Date);
            new WeekCycle().Register(new SchoolSystem(Content)).AdvanceWeeks(world, weeks, out _);
            return world;
        }

        /// <summary>Una vida entera (con todos los sistemas) jugada hasta esa edad y semana del año.</summary>
        private static LifeSession SessionAt(int age, int weekOfYear)
        {
            var session = new LifeSession(Content, 3, "pais.chile");
            session.TimeMachine.Enabled = false;
            int target = (age * GameDate.WeeksPerYear) + weekOfYear;
            while (session.World.Player.IsAlive && session.World.Player.AgeInWeeks(session.World.Date) < target)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }

            return session;
        }

        [Fact]
        public void CalendarioEscolar_EnVeranoYEnInviernoNoHayClases()
        {
            Assert.False(Chile.IsSchoolTime(new GameDate(2)));       // enero
            Assert.True(Chile.IsSchoolTime(new GameDate(14)));       // abril
            Assert.False(Chile.IsSchoolTime(new GameDate(26)));      // fines de junio
            Assert.True(Chile.IsSchoolTime(new GameDate(35)));       // septiembre
            Assert.False(Chile.IsSchoolTime(new GameDate(51)));      // fines de diciembre
        }

        [Fact]
        public void Curso_AlEntrarABasica_TieneDiezCompanerosConNombreDistintoYProfeJefe()
        {
            World world = LiveUntil(6, 12);
            Character player = world.Player;

            Assert.NotNull(player.SchoolClass);
            Assert.Equal("etapa.chile.basica", player.SchoolClass!.StageId);
            Assert.Equal(1, player.SchoolClass.Level);
            Assert.Equal(32, player.SchoolClass.Students);
            var mates = SchoolRules.Classmates(world, player);
            Assert.Equal(10, mates.Count);
            Assert.Equal(mates.Count, mates.Select(m => m.FirstName).Append(player.FirstName).Distinct().Count() - 1);
            Assert.All(mates, m => Assert.InRange(m.BirthDate.TotalWeeks - player.BirthDate.TotalWeeks, SchoolRules.OldestClassmateWeeks, SchoolRules.YoungestClassmateWeeks));
            Character? teacher = SchoolRules.Teacher(world, player);
            Assert.NotNull(teacher);
            Assert.InRange(teacher!.AgeInYears(world.Date), SchoolRules.MinTeacherAge - 1, SchoolRules.MaxTeacherAge + 1);
            Assert.Contains(world.Journal, j => j.TextKey == "diario.curso.nuevo");
        }

        [Fact]
        public void Curso_EnLasVacacionesAntesDeBasica_TodaviaNoExiste()
        {
            World world = LiveUntil(6, 3);

            Assert.Null(world.Player.SchoolClass);
        }

        [Fact]
        public void Curso_CadaAnoPasaDeCurso_YAlLlegarAMediaArmaUnoNuevo()
        {
            World world = LiveUntil(13, 20);
            Assert.Equal(8, world.Player.SchoolClass!.Level);
            var basica = world.Player.SchoolClass.ClassmateIds.ToList();

            new WeekCycle().Register(new SchoolSystem(Content)).AdvanceWeeks(world, GameDate.WeeksPerYear, out _);

            SchoolClass media = world.Player.SchoolClass!;
            Assert.Equal("etapa.chile.media", media.StageId);
            Assert.Equal(1, media.Level);
            Assert.Equal(10, media.ClassmateIds.Count);
            Assert.True(media.ClassmateIds.Count(basica.Contains) < 10, "Al pasar a media no sigue el curso entero.");
        }

        [Fact]
        public void Curso_AlTerminarElColegio_SeCierraYLosCompanerosSiguenEnElMundo()
        {
            World world = LiveUntil(17, 20);
            var mates = world.Player.SchoolClass!.ClassmateIds.ToList();

            new WeekCycle().Register(new SchoolSystem(Content)).AdvanceWeeks(world, GameDate.WeeksPerYear, out _);

            Assert.Null(world.Player.SchoolClass);
            Assert.All(mates, id => Assert.True(world.TryGetCharacter(id, out _)));
        }

        [Fact]
        public void Conocer_EnElColegio_EsAlguienDeTuCursoYQuedaComoAmigoDeVerdad()
        {
            World world = LiveUntil(9, 20);

            Character friend = FamilyFactory.CreateAcquaintance(world, Content, world.Player, RelationshipBond.Friend);

            Assert.Contains(friend.Id, world.Player.SchoolClass!.ClassmateIds);
            Relationship feelings = RelationshipRules.Feelings(world, world.Player, friend);
            Assert.Equal(RelationshipBond.Friend, feelings.Bond);
            Assert.True(feelings.Affection >= RelationshipRules.Initial(RelationshipBond.Friend).Affection);
        }

        [Fact]
        public void Companeros_QueSeQuierenDeVerdad_SeVuelvenAmigos()
        {
            World world = LiveUntil(9, 20);
            Character mate = SchoolRules.Classmates(world, world.Player)[0];
            RelationshipRules.Edit(world, world.Player, mate).Affection = 70;
            RelationshipRules.Edit(world, mate, world.Player).Affection = 65;

            var friends = SchoolRules.NewFriendships(world, world.Player);

            Assert.Contains(mate, friends);
            Assert.Equal(RelationshipBond.Friend, RelationshipRules.Feelings(world, world.Player, mate).Bond);
        }

        [Fact]
        public void Guardado_ConservaElCurso()
        {
            LifeSession session = SessionAt(10, 20);
            SchoolClass before = session.World.Player.SchoolClass!;

            LifeSession loaded = LifeSession.LoadFromBytes(Content, session.SaveToBytes());

            SchoolClass after = loaded.World.Player.SchoolClass!;
            Assert.Equal(before.StageId, after.StageId);
            Assert.Equal(before.Level, after.Level);
            Assert.Equal(before.Letter, after.Letter);
            Assert.Equal(before.SchoolYear, after.SchoolYear);
            Assert.Equal(before.TeacherId, after.TeacherId);
            Assert.Equal(before.ClassmateIds, after.ClassmateIds);
        }

        [Fact]
        public void Pantalla_MuestraElCursoComoSeDiceEnChile_ConLaProfeJefe()
        {
            LifeSession session = SessionAt(13, 20);

            SchoolClassView? view = new LifePresenter(session).SchoolClass();

            Assert.NotNull(view);
            Assert.StartsWith("8º básico ", view!.Name);
            Assert.Matches("^(la|el) profe ", view.Teacher);
            Assert.Equal(10, view.Classmates.Count);
            Assert.True(view.Classmates.Zip(view.Classmates.Skip(1), (a, b) => a.Affection >= b.Affection).All(x => x));
        }
    }
}
