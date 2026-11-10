using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// La casa del jugador lista para dibujar en 3D: quiénes están, qué tan acomodado es el
    /// hogar y, si arrienda, qué arrendó (pieza, departamento o casa).
    /// </summary>
    public sealed class HomeView
    {
        public HomeView(string communeId, int householdLevel, LifeStage playerStage, IReadOnlyList<HomeMemberView> members)
            : this(communeId, householdLevel, playerStage, members, new string[0])
        {
        }

        public HomeView(string communeId, int householdLevel, LifeStage playerStage, IReadOnlyList<HomeMemberView> members, IReadOnlyList<string> sceneObjects)
            : this(communeId, householdLevel, playerStage, members, sceneObjects, null)
        {
        }

        public HomeView(string communeId, int householdLevel, LifeStage playerStage, IReadOnlyList<HomeMemberView> members, IReadOnlyList<string> sceneObjects, string? housingTypeId)
        {
            HousingTypeId = housingTypeId;
            SceneObjects = sceneObjects;
            CommuneId = communeId;
            HouseholdLevel = householdLevel;
            PlayerStage = playerStage;
            Members = members;
        }

        /// <summary>Comuna donde está la casa (para elegir el estilo del barrio más adelante).</summary>
        public string CommuneId { get; }

        /// <summary>Nivel socioeconómico del hogar, de 0 a 100: más alto, casa más amplia y con más muebles.</summary>
        public int HouseholdLevel { get; }

        /// <summary>
        /// Lo que arrienda el jugador (id de <c>economia.vivienda</c>, por ejemplo
        /// "vivienda.pieza"), o null si vive en la casa de su familia. La pantalla dibuja una
        /// pieza chica, un departamento o una casa según esto.
        /// </summary>
        public string? HousingTypeId { get; }

        /// <summary>Etapa del jugador: decide cuna, cama o escritorio en su pieza.</summary>
        public LifeStage PlayerStage { get; }

        /// <summary>Las personas en la casa, empezando por el jugador.</summary>
        public IReadOnlyList<HomeMemberView> Members { get; }

        /// <summary>
        /// Los objetos de la escena que se ven ahora según la vida del jugador (ids
        /// <c>escena.casa.*</c>, <c>escena.barrio.*</c>, <c>escena.trabajo.*</c> de los paquetes, en orden).
        /// La pantalla dibuja los que conoce y salta los demás.
        /// </summary>
        public IReadOnlyList<string> SceneObjects { get; }

        /// <summary>
        /// Resumen corto para saber si la casa cambió y hay que volver a dibujarla
        /// (no cambia cada semana, así que no se redibuja siempre).
        /// </summary>
        public string Signature
        {
            get
            {
                var parts = new List<string> { CommuneId, (HouseholdLevel / 10).ToString(), PlayerStage.ToString(), HousingTypeId ?? "familia" };
                foreach (HomeMemberView member in Members)
                {
                    parts.Add(member.Id + ":" + member.Stage);
                }

                parts.AddRange(SceneObjects);

                return string.Join("|", parts);
            }
        }
    }
}
