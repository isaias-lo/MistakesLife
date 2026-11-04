using System;

namespace MistakesLife.Core.Economy
{
    /// <summary>Dónde está alguien en su carrera: el área y el cargo (por ejemplo, senior en ciberseguridad).</summary>
    public sealed class CareerPosition
    {
        public CareerPosition(string areaId, string positionId)
        {
            AreaId = areaId ?? throw new ArgumentNullException(nameof(areaId));
            PositionId = positionId ?? throw new ArgumentNullException(nameof(positionId));
        }

        /// <summary>Por ejemplo "carrera.software.area.ciberseguridad".</summary>
        public string AreaId { get; }

        /// <summary>Por ejemplo "carrera.software.cargo.senior".</summary>
        public string PositionId { get; }
    }
}
