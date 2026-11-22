namespace MistakesLife.Unity
{
    /// <summary>Qué muestra la ventana 3D.</summary>
    public enum SceneView
    {
        /// <summary>La pieza de la casa con la familia.</summary>
        Room,

        /// <summary>La cuadra del barrio con el camino de la semana.</summary>
        Neighborhood,

        /// <summary>El lugar de trabajo (solo si el jugador trabaja).</summary>
        Work,

        /// <summary>
        /// El lugar de una sección que se toca (D5c): tu sala, tu aula o tu pega con su gente
        /// real y sus nombres. Lo abre <see cref="PlaceScreen"/>.
        /// </summary>
        Place,

        /// <summary>El mapa chico de tu barrio al abrir Tu gente (D5c2): sus lugares se tocan. Lo abre <see cref="MapScreen"/>.</summary>
        Map,

        /// <summary>Una salida del barrio (D5c2): el lugar donde fuiste, contigo y con quien fuiste.</summary>
        Outing,
    }
}
