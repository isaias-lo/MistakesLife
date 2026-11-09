namespace MistakesLife.Core.Presentation
{
    /// <summary>Una persona que está en la casa del jugador, lista para dibujar como figura.</summary>
    public sealed class HomeMemberView
    {
        public HomeMemberView(int id, string name, string relation, int ageInYears, LifeStage stage, bool isFemale, bool isPlayer)
            : this(id, name, relation, ageInYears, stage, isFemale, isPlayer, NeutralHappiness)
        {
        }

        public HomeMemberView(int id, string name, string relation, int ageInYears, LifeStage stage, bool isFemale, bool isPlayer, int happiness)
        {
            Happiness = happiness;
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

        /// <summary>Felicidad de la persona (0 a 100): la figura pone cara contenta, seria o triste.</summary>
        public int Happiness { get; }

        /// <summary>Gesto de la figura según su felicidad: 1 contenta, 0 seria, -1 triste.</summary>
        public int Mood => Happiness >= HappyFrom ? 1 : Happiness < SadBelow ? -1 : 0;

        /// <summary>Desde esta felicidad la figura sonríe.</summary>
        public const int HappyFrom = 60;

        /// <summary>Bajo esta felicidad la figura se ve triste.</summary>
        public const int SadBelow = 35;

        private const int NeutralHappiness = 50;
    }
}
