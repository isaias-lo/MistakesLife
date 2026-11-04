#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma la pieza de la casa con formas simples: muros, muebles según el nivel del hogar,
    /// el rincón del jugador según su etapa y una figura por persona.
    /// </summary>
    public static class RoomBuilder
    {
        public static void BuildRoom(ShapeKit kit, HomeView home)
        {
            int level = home.HouseholdLevel;
            float width = 6f + (level / 25f);
            const float depth = 5f;

            Color floor = level < 35 ? new Color(0.62f, 0.62f, 0.60f) : new Color(0.66f, 0.50f, 0.34f);
            Color wall = level < 35 ? new Color(0.86f, 0.74f, 0.60f) : level < 65 ? new Color(0.86f, 0.80f, 0.68f) : new Color(0.90f, 0.88f, 0.82f);

            kit.Box("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(width, 0.1f, depth), floor);
            kit.Box("Muro del fondo", new Vector3(0f, 1.3f, (depth / 2f) + 0.05f), new Vector3(width, 2.6f, 0.1f), wall);
            kit.Box("Muro lateral", new Vector3((-width / 2f) - 0.05f, 1.3f, 0f), new Vector3(0.1f, 2.6f, depth + 0.2f), wall * 0.92f);
            kit.Box("Ventana", new Vector3(width / 4f, 1.6f, (depth / 2f) - 0.01f), new Vector3(1.4f, 0.9f, 0.04f), new Color(0.62f, 0.80f, 0.92f));
            kit.Box("Puerta", new Vector3(-width / 2f + 0.01f, 1.0f, -depth / 4f), new Vector3(0.04f, 2.0f, 0.9f), new Color(0.45f, 0.32f, 0.22f));

            if (level >= 50)
            {
                kit.Box("Alfombra", new Vector3(0.6f, 0.01f, -0.6f), new Vector3(2.4f, 0.02f, 1.6f), new Color(0.70f, 0.42f, 0.36f));
            }
        }

        public static void BuildFurniture(ShapeKit kit, HomeView home)
        {
            int level = home.HouseholdLevel;
            float left = -2.4f;

            // Rincón del jugador según su etapa.
            switch (home.PlayerStage)
            {
                case LifeStage.Baby:
                    kit.Box("Cuna", new Vector3(left, 0.35f, 1.6f), new Vector3(1.0f, 0.7f, 0.6f), new Color(0.95f, 0.92f, 0.85f));
                    kit.Box("Colchon de la cuna", new Vector3(left, 0.55f, 1.6f), new Vector3(0.9f, 0.1f, 0.5f), new Color(0.75f, 0.85f, 0.95f));
                    break;
                case LifeStage.Child:
                case LifeStage.Teen:
                    kit.Box("Cama", new Vector3(left, 0.25f, 1.4f), new Vector3(0.9f, 0.5f, 1.9f), new Color(0.40f, 0.55f, 0.75f));
                    kit.Box("Escritorio", new Vector3(left + 1.6f, 0.4f, 2.1f), new Vector3(1.2f, 0.8f, 0.6f), new Color(0.72f, 0.58f, 0.40f));
                    break;
                default:
                    // De adulto, otro adulto en la casa es la pareja: cama de dos plazas.
                    bool couple = false;
                    foreach (HomeMemberView member in home.Members)
                    {
                        couple |= !member.IsPlayer && member.Stage >= LifeStage.Adult;
                    }

                    kit.Box("Cama", new Vector3(left + 0.3f, 0.25f, 1.4f), new Vector3(couple ? 1.6f : 0.9f, 0.5f, 1.9f), new Color(0.55f, 0.45f, 0.62f));
                    break;
            }

            // Comedor: siempre hay una mesa.
            kit.Box("Mesa", new Vector3(1.6f, 0.4f, 0.9f), new Vector3(1.4f, 0.08f, 0.9f), new Color(0.58f, 0.42f, 0.28f));
            kit.Box("Pata de la mesa", new Vector3(1.6f, 0.2f, 0.9f), new Vector3(0.12f, 0.4f, 0.12f), new Color(0.40f, 0.28f, 0.18f));

            if (level >= 25)
            {
                kit.Box("Sillon", new Vector3(0.4f, 0.3f, -1.6f), new Vector3(1.8f, 0.6f, 0.7f), new Color(0.35f, 0.45f, 0.40f));
            }

            if (level >= 40)
            {
                kit.Box("Tele", new Vector3(0.4f, 0.9f, 2.35f), new Vector3(1.2f, 0.7f, 0.06f), new Color(0.12f, 0.12f, 0.14f));
            }

            if (level >= 60)
            {
                kit.Box("Macetero", new Vector3(2.8f, 0.25f, 2.0f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.60f, 0.38f, 0.26f));
                kit.Ball("Planta", new Vector3(2.8f, 0.8f, 2.0f), 0.7f, new Color(0.30f, 0.55f, 0.30f));
            }
        }

        public static void BuildPeople(ShapeKit kit, IReadOnlyList<HomeMemberView> members)
        {
            float spacing = 0.9f;
            float start = -((members.Count - 1) * spacing) / 2f;
            for (int i = 0; i < members.Count; i++)
            {
                HomeMemberView member = members[i];
                Vector3 position = new Vector3(start + (i * spacing), 0f, -0.4f);
                Person(kit, member, position, i);
            }
        }

        /// <summary>Una figura: cuerpo, cabeza y, para el jugador, un círculo amarillo a sus pies.</summary>
        public static void Person(ShapeKit kit, HomeMemberView member, Vector3 feet, int index)
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
                new Color(0.80f, 0.65f, 0.30f), new Color(0.60f, 0.45f, 0.75f),
            };
            Color shirt = member.IsPlayer ? UiFactory.Accent : shirts[index % shirts.Length];
            Color skin = new Color(0.86f, 0.68f, 0.55f);
            Color hair = member.Stage == LifeStage.Elder ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.25f, 0.18f, 0.12f);

            float head = height * 0.22f;
            float body = height - head;
            GameObject torso = kit.Shape(PrimitiveType.Capsule, member.Name, feet + new Vector3(0f, body / 2f, 0f), new Vector3(body * 0.45f, body / 2f, body * 0.45f), shirt);
            kit.Ball("Cabeza", feet + new Vector3(0f, body + (head / 2f), 0f), head, skin).transform.SetParent(torso.transform, true);
            float hairSize = member.IsFemale ? head * 1.12f : head * 1.02f;
            kit.Ball("Pelo", feet + new Vector3(0f, body + (head * 0.62f), 0.04f), hairSize, hair).transform.SetParent(torso.transform, true);

            if (member.IsPlayer)
            {
                kit.Shape(PrimitiveType.Cylinder, "Marca del jugador", feet + new Vector3(0f, 0.02f, 0f), new Vector3(0.7f, 0.01f, 0.7f), new Color(0.98f, 0.82f, 0.25f));
            }
        }
    }
}
