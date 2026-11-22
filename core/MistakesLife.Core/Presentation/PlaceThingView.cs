namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Algo de tu sala o tu pega que se puede tocar (D5c): tu libreta, la pizarra con la prueba
    /// que viene o tu agenda con las tareas del mes. Solo aparece si de verdad hay algo que ver.
    /// </summary>
    public sealed class PlaceThingView
    {
        public PlaceThingView(string id, string label)
        {
            Id = id;
            Label = label;
        }

        /// <summary>Qué es: <see cref="PlaceView.Notebook"/>, <see cref="PlaceView.Board"/> o <see cref="PlaceView.Agenda"/>.</summary>
        public string Id { get; }

        /// <summary>La etiqueta que se ve en la escena ("Tu libreta", "Matemática · prueba el jueves 14").</summary>
        public string Label { get; }
    }
}
