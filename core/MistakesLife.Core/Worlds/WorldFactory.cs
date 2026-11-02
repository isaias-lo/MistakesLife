using MistakesLife.Core.Characters;
using MistakesLife.Core.Randomness;

namespace MistakesLife.Core.Worlds
{
    /// <summary>
    /// Crea un mundo nuevo con un recién nacido como jugador.
    /// Por ahora sin país ni familia: eso llega con los paquetes (tarea 5).
    /// </summary>
    public static class WorldFactory
    {
        public static World CreateNewLife(ulong seed)
        {
            var world = new World(seed);
            GameRandom random = world.Random.Fork("mundo.nacimiento");

            var player = new Character(
                world.NextCharacterId(),
                firstName: "nombre.pendiente",
                lastName: "apellido.pendiente",
                sex: random.Chance(0.5) ? Sex.Female : Sex.Male,
                birthDate: world.Date,
                personality: Personality.Random(random),
                stats: new Stats(
                    health: random.NextInt(70, 101),
                    happiness: random.NextInt(60, 101),
                    smarts: random.NextInt(30, 71)));

            world.AddCharacter(player);
            world.SetPlayer(player.Id);
            world.WriteJournal(player.Id, "diario.nacimiento");
            return world;
        }
    }
}
