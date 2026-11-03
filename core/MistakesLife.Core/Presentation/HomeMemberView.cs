namespace MistakesLife.Core.Presentation
{
    /// <summary>Una persona que está en la casa del jugador, lista para dibujar como figura.</summary>
    public sealed class HomeMemberView
    {
        public HomeMemberView(int id, string name, string relation, int ageInYears, LifeStage stage, bool isFemale, bool isPlayer)
        {
            Id = id;
            Name = name;
            Relation = relation;
            AgeInYears = ageInYears;
            Stage = stage;
            IsFemale = isFemale;
            IsPlayer = isPlayer;
        }

        public int Id { get; }

        public string Name { get; }

        /// <summary>Qué es del jugador (vacío para el propio jugador).</summary>
        public string Relation { get; }

        public int AgeInYears { get; }

        public LifeStage Stage { get; }

        /// <summary>Sexo, solo para elegir la figura.</summary>
        public bool IsFemale { get; }

        public bool IsPlayer { get; }
    }
}
