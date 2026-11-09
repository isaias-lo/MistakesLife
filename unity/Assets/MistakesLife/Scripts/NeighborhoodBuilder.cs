#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma una cuadra de barrio estilo maqueta: calle con veredas, casas de colores con reja
    /// (como en una villa de Puente Alto), el colegio, el almacén, una plaza y el paradero.
    /// La casa del jugador se marca, y un camino amarillo lleva adonde va esa semana.
    /// El jugador y su familia aparecen con su edad; vecinos y autos cambian de una vida a otra.
    /// El nivel del hogar decide cómo son las casas (techo de zinc o de tejas, uno o dos pisos).
    /// Cada edificio va en su ancla ("ancla.colegio"...) y se puede reemplazar por un modelo
    /// (<see cref="Models"/>): barrio.casa_humilde, barrio.casa_media, barrio.casa_acomodada,
    /// barrio.colegio, barrio.almacen, barrio.plaza, barrio.paradero, barrio.arbol y barrio.auto.
    /// </summary>
    public static class NeighborhoodBuilder
    {
        private const float StreetZ = 0f;
        private const float NorthRowZ = 7f;
        private const float SouthRowZ = -7f;
        private const float PlayerHouseX = -4f;

        /// <summary>Nombre del lugar: las anclas editables van en Resources/Lugares/barrio.prefab.</summary>
        public const string Place = "barrio";

        /// <summary>Las casas del norte, de izquierda a derecha; la del jugador es la tercera.</summary>
        private static readonly float[] NorthHouses = { -16f, -10f, PlayerHouseX, 2f, 8f, 14f };

        private const string PlayerHouse = "ancla.casa_3";

        /// <summary>Dónde va cada edificio si nadie movió las anclas.</summary>
        public static IReadOnlyDictionary<string, Vector3> DefaultAnchors()
        {
            var anchors = new Dictionary<string, Vector3>();
            for (int i = 0; i < NorthHouses.Length; i++)
            {
                anchors[$"ancla.casa_{i + 1}"] = new Vector3(NorthHouses[i], 0f, NorthRowZ);
            }

            anchors["ancla.colegio"] = new Vector3(-11f, 0f, SouthRowZ);
            anchors["ancla.almacen"] = new Vector3(-2f, 0f, SouthRowZ);
            anchors["ancla.plaza"] = new Vector3(9f, 0f, SouthRowZ);
            anchors["ancla.paradero"] = new Vector3(4f, 0f, -3.4f);
            return anchors;
        }


        private static readonly Color[] HouseColors =
        {
            new Color(0.95f, 0.78f, 0.55f), new Color(0.70f, 0.85f, 0.78f), new Color(0.93f, 0.62f, 0.55f),
            new Color(0.78f, 0.80f, 0.92f), new Color(0.96f, 0.90f, 0.62f),
        };

        public static void Build(ShapeKit kit, HomeView home, DailyPlace place)
        {
            int level = home.HouseholdLevel;
            Anchors anchors = Anchors.Create(kit.Parent, Place, DefaultAnchors());
            Ground(kit);

            // Fila de casas al norte de la calle; la del jugador al medio.
            for (int i = 0; i < NorthHouses.Length; i++)
            {
                string anchor = $"ancla.casa_{i + 1}";
                House(kit, anchors[anchor], level, HouseColors[i % HouseColors.Length], anchor == PlayerHouse);
            }

            // Al sur: colegio, almacén, plaza y paradero.
            Models.Place(kit, anchors["ancla.colegio"], "barrio.colegio", School);
            Models.Place(kit, anchors["ancla.almacen"], "barrio.almacen", Store);
            Models.Place(kit, anchors["ancla.plaza"], "barrio.plaza", Plaza);
            Models.Place(kit, anchors["ancla.paradero"], "barrio.paradero", BusStop);

            // "Vas aquí": en el patio del colegio o en el paradero, según adonde va esa semana.
            if (place != DailyPlace.Home)
            {
                Vector3 goal = Destination(anchors, place);
                kit.Shape(PrimitiveType.Cylinder, "Vas aqui", new Vector3(goal.x, place == DailyPlace.School ? 0.06f : 0.1f, goal.z), new Vector3(1.4f, 0.02f, 1.4f), Palette.PlayerMark);
            }

            Path(kit, anchors, place);

            // Lo que cambia de una vida a otra y con los años: quién está en la calle y qué autos hay.
            int seed = Seed(home);
            People(kit, anchors, home, place, seed);
            Cars(kit, level, seed);
        }

        /// <summary>
        /// Número fijo por vida (sale del id del jugador y del nivel del hogar) para variar
        /// el barrio sin azar: la misma vida siempre se ve igual, otra vida se ve distinta.
        /// No es azar de la simulación, solo decoración.
        /// </summary>
        private static int Seed(HomeView home)
        {
            int playerId = home.Members.Count > 0 ? home.Members[0].Id : 0;
            return unchecked((playerId * 7919) + (home.HouseholdLevel * 31) + home.CommuneId.Length);
        }

        private static int Pick(int seed, int salt, int count)
        {
            int mixed = unchecked((seed * 73856093) ^ (salt * 19349663));
            return (mixed & 0x7fffffff) % count;
        }

        /// <summary>
        /// El jugador (con su tamaño según la edad) está donde va esa semana o frente a su
        /// casa; su familia, frente a la casa. Además unos vecinos en la vereda y la plaza,
        /// y niños en el patio del colegio en horario de clases.
        /// </summary>
        private static void People(ShapeKit kit, Anchors anchors, HomeView home, DailyPlace place, int seed)
        {
            // Antejardín: en fila, separados, para que nadie tape a otro desde la cámara.
            Vector3 door = anchors[PlayerHouse].localPosition + new Vector3(-1.5f, 0f, -3.0f);
            for (int i = 0; i < home.Members.Count; i++)
            {
                HomeMemberView member = home.Members[i];
                Vector3 feet = door + new Vector3(i * 1.2f, 0f, 0f);
                if (member.IsPlayer && place != DailyPlace.Home)
                {
                    // Parado sobre el círculo de "vas aquí", donde termina el camino.
                    feet = Destination(anchors, place);
                }

                RoomBuilder.Person(kit, member, feet, i, markerSize: 1.2f);
            }

            // Lugares en la vereda lejos de los troncos, del paradero y del camino del jugador.
            float[] sidewalkX = { -18f, -12.5f, -9f, 0.5f, 8f, 14.5f };
            int neighbors = 2 + Pick(seed, 1, 3);
            int first = Pick(seed, 10, sidewalkX.Length);
            for (int n = 0; n < neighbors; n++)
            {
                float x = sidewalkX[(first + (n * 2)) % sidewalkX.Length];
                float z = Pick(seed, 20 + n, 2) == 0 ? StreetZ + 3.1f : StreetZ - 3.1f;
                Neighbor(kit, new Vector3(x, 0.08f, z), Pick(seed, 30 + n, 4), n);
            }

            // Plaza: alguien en la banca y, a veces, niños jugando.
            Vector3 plaza = anchors["ancla.plaza"].localPosition;
            Neighbor(kit, plaza + new Vector3(0.4f, 0.04f, 2.1f), 3, 7);
            if (Pick(seed, 40, 2) == 0)
            {
                Neighbor(kit, plaza + new Vector3(-1.5f, 0.04f, 0f), 1, 8);
                Neighbor(kit, plaza + new Vector3(-0.7f, 0.04f, 0.6f), 1, 9);
            }

            if (place == DailyPlace.School)
            {
                Vector3 school = anchors["ancla.colegio"].localPosition;
                for (int k = 0; k < 4; k++)
                {
                    Neighbor(kit, school + new Vector3(-2.5f + (k * 1.6f), 0.04f, 3.2f + ((k % 2) * 0.6f)), 1, 10 + k);
                }
            }
        }

        /// <summary>Un vecino: 0 adulto, 1 niño, 2 adolescente, 3 persona mayor.</summary>
        private static void Neighbor(ShapeKit kit, Vector3 feet, int kind, int index)
        {
            (int age, LifeStage stage) = kind switch
            {
                1 => (8, LifeStage.Child),
                2 => (15, LifeStage.Teen),
                3 => (72, LifeStage.Elder),
                _ => (40, LifeStage.Adult),
            };
            var person = new HomeMemberView(-100 - index, string.Empty, string.Empty, age, stage, index % 2 == 0, false);
            RoomBuilder.Person(kit, person, feet, index + 1);
        }

        /// <summary>Autos estacionados junto a las veredas; en barrios de más nivel hay más.</summary>
        private static void Cars(ShapeKit kit, int level, int seed)
        {
            Color[] paint =
            {
                new Color(0.75f, 0.20f, 0.20f), new Color(0.85f, 0.85f, 0.85f), new Color(0.20f, 0.30f, 0.55f),
                new Color(0.25f, 0.25f, 0.27f), new Color(0.60f, 0.62f, 0.30f),
            };
            float[] slots = { -18f, -13f, -8.5f, -1f, 5.5f, 10.5f, 16f };
            int count = 1 + (level / 30) + Pick(seed, 50, 2);
            for (int c = 0; c < count && c < slots.Length; c++)
            {
                float x = slots[(Pick(seed, 60, slots.Length) + (c * 3)) % slots.Length];
                float z = c % 2 == 0 ? StreetZ + 1.6f : StreetZ - 1.6f;
                Color color = paint[Pick(seed, 70 + c, paint.Length)];
                Models.Place(kit, Spot(kit, "Lugar del auto", new Vector3(x, 0f, z)), "barrio.auto", k => Car(k, color));
            }
        }

        /// <summary>Un punto suelto (sin ancla editable) para poner un modelo: autos y árboles.</summary>
        private static Transform Spot(ShapeKit kit, string name, Vector3 at)
        {
            Transform spot = new GameObject(name).transform;
            spot.SetParent(kit.Parent, false);
            spot.localPosition = at;
            return spot;
        }

        private static void Car(ShapeKit kit, Color paint)
        {
            Color tire = new Color(0.10f, 0.10f, 0.11f);
            kit.Box("Auto", new Vector3(0f, 0.5f, 0f), new Vector3(3.2f, 0.6f, 1.5f), paint);
            kit.Box("Cabina", new Vector3(-0.2f, 1.05f, 0f), new Vector3(1.7f, 0.5f, 1.4f), new Color(0.62f, 0.80f, 0.92f));
            kit.Box("Ruedas", new Vector3(-1.0f, 0.22f, 0f), new Vector3(0.5f, 0.44f, 1.6f), tire);
            kit.Box("Ruedas", new Vector3(1.0f, 0.22f, 0f), new Vector3(0.5f, 0.44f, 1.6f), tire);
        }

        /// <summary>Adonde va el jugador esa semana (fin del camino amarillo): el patio del colegio o el paradero.</summary>
        private static Vector3 Destination(Anchors anchors, DailyPlace place) =>
            place == DailyPlace.School
                ? anchors["ancla.colegio"].localPosition + new Vector3(0f, 0.04f, 3.6f)
                : anchors["ancla.paradero"].localPosition + new Vector3(0f, 0.08f, 0f);

        private static void Ground(ShapeKit kit)
        {
            kit.Box("Suelo", new Vector3(0f, -0.06f, 0f), new Vector3(44f, 0.1f, 32f), new Color(0.62f, 0.68f, 0.55f));
            kit.Box("Calle", new Vector3(0f, 0f, StreetZ), new Vector3(44f, 0.02f, 5f), new Color(0.32f, 0.32f, 0.34f));
            kit.Box("Vereda norte", new Vector3(0f, 0.04f, StreetZ + 3.1f), new Vector3(44f, 0.08f, 1.2f), new Color(0.80f, 0.78f, 0.74f));
            kit.Box("Vereda sur", new Vector3(0f, 0.04f, StreetZ - 3.1f), new Vector3(44f, 0.08f, 1.2f), new Color(0.80f, 0.78f, 0.74f));
            for (float x = -20f; x <= 20f; x += 4f)
            {
                kit.Box("Linea", new Vector3(x, 0.02f, StreetZ), new Vector3(1.6f, 0.01f, 0.15f), new Color(0.95f, 0.95f, 0.90f));
            }

            for (float x = -18f; x <= 9f; x += 9f)
            {
                Tree(kit, Spot(kit, "Lugar del arbol", new Vector3(x + 2.5f, 0f, StreetZ + 3.4f)));
            }
        }

        /// <summary>
        /// Una casa: hogares humildes con techo plano de zinc y un piso; hogares acomodados
        /// con dos pisos, techo de tejas y antejardín. Todas con reja, como es común en Chile.
        /// El modelo depende del nivel: barrio.casa_humilde, barrio.casa_media o barrio.casa_acomodada.
        /// La marca amarilla de la casa del jugador va aparte, así queda aunque se cambie el modelo.
        /// </summary>
        private static void House(ShapeKit kit, Transform anchor, int level, Color wall, bool isPlayer)
        {
            bool twoStories = level >= 60;
            float height = twoStories ? 4.6f : 2.5f;
            float width = level >= 35 ? 4.4f : 3.6f;
            const float depth = 4f;
            string id = level < 35 ? "barrio.casa_humilde" : level < 60 ? "barrio.casa_media" : "barrio.casa_acomodada";

            Models.Place(kit, anchor, id, k =>
            {
                k.Box("Casa", new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth), wall);
                k.Box("Puerta", new Vector3(-0.8f, 1.0f, (-depth / 2f) - 0.02f), new Vector3(0.8f, 2.0f, 0.06f), Palette.DarkWood);
                k.Box("Ventana", new Vector3(0.8f, 1.4f, (-depth / 2f) - 0.02f), new Vector3(1.1f, 0.8f, 0.06f), new Color(0.62f, 0.80f, 0.92f));

                Color roofColor = isPlayer ? Palette.Player.color : level < 35 ? new Color(0.62f, 0.64f, 0.66f) : new Color(0.66f, 0.32f, 0.24f);
                if (level < 35)
                {
                    k.Box("Techo de zinc", new Vector3(0f, height + 0.08f, 0f), new Vector3(width + 0.4f, 0.16f, depth + 0.4f), roofColor);
                }
                else
                {
                    // Techo a dos aguas: un cubo girado 45° del que solo se ve la mitad de arriba.
                    GameObject roof = k.Box("Techo de tejas", new Vector3(0f, height, 0f), new Vector3(width + 0.3f, depth * 0.72f, depth * 0.72f), roofColor);
                    roof.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                }

                // Reja y antejardín hacia la calle.
                k.Box("Reja", new Vector3(0f, 0.55f, (-depth / 2f) - 1.6f), new Vector3(width + 1.2f, 1.1f, 0.06f), new Color(0.25f, 0.25f, 0.27f));
                if (level >= 60)
                {
                    k.Box("Antejardin", new Vector3(0f, 0.02f, (-depth / 2f) - 0.8f), new Vector3(width + 1.0f, 0.04f, 1.4f), new Color(0.45f, 0.65f, 0.40f));
                }
            });

            if (isPlayer)
            {
                Vector3 at = anchor.localPosition;
                float fenceZ = at.z - (depth / 2f) - 1.6f;
                kit.Shape(PrimitiveType.Cylinder, "Tu casa", new Vector3(at.x, 0.05f, fenceZ - 0.9f), new Vector3(1.4f, 0.02f, 1.4f), Palette.PlayerMark);

                // Alfiler amarillo sobre el techo: se ve desde lejos cuál es tu casa.
                float top = height + (level < 35 ? 0.2f : (depth * 0.36f));
                kit.Box("Alfiler", new Vector3(at.x, top + 0.8f, at.z), new Vector3(0.12f, 1.6f, 0.12f), Palette.PlayerMark);
                kit.Ball("Alfiler", new Vector3(at.x, top + 1.9f, at.z), 0.9f, Palette.PlayerMark);
            }
        }

        private static void School(ShapeKit kit)
        {
            kit.Box("Colegio", new Vector3(0f, 1.6f, 0f), new Vector3(9f, 3.2f, 5f), new Color(0.92f, 0.86f, 0.70f));
            kit.Box("Franja del colegio", new Vector3(0f, 2.6f, 2.52f), new Vector3(9f, 0.5f, 0.06f), new Color(0.25f, 0.40f, 0.65f));
            kit.Box("Patio", new Vector3(0f, 0.02f, 3.6f), new Vector3(9f, 0.04f, 2.2f), new Color(0.75f, 0.72f, 0.66f));
        }

        private static void Store(ShapeKit kit)
        {
            kit.Box("Almacen", new Vector3(0f, 1.3f, 0f), new Vector3(4f, 2.6f, 4f), new Color(0.96f, 0.95f, 0.90f));
            kit.Box("Letrero del almacen", new Vector3(0f, 2.2f, 2.02f), new Vector3(3.6f, 0.5f, 0.06f), new Color(0.80f, 0.20f, 0.20f));
        }

        private static void Plaza(ShapeKit kit)
        {
            kit.Box("Plaza", new Vector3(0f, 0.02f, 0f), new Vector3(9f, 0.04f, 7f), new Color(0.45f, 0.66f, 0.40f));
            kit.Box("Banca", new Vector3(0f, 0.25f, 2.6f), new Vector3(1.8f, 0.5f, 0.5f), new Color(0.55f, 0.40f, 0.28f));
            Tree(kit, Spot(kit, "Lugar del arbol", new Vector3(-3f, 0f, -0.6f)));
            Tree(kit, Spot(kit, "Lugar del arbol", new Vector3(1.5f, 0f, -0.6f)));
            Tree(kit, Spot(kit, "Lugar del arbol", new Vector3(2.4f, 0f, 1.8f)));
        }

        private static void BusStop(ShapeKit kit)
        {
            kit.Box("Poste del paradero", new Vector3(-0.9f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.1f), new Color(0.30f, 0.30f, 0.32f));
            kit.Box("Poste del paradero", new Vector3(0.9f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.1f), new Color(0.30f, 0.30f, 0.32f));
            kit.Box("Techo del paradero", new Vector3(0f, 2.25f, 0f), new Vector3(2.4f, 0.1f, 1.0f), new Color(0.85f, 0.30f, 0.25f));
        }

        private static void Tree(ShapeKit kit, Transform spot)
        {
            Models.Place(kit, spot, "barrio.arbol", k =>
            {
                k.Box("Tronco", new Vector3(0f, 0.8f, 0f), new Vector3(0.25f, 1.6f, 0.25f), new Color(0.45f, 0.32f, 0.22f));
                k.Ball("Copa", new Vector3(0f, 2.1f, 0f), 1.6f, new Color(0.32f, 0.55f, 0.32f));
            });
        }

        /// <summary>Puntos amarillos desde la casa del jugador hasta adonde va esa semana.</summary>
        private static void Path(ShapeKit kit, Anchors anchors, DailyPlace place)
        {
            if (place == DailyPlace.Home)
            {
                return;
            }

            Vector3 start = anchors[PlayerHouse].localPosition + new Vector3(0f, 0.1f, -4f);
            Vector3 end = Destination(anchors, place) + new Vector3(0f, 0.06f, 0f);
            end.y = 0.1f;

            // Baja a la vereda, cruza la calle y sigue por la vereda sur hasta el destino.
            Dots(kit, start, new Vector3(start.x, 0.1f, StreetZ + 3.1f));
            Dots(kit, new Vector3(start.x, 0.1f, StreetZ + 3.1f), new Vector3(start.x, 0.1f, StreetZ - 3.1f));
            Dots(kit, new Vector3(start.x, 0.1f, StreetZ - 3.1f), new Vector3(end.x, 0.1f, StreetZ - 3.1f));
            Dots(kit, new Vector3(end.x, 0.1f, StreetZ - 3.1f), end);
        }

        private static void Dots(ShapeKit kit, Vector3 from, Vector3 to)
        {
            float length = Vector3.Distance(from, to);
            int count = Mathf.Max(1, Mathf.RoundToInt(length / 0.9f));
            for (int i = 0; i <= count; i++)
            {
                kit.Ball("Camino", Vector3.Lerp(from, to, i / (float)count), 0.3f, Palette.PlayerMark);
            }
        }
    }
}
