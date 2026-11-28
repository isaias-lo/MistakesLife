namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// La cara de una persona (acuerdo con Isaias del 28-11-2026, ver "La gente y su cara" en
    /// docs/diseno-juego.md). Una parte se hereda de la mamá y el papá (piel, ojos, color de pelo,
    /// forma de la cara, canas, vista) y otra se elige (peinado, barba, lentes). Unity la dibuja
    /// con las piezas gente.* de Blender: los números de 0 a 100 son las llaves de forma de la cabeza.
    /// Ver <see cref="Life.FaceRules"/>.
    /// </summary>
    public sealed class Face
    {
        /// <summary>Tono de piel, de 0 (muy claro) a 100 (muy oscuro).</summary>
        public int SkinTone { get; set; }

        /// <summary>El color de ojos que trae de la mamá.</summary>
        public EyeColor EyesFromMother { get; set; }

        /// <summary>El color de ojos que trae del papá.</summary>
        public EyeColor EyesFromFather { get; set; }

        /// <summary>El color de ojos que se ve: el más oscuro de los dos.</summary>
        public EyeColor Eyes => EyesFromMother < EyesFromFather ? EyesFromMother : EyesFromFather;

        /// <summary>El color de pelo que trae de la mamá.</summary>
        public HairColor HairFromMother { get; set; }

        /// <summary>El color de pelo que trae del papá.</summary>
        public HairColor HairFromFather { get; set; }

        /// <summary>El color natural de pelo que se ve: el más oscuro de los dos.</summary>
        public HairColor Hair => HairFromMother < HairFromFather ? HairFromMother : HairFromFather;

        /// <summary>Ancho de la cara: 0 angosta, 50 normal, 100 ancha.</summary>
        public int Width { get; set; } = 50;

        /// <summary>Cuánto se marca el mentón (0 a 100).</summary>
        public int Chin { get; set; } = 50;

        /// <summary>Tamaño de la nariz: 0 chica, 50 normal, 100 grande.</summary>
        public int Nose { get; set; } = 50;

        /// <summary>Grosor de los labios (0 a 100).</summary>
        public int Lips { get; set; } = 50;

        /// <summary>Cuánto se marcan los pómulos (0 a 100).</summary>
        public int Cheekbones { get; set; } = 50;

        /// <summary>Tamaño de los ojos (0 a 100).</summary>
        public int EyeSize { get; set; } = 50;

        /// <summary>Si tiene cejas gruesas.</summary>
        public bool ThickBrows { get; set; }

        /// <summary>A qué edad le empiezan a salir canas.</summary>
        public int GrayFromAge { get; set; } = 50;

        /// <summary>
        /// A qué edad empieza a necesitar lentes: temprano si es miope, cerca de los 45 por la
        /// vista cansada, o null si nunca los usa.
        /// </summary>
        public int? GlassesFromAge { get; set; }

        /// <summary>El peinado que lleva.</summary>
        public HairStyle HairStyle { get; set; }

        /// <summary>La barba que lleva.</summary>
        public BeardStyle Beard { get; set; }

        /// <summary>
        /// Si el jugador decidió usar lentes (true) o no (false), o null para que dependa de
        /// <see cref="GlassesFromAge"/>.
        /// </summary>
        public bool? GlassesChoice { get; set; }

        /// <summary>
        /// Si el jugador armó esta cara pieza por pieza. Entonces sus papás heredan "al revés":
        /// sus caras se arman para que este hijo pueda haber salido de ellos.
        /// </summary>
        public bool Chosen { get; set; }

        /// <summary>Para el editor: deja los dos colores de ojos iguales al elegido.</summary>
        public void SetEyes(EyeColor color)
        {
            EyesFromMother = color;
            EyesFromFather = color;
        }

        /// <summary>Para el editor: deja los dos colores de pelo iguales al elegido.</summary>
        public void SetHair(HairColor color)
        {
            HairFromMother = color;
            HairFromFather = color;
        }

        /// <summary>Si a esta edad ya tiene canas.</summary>
        public bool IsGray(int ageInYears) => ageInYears >= GrayFromAge;

        /// <summary>Si a esta edad usa lentes.</summary>
        public bool WearsGlasses(int ageInYears) =>
            GlassesChoice ?? (GlassesFromAge.HasValue && ageInYears >= GlassesFromAge.Value);

        /// <summary>Una copia, para que el editor pruebe cambios sin tocar la cara de verdad.</summary>
        public Face Copy() => (Face)MemberwiseClone();
    }
}
