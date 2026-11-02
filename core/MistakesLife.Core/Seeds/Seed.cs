using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Seeds
{
    /// <summary>
    /// El recuerdo que el mundo guarda de una decisión, y que puede volver
    /// años después como un evento.
    /// </summary>
    public sealed class Seed
    {
        public const int MinStrength = 0;
        public const int MaxStrength = 100;

        private readonly SortedSet<int> _witnesses = new SortedSet<int>();
        private readonly List<string> _possibleEvents;
        private int _strength;

        internal Seed(int id, SeedDefinition definition, int ownerId, GameDate plantedDate, int ownerAgeAtPlanting, int? parentSeedId)
        {
            Id = id;
            TypeId = definition.TypeId;
            Polarity = definition.Polarity;
            OwnerId = ownerId;
            PlantedDate = plantedDate;
            OwnerAgeAtPlanting = ownerAgeAtPlanting;
            ParentSeedId = parentSeedId;
            ExpiresAfterWeeks = definition.ExpiresAfterWeeks;
            MinWeeksDormant = definition.MinWeeksDormant;
            Condition = definition.Condition;
            Heritable = definition.Heritable;
            _possibleEvents = definition.PossibleEvents.ToList();
            _strength = ClampStrength(definition.Strength);
        }

        /// <summary>Número único de esta semilla en el mundo.</summary>
        public int Id { get; }

        /// <summary>Id estable del tipo, por ejemplo "semilla.copio_paes".</summary>
        public string TypeId { get; }

        public SeedPolarity Polarity { get; }

        /// <summary>Personaje a quien le pertenece la decisión.</summary>
        public int OwnerId { get; private set; }

        /// <summary>Origen: cuándo se plantó.</summary>
        public GameDate PlantedDate { get; }

        /// <summary>Origen: edad del dueño al plantarla.</summary>
        public int OwnerAgeAtPlanting { get; }

        /// <summary>Semilla de la que nació esta, si la hay.</summary>
        public int? ParentSeedId { get; }

        public int ExpiresAfterWeeks { get; }

        public int MinWeeksDormant { get; }

        public ISeedCondition? Condition { get; }

        public bool Heritable { get; }

        public IReadOnlyList<string> PossibleEvents => _possibleEvents;

        /// <summary>Personajes que saben lo que pasó. La semilla viaja con ellos.</summary>
        public IReadOnlyCollection<int> Witnesses => _witnesses;

        /// <summary>Qué tan probable es que vuelva, de 0 a 100.</summary>
        public int Strength => _strength;

        public SeedState State { get; private set; } = SeedState.Dormant;

        public GameDate? AwakenedDate { get; private set; }

        public GameDate? ClosedDate { get; private set; }

        public bool IsOpen => State == SeedState.Dormant || State == SeedState.Awake;

        /// <summary>Fecha en que se olvida si no despierta antes.</summary>
        public GameDate ExpiryDate => PlantedDate.AddWeeks(ExpiresAfterWeeks);

        /// <summary>Otra decisión la refuerza (por ejemplo, repetir el error).</summary>
        public void Strengthen(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Usa Weaken para restar.");
            }

            _strength = ClampStrength(_strength + amount);
        }

        /// <summary>Otra decisión la apaga (pedir perdón, reparar el daño).</summary>
        public void Weaken(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Usa Strengthen para sumar.");
            }

            _strength = ClampStrength(_strength - amount);
        }

        /// <summary>Un personaje se entera: la semilla se extiende.</summary>
        public void AddWitness(int characterId) => _witnesses.Add(characterId);

        public bool RemoveWitness(int characterId) => _witnesses.Remove(characterId);

        public bool HasWitness(int characterId) => _witnesses.Contains(characterId);

        internal void Awaken(GameDate date)
        {
            RequireState(SeedState.Dormant);
            State = SeedState.Awake;
            AwakenedDate = date;
        }

        internal void Resolve(GameDate date)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException($"La semilla {Id} ya está cerrada.");
            }

            State = SeedState.Resolved;
            ClosedDate = date;
        }

        internal void Expire(GameDate date)
        {
            RequireState(SeedState.Dormant);
            State = SeedState.Expired;
            ClosedDate = date;
        }

        internal void TransferTo(int newOwnerId) => OwnerId = newOwnerId;

        private void RequireState(SeedState expected)
        {
            if (State != expected)
            {
                throw new InvalidOperationException($"La semilla {Id} está en estado {State}, se esperaba {expected}.");
            }
        }

        private static int ClampStrength(int value) => Math.Max(MinStrength, Math.Min(MaxStrength, value));
    }
}
