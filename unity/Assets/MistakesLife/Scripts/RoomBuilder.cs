#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma la pieza de la casa: muros y piso según el nivel del hogar, los muebles en sus
    /// anclas (<see cref="Anchors"/>) y una figura por persona. Cada mueble tiene un id
    /// ("mueble.mesa"...) y se puede reemplazar por un modelo (<see cref="Models"/>).
    /// </summary>
    public static class RoomBuilder
    {
        /// <summary>Nombre del lugar: las anclas editables van en Resources/Lugares/pieza.prefab.</summary>
        public const string Place = "pieza";

        /// <summary>El ancho de la pieza crece con el nivel del hogar (de 6 a 10 metros).</summary>
        public static float Width(int level) => 6f + (level / 25f);

        private const float Depth = 5f;

        /// <summary>
        /// Dónde va cada cosa si nadie movió las anclas. Las del muro lateral y la ventana
        /// dependen del ancho de la pieza; las demás son fijas.
        /// </summary>
        public static IReadOnlyDictionary<string, Vector3> DefaultAnchors(int level)
        {
            float width = Width(level);
            return new Dictionary<string, Vector3>
            {
                ["ancla.cama"] = new Vector3(-2.4f, 0f, 1.4f),
                ["ancla.escritorio"] = new Vector3(-0.8f, 0f, 2.1f),
                ["ancla.mesa"] = new Vector3(1.6f, 0f, 0.9f),
                ["ancla.sillon"] = new Vector3(2.6f, 0f, -0.4f),
                ["ancla.tele"] = new Vector3(0.4f, 0.9f, 2.35f),
                ["ancla.planta"] = new Vector3(2.8f, 0f, 2.0f),
                ["ancla.alfombra"] = new Vector3(0.6f, 0f, -0.6f),
                ["ancla.cuadro_izquierda"] = new Vector3(-1.0f, 1.85f, 2.44f),
                ["ancla.cuadro_derecha"] = new Vector3(0.4f, 1.95f, 2.44f),
                ["ancla.ventana"] = new Vector3(width / 4f, 0f, Depth / 2f),
                ["ancla.muro_lateral"] = new Vector3(-width / 2f, 0f, -0.2f),
                ["ancla.puerta"] = new Vector3(-width / 2f, 0f, -Depth / 4f),
                ["ancla.mochila"] = new Vector3(-1.45f, 0f, 0.3f),
                ["ancla.piso"] = new Vector3(0f, 0f, -1.6f),
                ["ancla.mecedora"] = new Vector3(2.6f, 0f, -1.7f),
            };
        }

        /// <summary>Crea las anclas de la pieza (las de siempre, o las movidas en el editor).</summary>
        public static Anchors CreateAnchors(Transform room, HomeView home) =>
            Anchors.Create(room, Place, DefaultAnchors(home.HouseholdLevel));

        public static void BuildRoom(ShapeKit kit, HomeView home, Anchors anchors)
        {
            int level = home.HouseholdLevel;
            float width = Width(level);

            Color floor = level < 35 ? new Color(0.62f, 0.62f, 0.60f) : new Color(0.66f, 0.50f, 0.34f);
            Color wall = level < 35 ? new Color(0.86f, 0.74f, 0.60f) : level < 65 ? new Color(0.86f, 0.80f, 0.68f) : new Color(0.90f, 0.88f, 0.82f);

            kit.Box("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(width, 0.1f, Depth), floor);
            kit.Box("Muro del fondo", new Vector3(0f, 1.3f, (Depth / 2f) + 0.05f), new Vector3(width, 2.6f, 0.1f), wall);
            kit.Box("Muro lateral", new Vector3((-width / 2f) - 0.05f, 1.3f, 0f), new Vector3(0.1f, 2.6f, Depth + 0.2f), wall * 0.92f);

            Models.Place(kit, anchors["ancla.ventana"], "mueble.ventana", k =>
                k.Box("Ventana", new Vector3(0f, 1.6f, -0.01f), new Vector3(1.4f, 0.9f, 0.04f), new Color(0.62f, 0.80f, 0.92f)));
            Models.Place(kit, anchors["ancla.puerta"], "mueble.puerta", k =>
                k.Box("Puerta", new Vector3(0.01f, 1.0f, 0f), new Vector3(0.04f, 2.0f, 0.9f), Palette.DarkWood));

            if (level >= 50)
            {
                Models.Place(kit, anchors["ancla.alfombra"], "mueble.alfombra", k =>
                    k.Box("Alfombra", new Vector3(0f, 0.01f, 0f), new Vector3(2.4f, 0.02f, 1.6f), new Color(0.70f, 0.42f, 0.36f)));
            }
        }

        public static void BuildFurniture(ShapeKit kit, HomeView home, Anchors anchors)
        {
            int level = home.HouseholdLevel;
            Transform bed = anchors["ancla.cama"];

            // Rincón del jugador según su etapa.
            switch (home.PlayerStage)
            {
                case LifeStage.Baby:
                    Models.Place(kit, bed, "mueble.cuna", k =>
                    {
                        k.Box("Cuna", new Vector3(0f, 0.35f, 0.2f), new Vector3(1.0f, 0.7f, 0.6f), new Color(0.95f, 0.92f, 0.85f));
                        k.Box("Colchon de la cuna", new Vector3(0f, 0.55f, 0.2f), new Vector3(0.9f, 0.1f, 0.5f), new Color(0.75f, 0.85f, 0.95f));
                    });
                    break;
                case LifeStage.Child:
                case LifeStage.Teen:
                    Models.Place(kit, bed, "mueble.cama", k =>
                        k.Box("Cama", new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 1.9f), new Color(0.40f, 0.55f, 0.75f)));
                    Models.Place(kit, anchors["ancla.escritorio"], "mueble.escritorio", k =>
                        k.Box("Escritorio", new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 0.6f), Palette.Wood));
                    break;
                default:
                    // De adulto, otro adulto en la casa es la pareja: cama de dos plazas.
                    bool couple = false;
                    foreach (HomeMemberView member in home.Members)
                    {
                        couple |= !member.IsPlayer && member.Stage >= LifeStage.Adult;
                    }

                    Models.Place(kit, bed, couple ? "mueble.cama_doble" : "mueble.cama_adulto", k =>
                        k.Box("Cama", new Vector3(0.3f, 0.25f, 0f), new Vector3(couple ? 1.6f : 0.9f, 0.5f, 1.9f), new Color(0.55f, 0.45f, 0.62f)));
                    break;
            }

            // Comedor: siempre hay una mesa.
            Models.Place(kit, anchors["ancla.mesa"], "mueble.mesa", k =>
            {
                k.Box("Mesa", new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.08f, 0.9f), Palette.Wood);
                k.Box("Pata de la mesa", new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), Palette.DarkWood);
            });

            if (level >= 25)
            {
                // A un costado, a lo largo: delante de la familia tapaba medio cuerpo a la cámara.
                Models.Place(kit, anchors["ancla.sillon"], "mueble.sillon", k =>
                    k.Box("Sillon", new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.6f, 1.6f), new Color(0.35f, 0.45f, 0.40f)));
            }

            if (level >= 40)
            {
                Models.Place(kit, anchors["ancla.tele"], "mueble.tele", k =>
                    k.Box("Tele", Vector3.zero, new Vector3(1.2f, 0.7f, 0.06f), Palette.Screen));
            }

            if (level >= 60)
            {
                Models.Place(kit, anchors["ancla.planta"], "mueble.planta", k =>
                {
                    k.Box("Macetero", new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.60f, 0.38f, 0.26f));
                    k.Ball("Planta", new Vector3(0f, 0.8f, 0f), 0.7f, new Color(0.30f, 0.55f, 0.30f));
                });
            }
        }

        public static void BuildPeople(ShapeKit kit, IReadOnlyList<HomeMemberView> members)
        {
            // En zigzag (una fila adelante y otra atrás) para que, vistos en diagonal
            // desde la cámara, nadie quede tapando a otro.
            float spacing = 1.0f;
            float start = -((members.Count - 1) * spacing) / 2f;
            for (int i = 0; i < members.Count; i++)
            {
                HomeMemberView member = members[i];
                float z = i % 2 == 0 ? -0.9f : 0.0f;
                Vector3 position = new Vector3(start + (i * spacing), 0f, z);
                Person(kit, member, position, i);
            }
        }

        /// <summary>
        /// Una figura estilo juguete de madera (docs/diseno-app.md, sección 4): cuerpo de una
        /// pieza, cabeza redonda con la cara pintada (ojos de punto, mejillas y una boca que
        /// sonríe, queda seria o se pone triste según su felicidad), pelo como casco y, en la
        /// vejez, pelo gris, lentes y, desde los 80, bastón. El jugador va de amarillo y, si
        /// <paramref name="markerSize"/> es mayor que 0, con un círculo a sus pies y una bolita
        /// sobre la cabeza.
        /// </summary>
        public static void Person(ShapeKit kit, HomeMemberView member, Vector3 feet, int index, float markerSize = 0.7f)
        {
            float height = member.Stage switch
            {
                LifeStage.Baby => 0.45f,
                LifeStage.Child => 0.95f,
                LifeStage.Teen => 1.45f,
                LifeStage.Elder => 1.5f,
                _ => 1.65f,
            };

            Color[] shirts =
            {
                new Color(0.85f, 0.45f, 0.40f), new Color(0.35f, 0.55f, 0.80f), new Color(0.50f, 0.70f, 0.45f),
                new Color(0.60f, 0.45f, 0.75f), new Color(0.45f, 0.65f, 0.70f),
            };
            Color shirt = shirts[index % shirts.Length];
            // Cada persona con su color de pelo (fijo según su id); en la vejez, gris.
            Color[] hairs =
            {
                new Color(0.25f, 0.18f, 0.12f), new Color(0.12f, 0.10f, 0.09f), new Color(0.45f, 0.28f, 0.16f),
                new Color(0.62f, 0.48f, 0.32f), new Color(0.38f, 0.16f, 0.10f),
            };
            Color hair = member.Stage == LifeStage.Elder ? new Color(0.85f, 0.85f, 0.85f) : hairs[Mathf.Abs(member.Id) % hairs.Length];

            // Como un juguete: mientras más chico, más grande la cabeza.
            float head = height * (member.Stage switch
            {
                LifeStage.Baby => 0.34f,
                LifeStage.Child => 0.30f,
                LifeStage.Teen => 0.25f,
                _ => 0.22f,
            });
            float body = height - head;
            Vector3 torsoSize = new Vector3(body * 0.45f, body / 2f, body * 0.45f);
            GameObject torso = member.IsPlayer
                ? kit.Shape(PrimitiveType.Capsule, member.Name, feet + new Vector3(0f, body / 2f, 0f), torsoSize, Palette.Player)
                : kit.Shape(PrimitiveType.Capsule, member.Name, feet + new Vector3(0f, body / 2f, 0f), torsoSize, shirt);
            Vector3 center = feet + new Vector3(0f, body + (head / 2f), 0f);
            kit.Ball("Cabeza", center, head, Palette.Skin).transform.SetParent(torso.transform, true);

            // Pelo como casco: cubre arriba y atrás de la cabeza y deja la cara libre.
            float hairSize = member.IsFemale ? head * 1.12f : head * 1.06f;
            // Va arriba y hacia atrás, para no bajar hasta los ojos (sobre todo en los niños).
            GameObject helmet = kit.Shape(PrimitiveType.Sphere, "Pelo", center + new Vector3(0f, head * 0.20f, head * 0.16f), new Vector3(hairSize, hairSize * 0.80f, hairSize * 0.96f), hair);
            helmet.transform.SetParent(torso.transform, true);

            Face(kit, torso.transform, member, center, head);

            if (member.Stage == LifeStage.Elder && member.AgeInYears >= CaneFromAge)
            {
                // Bastón de madera oscura al lado, con su mango.
                float cane = height * 0.55f;
                Vector3 side = feet + new Vector3(body * 0.36f, 0f, -body * 0.08f);
                kit.Shape(PrimitiveType.Cylinder, "Baston", side + new Vector3(0f, cane / 2f, 0f), new Vector3(0.035f, cane / 2f, 0.035f), Palette.DarkWood).transform.SetParent(torso.transform, true);
                kit.Box("Mango del baston", side + new Vector3(-0.04f, cane, 0f), new Vector3(0.12f, 0.035f, 0.035f), Palette.DarkWood).transform.SetParent(torso.transform, true);
            }

            if (member.IsPlayer)
            {
                Material marker = Palette.PlayerMark;
                if (markerSize > 0f)
                {
                    kit.Shape(PrimitiveType.Cylinder, "Marca del jugador", feet + new Vector3(0f, 0.02f, 0f), new Vector3(markerSize, 0.01f, markerSize), marker);
                }

                // Bolita amarilla justo sobre el pelo, proporcional a la estatura: se ve aunque
                // un mueble tape el piso, sin quedar flotando lejos de la cabeza.
                float dot = Mathf.Max(0.16f, height * 0.12f);
                float top = body + (head * 0.70f) + (hairSize * 0.40f);
                kit.Ball("Marca sobre la cabeza", feet + new Vector3(0f, top + (height * 0.06f) + (dot / 2f), 0f), dot, marker);
            }
        }

        /// <summary>Desde esta edad la figura usa bastón.</summary>
        private const int CaneFromAge = 80;

        /// <summary>
        /// La cara se pinta mirando hacia la cámara de la pieza y del trabajo (que miran desde
        /// adelante y un poco a la derecha), para que se vea sin girar a nadie.
        /// </summary>
        private static readonly Quaternion FaceTurn = Quaternion.Euler(0f, -30f, 0f);

        private static readonly Color Paint = new Color(0.12f, 0.10f, 0.10f);
        private static readonly Color Cheek = new Color(0.93f, 0.55f, 0.55f);

        /// <summary>La cara pintada: ojos de punto, mejillas, boca según el ánimo y, en la vejez, lentes.</summary>
        private static void Face(ShapeKit kit, Transform parent, HomeMemberView member, Vector3 center, float head)
        {
            float r = head / 2f;
            Vector3 At(float x, float y, float z) => center + (FaceTurn * new Vector3(x * r, y * r, z * r));
            void Part(GameObject part, Quaternion tilt)
            {
                part.transform.rotation = FaceTurn * tilt;
                part.transform.SetParent(parent, true);
            }

            float eye = head * 0.12f;
            Part(kit.Ball("Ojo izquierdo", At(-0.34f, 0.22f, -0.90f), eye, Paint), Quaternion.identity);
            Part(kit.Ball("Ojo derecho", At(0.34f, 0.22f, -0.90f), eye, Paint), Quaternion.identity);
            Vector3 cheek = new Vector3(head * 0.17f, head * 0.10f, head * 0.05f);
            Part(kit.Shape(PrimitiveType.Sphere, "Mejilla izquierda", At(-0.54f, -0.02f, -0.82f), cheek, Cheek), Quaternion.identity);
            Part(kit.Shape(PrimitiveType.Sphere, "Mejilla derecha", At(0.54f, -0.02f, -0.82f), cheek, Cheek), Quaternion.identity);

            // La boca son dos trazos: con las puntas arriba sonríe, abajo está triste, rectos queda seria.
            float tilt = member.Mood * 18f;
            Vector3 stroke = new Vector3(head * 0.17f, head * 0.045f, head * 0.03f);
            Part(kit.Box("Boca izquierda", At(-0.15f, -0.16f, -0.96f), stroke, Paint), Quaternion.Euler(0f, 0f, -tilt));
            Part(kit.Box("Boca derecha", At(0.15f, -0.16f, -0.96f), stroke, Paint), Quaternion.Euler(0f, 0f, tilt));

            if (member.Stage == LifeStage.Elder)
            {
                // Lentes: el marco de arriba sobre los ojos y las patillas hacia atrás.
                Part(kit.Box("Lentes", At(0f, 0.40f, -0.88f), new Vector3(head * 0.78f, head * 0.04f, head * 0.04f), Paint), Quaternion.identity);
                Part(kit.Box("Patilla izquierda", At(-0.90f, 0.38f, -0.30f), new Vector3(head * 0.03f, head * 0.03f, head * 0.5f), Paint), Quaternion.identity);
                Part(kit.Box("Patilla derecha", At(0.90f, 0.38f, -0.30f), new Vector3(head * 0.03f, head * 0.03f, head * 0.5f), Paint), Quaternion.identity);
            }
        }
    }
}
