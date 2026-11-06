namespace MistakesLife.Core.Presentation
{
    /// <summary>Un momento del pasado al que el jugador puede volver, listo para mostrar.</summary>
    public sealed class MomentView
    {
        public MomentView(int snapshotId, int ageInYears, string label)
        {
            SnapshotId = snapshotId;
            AgeInYears = ageInYears;
            Label = label;
        }

        /// <summary>Número de la foto del mundo (para <see cref="LifePresenter.GoBack"/>).</summary>
        public int SnapshotId { get; }

        /// <summary>Edad que tenía el jugador en ese momento.</summary>
        public int AgeInYears { get; }

        /// <summary>Texto del botón, por ejemplo "A tus 15 años, antes de decidir: ...".</summary>
        public string Label { get; }
    }
}
