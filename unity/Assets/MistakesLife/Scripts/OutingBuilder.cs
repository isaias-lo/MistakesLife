#nullable enable

using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma la escena de una salida del barrio (D5c2, boceto BSalida): el lugar donde fuiste visto
    /// de cerca (la sala del cine, la mesa del café, la plaza, la ramada de la fonda...) con tu
    /// figura y la de quien fue contigo. La escena sigue lo que te pasa: si vas al cine con Tomás,
    /// se ve el cine con Tomás. Cada lugar se puede cambiar por un modelo ("salida.mall"...).
    /// </summary>
    public static class OutingBuilder
    {
        private const float Width = 7f;
        private const float Depth = 5.5f;

        public static void Build(ShapeKit kit, OutingView outing)
        {
            var anchor = new GameObject("Salida").transform;
            anchor.SetParent(kit.Parent, false);
            Models.Place(kit, anchor, "salida." + outing.Shape, k => Draw(k, outing.Shape));

            // Tú y quien fue contigo, adelante, mirando a la cámara.
            Vector3 you = outing.Companion == null ? new Vector3(0f, 0f, -1.2f) : new Vector3(-0.55f, 0f, -1.2f);
            RoomBuilder.Person(kit, outing.Player, you, 0, markerSize: 0f);
            if (outing.Companion != null)
            {
                HomeMemberView companion = outing.Companion;
                PlaceBuilder.Named(kit, PlaceBuilder.PersonPrefix + companion.Id, k => RoomBuilder.Person(k, companion, new Vector3(0.55f, 0f, -1.2f), 1));
            }
        }

        private static void Draw(ShapeKit k, string shape)
        {
            switch (shape)
            {
                case "mall":
                    // La sala del cine: butacas rojas y la pantalla encendida.
                    Floor(k, new Color(0.23f, 0.18f, 0.23f), new Color(0.17f, 0.13f, 0.19f));
                    k.Box("Pantalla", new Vector3(0f, 1.8f, Depth / 2f - 0.1f), new Vector3(5.6f, 2.2f, 0.05f), new Color(0.86f, 0.90f, 0.94f));
                    for (int row = 0; row < 3; row++)
                    {
                        for (int seat = -3; seat <= 3; seat++)
                        {
                            k.Box("Butaca", new Vector3(seat * 0.75f, 0.3f, row * 0.9f - 0.2f), new Vector3(0.6f, 0.6f, 0.55f), new Color(0.56f, 0.18f, 0.23f));
                        }
                    }

                    break;
                case "cafe":
                    Floor(k, new Color(0.62f, 0.48f, 0.35f), new Color(0.90f, 0.84f, 0.74f));
                    k.Box("Mesa", new Vector3(0f, 0.72f, 0.2f), new Vector3(1.0f, 0.06f, 0.8f), new Color(0.40f, 0.28f, 0.18f));
                    k.Box("Pata de la mesa", new Vector3(0f, 0.36f, 0.2f), new Vector3(0.08f, 0.7f, 0.08f), new Color(0.30f, 0.22f, 0.16f));
                    k.Shape(PrimitiveType.Cylinder, "Taza", new Vector3(-0.2f, 0.82f, 0.2f), new Vector3(0.12f, 0.06f, 0.12f), Color.white);
                    k.Shape(PrimitiveType.Cylinder, "Taza", new Vector3(0.2f, 0.82f, 0.2f), new Vector3(0.12f, 0.06f, 0.12f), Color.white);
                    k.Box("Mesón", new Vector3(-2.2f, 0.55f, 1.8f), new Vector3(2.2f, 1.1f, 0.7f), new Color(0.23f, 0.48f, 0.36f));
                    break;
                case "bar":
                    Floor(k, new Color(0.30f, 0.22f, 0.20f), new Color(0.36f, 0.27f, 0.30f));
                    k.Box("Barra", new Vector3(0f, 0.55f, 1.4f), new Vector3(4.2f, 1.1f, 0.6f), new Color(0.40f, 0.28f, 0.18f));
                    k.Box("Repisa", new Vector3(0f, 1.6f, Depth / 2f - 0.15f), new Vector3(4.0f, 0.08f, 0.3f), new Color(0.30f, 0.22f, 0.16f));
                    for (int i = -4; i <= 4; i++)
                    {
                        k.Shape(PrimitiveType.Cylinder, "Botella", new Vector3(i * 0.42f, 1.78f, Depth / 2f - 0.15f), new Vector3(0.1f, 0.16f, 0.1f), new Color(0.25f + (i & 1) * 0.3f, 0.45f, 0.30f));
                    }

                    break;
                case "gimnasio":
                    Floor(k, new Color(0.25f, 0.26f, 0.29f), new Color(0.80f, 0.82f, 0.85f));
                    k.Box("Máquina", new Vector3(-2.0f, 0.6f, 1.2f), new Vector3(0.8f, 1.2f, 1.4f), new Color(0.35f, 0.36f, 0.40f));
                    k.Box("Trotadora", new Vector3(2.0f, 0.25f, 1.2f), new Vector3(0.8f, 0.2f, 1.6f), new Color(0.20f, 0.21f, 0.24f));
                    k.Box("Barra de pesas", new Vector3(0f, 0.25f, 1.6f), new Vector3(1.6f, 0.05f, 0.05f), new Color(0.70f, 0.70f, 0.72f));
                    k.Box("Espejo", new Vector3(0f, 1.3f, Depth / 2f - 0.08f), new Vector3(5.0f, 1.6f, 0.04f), new Color(0.75f, 0.84f, 0.90f));
                    break;
                case "plaza":
                case "parque":
                    k.Box("Pasto", new Vector3(0f, -0.05f, 0f), new Vector3(Width, 0.1f, Depth), new Color(0.52f, 0.66f, 0.44f));
                    k.Box("Camino", new Vector3(0f, 0.01f, -0.6f), new Vector3(Width, 0.02f, 1.2f), new Color(0.80f, 0.74f, 0.62f));
                    k.Box("Banca", new Vector3(-1.8f, 0.3f, 0.6f), new Vector3(1.2f, 0.08f, 0.4f), new Color(0.58f, 0.42f, 0.28f));
                    Tree(k, new Vector3(-2.6f, 0f, 1.8f));
                    Tree(k, new Vector3(2.4f, 0f, 1.6f));
                    Tree(k, new Vector3(0.4f, 0f, 2.2f));
                    if (shape == "parque")
                    {
                        k.Box("Laguna", new Vector3(1.6f, 0.02f, 0.9f), new Vector3(1.8f, 0.03f, 1.2f), new Color(0.55f, 0.71f, 0.84f));
                    }

                    break;
                case "fonda":
                    k.Box("Tierra", new Vector3(0f, -0.05f, 0f), new Vector3(Width, 0.1f, Depth), new Color(0.72f, 0.62f, 0.46f));
                    k.Box("Ramada", new Vector3(0f, 1.2f, 1.6f), new Vector3(5.0f, 0.15f, 1.8f), new Color(0.85f, 0.75f, 0.48f));
                    for (int i = -1; i <= 1; i += 2)
                    {
                        k.Box("Poste", new Vector3(i * 2.3f, 0.6f, 1.6f), new Vector3(0.12f, 1.2f, 0.12f), new Color(0.50f, 0.38f, 0.24f));
                    }

                    Color[] flags = { new Color(0.84f, 0.17f, 0.12f), Color.white, new Color(0.14f, 0.25f, 0.48f) };
                    for (int i = 0; i < 11; i++)
                    {
                        k.Box("Banderín", new Vector3(-2.5f + i * 0.5f, 1.55f, 0.7f), new Vector3(0.22f, 0.22f, 0.02f), flags[i % 3]);
                    }

                    k.Box("Mesón de empanadas", new Vector3(-1.6f, 0.45f, 1.9f), new Vector3(1.6f, 0.9f, 0.6f), new Color(0.40f, 0.28f, 0.18f));
                    break;
                case "cancha_futbol5":
                    k.Box("Cancha", new Vector3(0f, -0.05f, 0f), new Vector3(Width, 0.1f, Depth), new Color(0.30f, 0.60f, 0.34f));
                    k.Box("Línea", new Vector3(0f, 0.01f, 0.6f), new Vector3(Width - 0.4f, 0.02f, 0.05f), Color.white);
                    k.Box("Arco", new Vector3(0f, 0.6f, Depth / 2f - 0.3f), new Vector3(2.4f, 1.2f, 0.08f), Color.white);
                    k.Ball("Pelota", new Vector3(1.4f, 0.12f, -0.6f), 0.24f, Color.white);
                    k.Box("Reja", new Vector3(0f, 1.0f, Depth / 2f), new Vector3(Width, 2.0f, 0.03f), new Color(0.55f, 0.57f, 0.60f));
                    break;
                default:
                    // La casa de tus amigos y cualquier otro lugar: un living.
                    Floor(k, new Color(0.70f, 0.55f, 0.38f), new Color(0.92f, 0.88f, 0.80f));
                    k.Box("Sillón", new Vector3(0f, 0.35f, 1.6f), new Vector3(2.4f, 0.7f, 0.8f), new Color(0.45f, 0.55f, 0.68f));
                    k.Box("Mesa de centro", new Vector3(0f, 0.3f, 0.5f), new Vector3(1.2f, 0.08f, 0.6f), new Color(0.40f, 0.28f, 0.18f));
                    k.Box("Tele", new Vector3(-2.6f, 1.0f, 0.4f), new Vector3(0.08f, 0.7f, 1.2f), new Color(0.12f, 0.12f, 0.14f));
                    break;
            }
        }

        /// <summary>Piso y dos muros (fondo y lado), como una maqueta abierta.</summary>
        private static void Floor(ShapeKit k, Color floor, Color wall)
        {
            k.Box("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(Width, 0.1f, Depth), floor);
            k.Box("Muro del fondo", new Vector3(0f, 1.4f, (Depth / 2f) + 0.05f), new Vector3(Width, 2.8f, 0.1f), wall);
            k.Box("Muro lateral", new Vector3((-Width / 2f) - 0.05f, 1.4f, 0f), new Vector3(0.1f, 2.8f, Depth + 0.2f), wall * 0.92f);
        }

        private static void Tree(ShapeKit k, Vector3 at)
        {
            k.Box("Tronco", at + new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 1.0f, 0.18f), new Color(0.42f, 0.31f, 0.21f));
            k.Ball("Copa", at + new Vector3(0f, 1.4f, 0f), 1.1f, new Color(0.31f, 0.54f, 0.36f));
        }
    }
}
