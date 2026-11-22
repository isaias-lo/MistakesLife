namespace MistakesLife.Core.Presentation
{
    /// <summary>Un lugar del mapa de tu barrio (D5c2): su nombre, su forma 3D y si abre otra pantalla.</summary>
    public sealed class MapPlaceView
    {
        public MapPlaceView(string id, string name, string description, string shape, string? opens, string? countryNote)
        {
            Id = id;
            Name = name;
            Description = description;
            Shape = shape;
            Opens = opens;
            CountryNote = countryNote;
        }

        /// <summary>"lugar.barrio.mall": para pedir lo que se hace ahí (<see cref="LifePresenter.PlaceActivities"/>).</summary>
        public string Id { get; }

        /// <summary>"Mall y cine".</summary>
        public string Name { get; }

        /// <summary>"El mall de tu comuna, con cine y patio de comidas."</summary>
        public string Description { get; }

        /// <summary>La forma 3D en el mapa ("mall", "fonda"): Unity la dibuja.</summary>
        public string Shape { get; }

        /// <summary>
        /// Si al tocarlo se abre otra pantalla en vez de sus acciones: "casa" (vuelves a tu casa),
        /// "estudios" (tu sala) o "trabajo" (tu oficina); null si muestra lo que se hace ahí.
        /// </summary>
        public string? Opens { get; }

        /// <summary>"Solo en Chile" si es un lugar del paquete de tu país; null si es de base.</summary>
        public string? CountryNote { get; }
    }
}
