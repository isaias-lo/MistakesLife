namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Adónde va la escena 3D cuando un momento detiene el tiempo (D5c3, "la escena sigue lo que
    /// te pasa"): tu casa, tu sala o tu pega con su gente, o un lugar del barrio de cerca. La
    /// persona del momento va destacada. Unity solo lo dibuja.
    /// </summary>
    public sealed class MomentSceneView
    {
        public const string Home = "casa";

        public const string Section = "lugar";

        public const string Out = "salida";

        public MomentSceneView(string scene, PlaceView? place, OutingView? outing, int? personId, string caption)
        {
            Scene = scene;
            Place = place;
            Outing = outing;
            PersonId = personId;
            Caption = caption;
        }

        /// <summary><see cref="Home"/> (la casa), <see cref="Section"/> (tu sala, aula o pega) o <see cref="Out"/> (un lugar del barrio).</summary>
        public string Scene { get; }

        /// <summary>Tu sala, tu aula o tu pega con su gente, si <see cref="Scene"/> es <see cref="Section"/>.</summary>
        public PlaceView? Place { get; }

        /// <summary>El lugar del barrio de cerca, contigo y la persona del momento, si <see cref="Scene"/> es <see cref="Out"/>.</summary>
        public OutingView? Outing { get; }

        /// <summary>La persona del momento, para destacarla en la escena. Null si es sin nadie.</summary>
        public int? PersonId { get; }

        /// <summary>Dónde y con quién, listo para mostrar bajo la escena ("La plaza · con Tomás").</summary>
        public string Caption { get; }
    }
}
