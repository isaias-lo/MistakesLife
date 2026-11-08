#nullable enable

using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Dibuja los objetos que dependen de la vida del jugador (<see cref="HomeView.SceneObjects"/>):
    /// libros si estudia, el diploma si se tituló, cuentas rojas si está moroso, remedios si
    /// está enfermo... Cuándo aparece cada uno lo decide su condición en el paquete
    /// (packs/base/scene/); aquí solo se sabe cómo se ve y dónde va. Un id que esta pantalla
    /// no conoce se salta, así un paquete nuevo nunca rompe el juego.
    /// </summary>
    public static class SceneObjectBuilder
    {
        private static readonly Color Paper = new Color(0.95f, 0.94f, 0.90f);
        private static readonly Color Frame = new Color(0.30f, 0.22f, 0.16f);
        private static readonly Color Alarm = new Color(0.85f, 0.22f, 0.20f);

        /// <summary>Los objetos de la pieza. La mesa del comedor está en (1.6, 0.44, 0.9) y el muro del fondo en z = 2.5.</summary>
        public static void BuildRoomObjects(ShapeKit kit, HomeView home)
        {
            float leftWall = -(6f + (home.HouseholdLevel / 25f)) / 2f;
            float window = (6f + (home.HouseholdLevel / 25f)) / 4f;
            foreach (string id in home.SceneObjects)
            {
                switch (id)
                {
                    case "escena.casa.libros":
                        Books(kit, new Vector3(1.15f, 0.44f, 0.75f));
                        break;
                    case "escena.casa.mochila":
                        kit.Box("Mochila", new Vector3(-1.45f, 0.22f, 0.3f), new Vector3(0.4f, 0.44f, 0.24f), new Color(0.25f, 0.45f, 0.75f));
                        kit.Box("Bolsillo de la mochila", new Vector3(-1.45f, 0.16f, 0.17f), new Vector3(0.3f, 0.2f, 0.04f), new Color(0.20f, 0.36f, 0.60f));
                        break;
                    case "escena.casa.diploma":
                        Picture(kit, "Diploma", new Vector3(-1.0f, 1.85f, 2.44f), new Vector2(0.7f, 0.5f), Paper);
                        kit.Ball("Sello del diploma", new Vector3(-0.8f, 1.72f, 2.41f), 0.08f, new Color(0.85f, 0.65f, 0.20f));
                        break;
                    case "escena.casa.notebook":
                        kit.Box("Notebook", new Vector3(1.9f, 0.455f, 1.05f), new Vector3(0.5f, 0.03f, 0.34f), new Color(0.25f, 0.26f, 0.30f));
                        GameObject screen = kit.Box("Pantalla del notebook", new Vector3(1.9f, 0.61f, 1.22f), new Vector3(0.5f, 0.32f, 0.03f), new Color(0.35f, 0.60f, 0.85f));
                        screen.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                        break;
                    case "escena.casa.avisos_trabajo":
                        kit.Box("Diario con avisos", new Vector3(1.3f, 0.45f, 1.15f), new Vector3(0.5f, 0.02f, 0.36f), new Color(0.82f, 0.82f, 0.80f));
                        kit.Shape(PrimitiveType.Cylinder, "Aviso marcado", new Vector3(1.38f, 0.465f, 1.1f), new Vector3(0.14f, 0.005f, 0.14f), Alarm);
                        break;
                    case "escena.casa.cuentas":
                        for (int i = 0; i < 4; i++)
                        {
                            GameObject bill = kit.Box("Cuenta impaga", new Vector3(1.5f + (i * 0.07f), 0.45f + (i * 0.015f), 0.58f + (i * 0.03f)), new Vector3(0.32f, 0.012f, 0.2f), i % 2 == 0 ? Alarm : Paper);
                            bill.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
                        }

                        break;
                    case "escena.casa.remedios":
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 bottle = new Vector3(2.05f + (i * 0.12f), 0.52f, 0.62f);
                            kit.Shape(PrimitiveType.Cylinder, "Remedio", bottle, new Vector3(0.08f, 0.08f, 0.08f), Color.white);
                            kit.Shape(PrimitiveType.Cylinder, "Tapa del remedio", bottle + new Vector3(0f, 0.09f, 0f), new Vector3(0.085f, 0.015f, 0.085f), Alarm);
                        }

                        break;
                    case "escena.casa.flores":
                        kit.Shape(PrimitiveType.Cylinder, "Florero", new Vector3(window, 1.27f, 2.32f), new Vector3(0.16f, 0.12f, 0.16f), new Color(0.45f, 0.65f, 0.80f));
                        kit.Ball("Flor", new Vector3(window - 0.07f, 1.47f, 2.32f), 0.13f, new Color(0.95f, 0.45f, 0.55f));
                        kit.Ball("Flor", new Vector3(window + 0.08f, 1.5f, 2.3f), 0.12f, new Color(0.98f, 0.85f, 0.30f));
                        kit.Ball("Flor", new Vector3(window, 1.56f, 2.34f), 0.12f, new Color(0.90f, 0.55f, 0.85f));
                        break;
                    case "escena.casa.desorden":
                        Clothes(kit, new Vector3(0.3f, 0.02f, -1.8f), 20f, new Color(0.55f, 0.30f, 0.30f));
                        Clothes(kit, new Vector3(-0.9f, 0.02f, -1.5f), -35f, new Color(0.30f, 0.35f, 0.55f));
                        Clothes(kit, new Vector3(1.2f, 0.03f, -0.4f), 60f, new Color(0.45f, 0.45f, 0.42f));
                        break;
                    case "escena.casa.juguetes":
                        kit.Ball("Pelota", new Vector3(1.0f, 0.12f, -1.7f), 0.24f, new Color(0.95f, 0.55f, 0.20f));
                        kit.Box("Cubo de juguete", new Vector3(1.45f, 0.08f, -1.9f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.30f, 0.70f, 0.40f));
                        kit.Box("Cubo de juguete", new Vector3(1.6f, 0.08f, -1.75f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.35f, 0.50f, 0.90f));
                        kit.Box("Cubo de juguete", new Vector3(1.52f, 0.24f, -1.82f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.95f, 0.80f, 0.25f));
                        break;
                    case "escena.casa.foto_pareja":
                        Picture(kit, "Foto con la pareja", new Vector3(0.4f, 1.95f, 2.44f), new Vector2(0.5f, 0.4f), new Color(0.95f, 0.75f, 0.78f));
                        break;
                    case "escena.casa.foto_recuerdo":
                        // En el muro lateral, con una cinta negra en la esquina: alguien que ya no está.
                        kit.Box("Foto del recuerdo", new Vector3(leftWall + 0.03f, 1.7f, -0.2f), new Vector3(0.04f, 0.5f, 0.4f), Frame);
                        kit.Box("Retrato", new Vector3(leftWall + 0.05f, 1.7f, -0.2f), new Vector3(0.02f, 0.4f, 0.3f), new Color(0.75f, 0.72f, 0.68f));
                        GameObject ribbon = kit.Box("Cinta negra", new Vector3(leftWall + 0.06f, 1.88f, -0.05f), new Vector3(0.02f, 0.05f, 0.22f), Color.black);
                        ribbon.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                        kit.Shape(PrimitiveType.Cylinder, "Vela", new Vector3(leftWall + 0.25f, 0.15f, -0.2f), new Vector3(0.08f, 0.15f, 0.08f), Paper);
                        break;
                    case "escena.casa.mecedora":
                        RockingChair(kit, new Vector3(2.6f, 0f, -1.7f));
                        break;
                }
            }
        }

        private static void Books(ShapeKit kit, Vector3 table)
        {
            Color[] covers = { new Color(0.70f, 0.25f, 0.25f), new Color(0.25f, 0.45f, 0.70f), new Color(0.30f, 0.60f, 0.35f) };
            for (int i = 0; i < covers.Length; i++)
            {
                GameObject book = kit.Box("Libro", table + new Vector3(0f, 0.03f + (i * 0.06f), 0f), new Vector3(0.36f, 0.055f, 0.26f), covers[i]);
                book.transform.localRotation = Quaternion.Euler(0f, i * 12f, 0f);
            }
        }

        /// <summary>Un cuadro colgado en el muro del fondo: marco oscuro y lámina del color pedido.</summary>
        private static void Picture(ShapeKit kit, string name, Vector3 center, Vector2 size, Color inside)
        {
            kit.Box(name, center, new Vector3(size.x, size.y, 0.04f), Frame);
            kit.Box(name + " (lámina)", center + new Vector3(0f, 0f, -0.025f), new Vector3(size.x * 0.84f, size.y * 0.8f, 0.02f), inside);
        }

        private static void Clothes(ShapeKit kit, Vector3 floor, float turn, Color color)
        {
            GameObject piece = kit.Box("Ropa tirada", floor, new Vector3(0.5f, 0.04f, 0.32f), color);
            piece.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
        }

        private static void RockingChair(ShapeKit kit, Vector3 floor)
        {
            Color wood = new Color(0.55f, 0.38f, 0.24f);
            kit.Box("Asiento de la mecedora", floor + new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.08f, 0.6f), wood);
            GameObject back = kit.Box("Respaldo de la mecedora", floor + new Vector3(0f, 0.85f, 0.3f), new Vector3(0.6f, 0.75f, 0.06f), wood);
            back.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            kit.Box("Pata de la mecedora", floor + new Vector3(-0.25f, 0.22f, 0f), new Vector3(0.05f, 0.44f, 0.05f), wood);
            kit.Box("Pata de la mecedora", floor + new Vector3(0.25f, 0.22f, 0f), new Vector3(0.05f, 0.44f, 0.05f), wood);
            kit.Box("Balancín", floor + new Vector3(-0.25f, 0.03f, 0f), new Vector3(0.05f, 0.05f, 0.8f), wood * 0.85f);
            kit.Box("Balancín", floor + new Vector3(0.25f, 0.03f, 0f), new Vector3(0.05f, 0.05f, 0.8f), wood * 0.85f);
        }
    }
}
