using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Foto de la partida en un momento, con todo lo que la pantalla necesita dibujar.
    /// Solo datos ya traducidos: la pantalla no calcula nada ni decide nada.
    /// </summary>
    public sealed class LifeSnapshot
    {
        public LifeSnapshot(
            LifePhase phase,
            string playerName,
            int ageInYears,
            string ageLabel,
            string dateLabel,
            string place,
            int health,
            int happiness,
            int smarts,
            decimal money,
            string moneyLabel,
            int generation,
            ulong seed,
            EventView? pendingEvent,
            IReadOnlyList<PersonView> family,
            IReadOnlyList<PersonView> heirs)
        {
            Phase = phase;
            PlayerName = playerName;
            AgeInYears = ageInYears;
            AgeLabel = ageLabel;
            DateLabel = dateLabel;
            Place = place;
            Health = health;
            Happiness = happiness;
            Smarts = smarts;
            Money = money;
            MoneyLabel = moneyLabel;
            Generation = generation;
            Seed = seed;
            PendingEvent = pendingEvent;
            Family = family;
            Heirs = heirs;
        }

        public LifePhase Phase { get; }

        public string PlayerName { get; }

        public int AgeInYears { get; }

        /// <summary>Edad para mostrar ("12 semanas", "1 año", "34 años").</summary>
        public string AgeLabel { get; }

        /// <summary>Fecha del calendario del juego ("Año 34, semana 12").</summary>
        public string DateLabel { get; }

        /// <summary>Comuna, ciudad y país donde vive (vacío si no tiene residencia).</summary>
        public string Place { get; }

        /// <summary>Salud de 0 a 100.</summary>
        public int Health { get; }

        /// <summary>Felicidad de 0 a 100.</summary>
        public int Happiness { get; }

        /// <summary>Inteligencia de 0 a 100.</summary>
        public int Smarts { get; }

        public decimal Money { get; }

        /// <summary>Dinero con el formato del idioma ("$1.250.000").</summary>
        public string MoneyLabel { get; }

        /// <summary>Generación de la familia que se está jugando (1 = la primera vida).</summary>
        public int Generation { get; }

        /// <summary>Número de partida: con él se repite exactamente la misma vida.</summary>
        public ulong Seed { get; }

        /// <summary>El evento que espera una decisión (solo en <see cref="LifePhase.Choosing"/>).</summary>
        public EventView? PendingEvent { get; }

        /// <summary>Padres, pareja e hijos.</summary>
        public IReadOnlyList<PersonView> Family { get; }

        /// <summary>Hijos con quienes se puede seguir (solo en <see cref="LifePhase.ChoosingHeir"/>).</summary>
        public IReadOnlyList<PersonView> Heirs { get; }
    }
}
