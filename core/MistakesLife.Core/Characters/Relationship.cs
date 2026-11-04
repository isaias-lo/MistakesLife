using System;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Lo que un personaje siente por otro: cariño, confianza, respeto y rencor,
    /// cada uno de 0 a 100 y por separado (alguien puede respetarte y guardarte
    /// rencor a la vez). No es simétrico: lo que tu madre siente por ti no es lo
    /// que tú sientes por ella. El lazo (<see cref="Bond"/>) sí va en ambos sentidos.
    /// </summary>
    public sealed class Relationship
    {
        public const int Min = 0;

        public const int Max = 100;

        private int _affection;
        private int _trust;
        private int _respect;
        private int _resentment;

        public Relationship(int otherId, int affection, int trust, int respect, int resentment, RelationshipBond bond = RelationshipBond.None)
        {
            OtherId = otherId;
            Affection = affection;
            Trust = trust;
            Respect = respect;
            Resentment = resentment;
            Bond = bond;
        }

        /// <summary>Id de la persona por quien se siente esto.</summary>
        public int OtherId { get; }

        /// <summary>Cariño ('carino'), de 0 a 100.</summary>
        public int Affection { get => _affection; set => _affection = Clamp(value); }

        /// <summary>Confianza ('confianza'), de 0 a 100.</summary>
        public int Trust { get => _trust; set => _trust = Clamp(value); }

        /// <summary>Respeto ('respeto'), de 0 a 100.</summary>
        public int Respect { get => _respect; set => _respect = Clamp(value); }

        /// <summary>Rencor ('rencor'), de 0 a 100. Con el tiempo se olvida de a poco.</summary>
        public int Resentment { get => _resentment; set => _resentment = Clamp(value); }

        /// <summary>Qué son: amigos, colegas, rivales, ex o nada especial.</summary>
        public RelationshipBond Bond { get; set; }

        /// <summary>Una copia con los mismos valores, para otra persona (por ejemplo, al crear un lazo en ambos sentidos).</summary>
        public Relationship CopyFor(int otherId) => new Relationship(otherId, Affection, Trust, Respect, Resentment, Bond);

        private static int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));

        public override string ToString() => $"#{OtherId}: cariño {Affection}, confianza {Trust}, respeto {Respect}, rencor {Resentment}";
    }
}
