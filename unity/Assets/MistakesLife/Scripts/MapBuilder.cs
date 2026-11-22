#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma el mapa chico de tu barrio (D5c2, boceto BMapa): un terreno con calles y, en cada
    /// manzana, un lugar del barrio (tu casa, la plaza, el mall con cine, el gimnasio, el parque,
    /// el café, la casa de tus amigos, tu colegio, tu pega, un bar y los lugares de tu país, como
    /// la fonda en septiembre). Cada lugar va dentro de un objeto "mapa.&lt;id del lugar&gt;" para que
    /// <see cref="HomeScene.Pick"/> sepa qué se tocó. Las manzanas tienen anclas editables
    /// (Resources/Lugares/mapa.prefab) y cada forma su id de modelo ("mapa.mall", "mapa.fonda"...).
    /// </summary>
    public static class MapBuilder
    {
        /// <summary>Cómo empieza el nombre del objeto de un lugar del mapa: "mapa.lugar.barrio.mall".</summary>
        public const string PlacePrefix = "mapa.";

        /// <summary>Nombre del lugar: las anclas editables van en Resources/Lugares/mapa.prefab.</summary>
        public const string Place = "mapa";

        /// <summary>Las manzanas: 4 columnas y 3 filas, separadas por calles.</summary>
        private static readonly float[] Columns = { -6.3f, -2.1f, 2.1f, 6.3f };
        private static readonly float[] Rows = { 3.6f, -0.2f, -4.0f };

        private const float Width = 17f;
        private const float Depth = 12.4f;

        private static readonly Color Grass = new Color(0.56f, 0.66f, 0.48f);
        private static readonly Color Street = new Color(0.43f, 0.43f, 0.46f);
        private static readonly Color Sidewalk = new Color(0.74f, 0.72f, 0.68f);

        /// <summary>Cuántas manzanas tiene el mapa.</summary>
        public static int Slots => Columns.Length * Rows.Length;

        /// <summary>Dónde va cada manzana si nadie movió las anclas.</summary>
        public static IReadOnlyDictionary<string, Vector3> DefaultAnchors()
        {
            var anchors = new Dictionary<string, Vector3>();
            int slot = 1;
            foreach (float z in Rows)
            {
                foreach (float x in Columns)
                {
                    anchors[$"ancla.manzana_{slot}"] = new Vector3(x, 0f, z);
                    slot++;
                }
            }

            return anchors;
        }

        public static void Build(ShapeKit kit, NeighborhoodView map, HomeMemberView? player)
        {
            Anchors anchors = Anchors.Create(kit.Parent, Place, DefaultAnchors());
            kit.Box("Pasto", new Vector3(0f, -0.06f, 0f), new Vector3(Width, 0.1f, Depth), Grass);
            for (int i = 0; i < Columns.Length - 1; i++)
            {
                float x = (Columns[i] + Columns[i + 1]) / 2f;
                kit.Box("Calle", new Vector3(x, -0.005f, 0f), new Vector3(0.9f, 0.02f, Depth), Street);
            }

            for (int i = 0; i < Rows.Length - 1; i++)
            {
                float z = (Rows[i] + Rows[i + 1]) / 2f;
                kit.Box("Calle", new Vector3(0f, -0.004f, z), new Vector3(Width, 0.02f, 0.9f), Street);
            }

            int count = Mathf.Min(map.Places.Count, Slots);
            for (int i = 0; i < count; i++)
            {
                MapPlaceView place = map.Places[i];
                Transform anchor = anchors[$"ancla.manzana_{i + 1}"];
                var holder = new GameObject(PlacePrefix + place.Id);
                holder.transform.SetParent(anchor, false);
                Models.Place(kit, holder.transform, PlacePrefix + place.Shape, k => Draw(k, place.Shape));
                if (player != null && place.Opens == "casa")
                {
                    // Tú, en la vereda de tu casa.
                    RoomBuilder.Person(kit, player, anchor.localPosition + new Vector3(1.25f, 0f, -1.35f), 0, markerSize: 0.6f);
                }
            }
        }

        /// <summary>La forma simple de cada lugar, sobre su manzana (de unos 3 x 3).</summary>
        public static void Draw(ShapeKit k, string shape)
        {
            k.Box("Vereda", new Vector3(0f, 0.01f, 0f), new Vector3(3.3f, 0.04f, 3.1f), Sidewalk);
            switch (shape)
            {
                case "casa":
                case "casa_amigos":
                    Color wall = shape == "casa" ? new Color(0.91f, 0.85f, 0.75f) : new Color(0.89f, 0.87f, 0.80f);
                    Color roof = shape == "casa" ? new Color(0.71f, 0.33f, 0.24f) : new Color(0.43f, 0.50f, 0.60f);
                    k.Box("Casa", new Vector3(0f, 0.75f, 0.3f), new Vector3(2.2f, 1.5f, 1.8f), wall);
                    k.Box("Techo", new Vector3(0f, 1.62f, 0.3f), new Vector3(2.4f, 0.25f, 2.0f), roof);
                    k.Box("Puerta", new Vector3(0.4f, 0.45f, -0.61f), new Vector3(0.45f, 0.9f, 0.04f), new Color(0.40f, 0.28f, 0.18f));
                    k.Box("Ventana", new Vector3(-0.5f, 0.85f, -0.61f), new Vector3(0.55f, 0.45f, 0.04f), new Color(0.62f, 0.80f, 0.92f));
                    if (shape == "casa_amigos")
                    {
                        k.Box("Casa vecina", new Vector3(-0.6f, 0.6f, -1.0f), new Vector3(1.0f, 1.2f, 0.9f), new Color(0.84f, 0.88f, 0.78f));
                    }

                    break;
                case "plaza":
                    k.Box("Pasto de la plaza", new Vector3(0f, 0.04f, 0f), new Vector3(2.9f, 0.04f, 2.7f), new Color(0.66f, 0.73f, 0.56f));
                    Tree(k, new Vector3(-1.0f, 0f, 0.9f));
                    Tree(k, new Vector3(1.0f, 0f, 0.9f));
                    Tree(k, new Vector3(-1.0f, 0f, -0.9f));
                    k.Box("Banca", new Vector3(0.6f, 0.25f, -0.8f), new Vector3(0.9f, 0.08f, 0.3f), new Color(0.58f, 0.42f, 0.28f));
                    k.Box("Juegos", new Vector3(0.3f, 0.6f, 0.1f), new Vector3(0.12f, 1.2f, 0.12f), new Color(0.85f, 0.35f, 0.30f));
                    k.Box("Columpio", new Vector3(0.3f, 1.2f, 0.1f), new Vector3(0.9f, 0.08f, 0.08f), new Color(0.85f, 0.35f, 0.30f));
                    break;
                case "parque":
                    k.Box("Pasto del parque", new Vector3(0f, 0.04f, 0f), new Vector3(3.1f, 0.04f, 2.9f), new Color(0.50f, 0.65f, 0.42f));
                    k.Box("Laguna", new Vector3(0.6f, 0.07f, 0.3f), new Vector3(1.2f, 0.03f, 1.0f), new Color(0.55f, 0.71f, 0.84f));
                    Tree(k, new Vector3(-1.1f, 0f, 1.0f));
                    Tree(k, new Vector3(-0.9f, 0f, -0.9f));
                    Tree(k, new Vector3(1.2f, 0f, -1.0f));
                    Tree(k, new Vector3(-0.2f, 0f, 1.1f));
                    break;
                case "mall":
                    k.Box("Mall", new Vector3(0f, 1.1f, 0.2f), new Vector3(3.0f, 2.2f, 2.4f), new Color(0.85f, 0.86f, 0.89f));
                    k.Box("Letrero del cine", new Vector3(0f, 1.6f, -1.02f), new Vector3(1.8f, 0.5f, 0.05f), new Color(0.14f, 0.25f, 0.48f));
                    k.Box("Entrada", new Vector3(0f, 0.5f, -1.02f), new Vector3(1.0f, 1.0f, 0.05f), new Color(0.18f, 0.23f, 0.32f));
                    break;
                case "gimnasio":
                    k.Box("Gimnasio", new Vector3(0f, 0.9f, 0.2f), new Vector3(2.8f, 1.8f, 2.2f), new Color(0.79f, 0.81f, 0.84f));
                    k.Box("Vidrio", new Vector3(0f, 0.9f, -0.92f), new Vector3(2.2f, 0.9f, 0.05f), new Color(0.55f, 0.66f, 0.76f));
                    k.Box("Letrero", new Vector3(0f, 1.6f, -0.92f), new Vector3(1.4f, 0.3f, 0.05f), new Color(0.85f, 0.35f, 0.30f));
                    break;
                case "cafe":
                    k.Box("Café", new Vector3(0f, 0.8f, 0.4f), new Vector3(2.2f, 1.6f, 1.6f), new Color(0.89f, 0.79f, 0.66f));
                    k.Box("Toldo", new Vector3(0f, 1.25f, -0.55f), new Vector3(2.2f, 0.1f, 0.5f), new Color(0.23f, 0.48f, 0.36f));
                    k.Box("Mesa afuera", new Vector3(-0.5f, 0.35f, -1.0f), new Vector3(0.5f, 0.06f, 0.5f), new Color(0.40f, 0.28f, 0.18f));
                    k.Box("Mesa afuera", new Vector3(0.6f, 0.35f, -1.0f), new Vector3(0.5f, 0.06f, 0.5f), new Color(0.40f, 0.28f, 0.18f));
                    break;
                case "bar":
                    k.Box("Bar", new Vector3(0f, 0.85f, 0.3f), new Vector3(2.4f, 1.7f, 1.9f), new Color(0.36f, 0.27f, 0.30f));
                    k.Box("Letrero del bar", new Vector3(0f, 1.35f, -0.67f), new Vector3(1.2f, 0.3f, 0.05f), new Color(0.96f, 0.72f, 0.30f));
                    k.Box("Puerta del bar", new Vector3(0f, 0.45f, -0.67f), new Vector3(0.6f, 0.9f, 0.05f), new Color(0.16f, 0.12f, 0.12f));
                    break;
                case "colegio":
                    k.Box("Colegio", new Vector3(0f, 1.0f, 0.4f), new Vector3(3.0f, 2.0f, 1.6f), new Color(0.91f, 0.88f, 0.81f));
                    k.Box("Patio", new Vector3(0f, 0.05f, -0.8f), new Vector3(2.8f, 0.04f, 1.0f), new Color(0.80f, 0.74f, 0.62f));
                    k.Box("Mástil", new Vector3(1.2f, 0.9f, -0.9f), new Vector3(0.06f, 1.8f, 0.06f), new Color(0.75f, 0.75f, 0.75f));
                    break;
                case "oficina":
                    k.Box("Edificio", new Vector3(0f, 1.6f, 0.2f), new Vector3(2.4f, 3.2f, 2.2f), new Color(0.74f, 0.78f, 0.84f));
                    for (int floor = 0; floor < 4; floor++)
                    {
                        k.Box("Ventanas", new Vector3(0f, 0.55f + floor * 0.72f, -0.92f), new Vector3(2.0f, 0.35f, 0.04f), new Color(0.55f, 0.68f, 0.80f));
                    }

                    break;
                case "fonda":
                    // Ramadas con techo de paja y banderines (Fiestas Patrias, paquete de Chile).
                    for (int i = -1; i <= 1; i++)
                    {
                        k.Box("Ramada", new Vector3(i * 1.0f, 0.55f, 0.3f), new Vector3(0.85f, 1.1f, 0.85f), new Color(0.79f, 0.65f, 0.35f));
                        k.Box("Techo de paja", new Vector3(i * 1.0f, 1.15f, 0.3f), new Vector3(1.0f, 0.12f, 1.0f), new Color(0.85f, 0.75f, 0.48f));
                    }

                    Color[] flags = { new Color(0.84f, 0.17f, 0.12f), Color.white, new Color(0.14f, 0.25f, 0.48f) };
                    for (int i = 0; i < 7; i++)
                    {
                        k.Box("Banderín", new Vector3(-1.4f + i * 0.47f, 1.45f, -0.4f), new Vector3(0.2f, 0.2f, 0.02f), flags[i % 3]);
                    }

                    break;
                case "cancha_futbol5":
                    // Cancha de pasto sintético con su reja (paquete de Argentina).
                    k.Box("Cancha", new Vector3(0f, 0.05f, 0f), new Vector3(3.0f, 0.04f, 2.6f), new Color(0.30f, 0.60f, 0.34f));
                    k.Box("Línea del medio", new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.02f, 2.5f), Color.white);
                    k.Box("Arco", new Vector3(-1.4f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.9f), Color.white);
                    k.Box("Arco", new Vector3(1.4f, 0.35f, 0f), new Vector3(0.08f, 0.7f, 0.9f), Color.white);
                    k.Box("Reja", new Vector3(0f, 0.6f, 1.3f), new Vector3(3.0f, 1.2f, 0.03f), new Color(0.55f, 0.57f, 0.60f));
                    break;
                default:
                    k.Box(shape, new Vector3(0f, 0.8f, 0.2f), new Vector3(2.2f, 1.6f, 2.0f), new Color(0.85f, 0.83f, 0.78f));
                    break;
            }
        }

        private static void Tree(ShapeKit k, Vector3 at)
        {
            k.Box("Tronco", at + new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.6f, 0.12f), new Color(0.42f, 0.31f, 0.21f));
            k.Ball("Copa", at + new Vector3(0f, 0.85f, 0f), 0.65f, new Color(0.31f, 0.54f, 0.36f));
        }
    }
}
