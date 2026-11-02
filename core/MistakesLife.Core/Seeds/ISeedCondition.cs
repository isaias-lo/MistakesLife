using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// Lo que tiene que pasar para que una semilla pueda despertar
    /// (por ejemplo, "postulas a una empresa donde trabaja un testigo").
    /// En la tarea 4 estas condiciones se escribirán en los paquetes JSON
    /// con un pequeño lenguaje; aquí solo se define la forma de preguntarlas.
    /// </summary>
    public interface ISeedCondition
    {
        bool IsMet(World world, Seed seed);
    }
}
