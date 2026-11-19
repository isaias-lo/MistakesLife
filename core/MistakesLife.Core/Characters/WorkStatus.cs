namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// En qué anda alguien que nadie juega, según los datos de su país (ver
    /// <see cref="Life.PeopleWorkSystem"/>). Quien trabaja en una pega del juego además
    /// tiene su <see cref="Character.Job"/>.
    /// </summary>
    public enum WorkStatus
    {
        /// <summary>Trabaja: en una pega del juego (tiene Job) o en otra que el juego todavía no tiene.</summary>
        Working,
        /// <summary>Está desocupado: busca pega.</summary>
        Unemployed,
        /// <summary>Estudia una carrera (educación superior).</summary>
        Studying,
        /// <summary>No trabaja ni busca, y ya tiene la edad de jubilarse.</summary>
        Retired,
        /// <summary>No trabaja ni busca (cuida a la familia, por salud u otra razón): no se le muestra nada.</summary>
        Inactive,
    }
}
