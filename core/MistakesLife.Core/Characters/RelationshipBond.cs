namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Qué es una persona para otra, además de la familia y la pareja (que salen
    /// de los lazos del personaje). Va en ambos sentidos: si A es amigo de B, B es
    /// amigo de A.
    /// </summary>
    public enum RelationshipBond
    {
        /// <summary>Solo se conocen: no hay un lazo especial.</summary>
        None,

        /// <summary>Amistad ('amistad').</summary>
        Friend,

        /// <summary>Colega del trabajo ('colega').</summary>
        Colleague,

        /// <summary>Rival o enemigo ('rival'). Se pueden reconciliar.</summary>
        Rival,

        /// <summary>Expareja ('ex'): se pone sola al terminar una pareja.</summary>
        Ex,
    }
}
