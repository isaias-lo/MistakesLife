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

        /// <summary>
        /// Los objetos de la pieza, cada uno en su ancla (<see cref="RoomBuilder.DefaultAnchors"/>).
        /// Si existe Resources/Modelos/&lt;id&gt;.prefab (por ejemplo escena.casa.diploma.prefab),
        /// se usa ese modelo en vez de la forma simple.
        /// </summary>
        public static void BuildRoomObjects(ShapeKit kit, HomeView home, Anchors anchors)
        {
            foreach (string id in home.SceneObjects)
            {
                string? anchor = AnchorOf(id);
                if (anchor != null)
                {
                    Models.Place(kit, anchors[anchor], id, k => DrawSimple(k, id));
                }
            }
        }

        /// <summary>En qué ancla va cada objeto. Un id que esta pantalla no conoce se salta.</summary>
        private static string? AnchorOf(string id) => id switch
        {
            "escena.casa.libros" => "ancla.mesa",
            "escena.casa.notebook" => "ancla.mesa",
            "escena.casa.avisos_trabajo" => "ancla.mesa",
            "escena.casa.cuentas" => "ancla.mesa",
            "escena.casa.remedios" => "ancla.mesa",
            "escena.casa.contrato_arriendo" => "ancla.mesa",
            "escena.casa.bolsas_super" => "ancla.cocina",
            "escena.casa.cartas_rechazo" => "ancla.mesa",
            "escena.casa.velas" => "ancla.mesa",
            "escena.casa.celular" => "ancla.mesa",
            "escena.casa.celular_avisos" => "ancla.mesa",
            "escena.casa.ropa_entrevista" => "ancla.ropero",
            "escena.casa.mochila_emergencia" => "ancla.puerta",
            "escena.casa.grietas" => "ancla.muro_lateral",
            "escena.casa.bebidas" => "ancla.cocina",
            "escena.casa.mochila" => "ancla.mochila",
            "escena.casa.diploma" => "ancla.cuadro_izquierda",
            "escena.casa.foto_pareja" => "ancla.cuadro_derecha",
            "escena.casa.flores" => "ancla.ventana",
            "escena.casa.desorden" => "ancla.piso",
            "escena.casa.juguetes" => "ancla.piso",
            "escena.casa.foto_recuerdo" => "ancla.muro_lateral",
            "escena.casa.mecedora" => "ancla.mecedora",
            _ => null,
        };

        /// <summary>La forma simple de reserva de cada objeto, medida desde su ancla.</summary>
        private static void DrawSimple(ShapeKit kit, string id)
        {
            switch (id)
            {
                case "escena.casa.libros":
                    Books(kit, new Vector3(-0.45f, 0.44f, -0.15f));
                    break;
                case "escena.casa.mochila":
                    kit.Box("Mochila", new Vector3(0f, 0.22f, 0f), new Vector3(0.4f, 0.44f, 0.24f), new Color(0.25f, 0.45f, 0.75f));
                    kit.Box("Bolsillo de la mochila", new Vector3(0f, 0.16f, -0.13f), new Vector3(0.3f, 0.2f, 0.04f), new Color(0.20f, 0.36f, 0.60f));
                    break;
                case "escena.casa.diploma":
                    Picture(kit, "Diploma", Vector3.zero, new Vector2(0.7f, 0.5f), Palette.Paper);
                    kit.Ball("Sello del diploma", new Vector3(0.2f, -0.13f, -0.03f), 0.08f, new Color(0.85f, 0.65f, 0.20f));
                    break;
                case "escena.casa.notebook":
                    kit.Box("Notebook", new Vector3(0.3f, 0.455f, 0.15f), new Vector3(0.5f, 0.03f, 0.34f), new Color(0.25f, 0.26f, 0.30f));
                    GameObject screen = kit.Box("Pantalla del notebook", new Vector3(0.3f, 0.61f, 0.32f), new Vector3(0.5f, 0.32f, 0.03f), new Color(0.35f, 0.60f, 0.85f));
                    screen.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                    break;
                case "escena.casa.avisos_trabajo":
                    kit.Box("Diario con avisos", new Vector3(-0.3f, 0.45f, 0.25f), new Vector3(0.5f, 0.02f, 0.36f), new Color(0.82f, 0.82f, 0.80f));
                    kit.Shape(PrimitiveType.Cylinder, "Aviso marcado", new Vector3(-0.22f, 0.465f, 0.2f), new Vector3(0.14f, 0.005f, 0.14f), Palette.Alarm);
                    break;
                case "escena.casa.cuentas":
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 place = new Vector3(-0.1f + (i * 0.07f), 0.45f + (i * 0.015f), -0.32f + (i * 0.03f));
                        GameObject bill = kit.Box("Cuenta impaga", place, new Vector3(0.32f, 0.012f, 0.2f), i % 2 == 0 ? Palette.Alarm : Palette.Paper);
                        bill.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);
                    }

                    break;
                case "escena.casa.contrato_arriendo":
                    // El contrato que firmaste: una hoja sobre la mesa con tu firma en amarillo.
                    GameObject contract = kit.Box("Contrato de arriendo", new Vector3(-0.1f, 0.446f, -0.02f), new Vector3(0.22f, 0.006f, 0.3f), Palette.Paper);
                    contract.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
                    GameObject signature = kit.Box("Tu firma", new Vector3(-0.09f, 0.451f, -0.12f), new Vector3(0.12f, 0.004f, 0.02f), Palette.PlayerMark);
                    signature.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
                    break;
                case "escena.casa.cartas_rechazo":
                    // Las respuestas de las postulaciones que no resultaron: sobres abiertos con un timbre rojo.
                    for (int i = 0; i < 3; i++)
                    {
                        GameObject letter = kit.Box("Carta de rechazo", new Vector3(0.48f + (i * 0.02f), 0.446f + (i * 0.006f), -0.1f + (i * 0.015f)), new Vector3(0.2f, 0.005f, 0.12f), Palette.Paper);
                        letter.transform.localRotation = Quaternion.Euler(0f, -14f + (i * 13f), 0f);
                    }

                    kit.Box("Timbre rojo", new Vector3(0.53f, 0.464f, -0.07f), new Vector3(0.05f, 0.002f, 0.04f), Palette.Alarm);
                    break;
                case "escena.casa.celular":
                    // Tu celular sobre la mesa: el material oscuro del celular con su pantalla apagada.
                    GameObject phone = kit.Box("Celular", new Vector3(0f, 0.449f, 0.36f), new Vector3(0.09f, 0.012f, 0.17f), new Color(0.043f, 0.059f, 0.102f));
                    phone.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                    GameObject off = kit.Box("Pantalla del celular", new Vector3(0f, 0.4555f, 0.36f), new Vector3(0.078f, 0.002f, 0.15f), Palette.Screen);
                    off.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                    break;
                case "escena.casa.celular_avisos":
                    // Llegaron correos sin leer: la pantalla del celular se ilumina y aparece el punto rojo.
                    GameObject lit = kit.Box("Pantalla encendida", new Vector3(0f, 0.457f, 0.36f), new Vector3(0.078f, 0.002f, 0.15f), new Color(0.62f, 0.84f, 1f));
                    lit.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                    kit.Ball("Aviso del correo", new Vector3(0.02f, 0.466f, 0.42f), 0.03f, new Color(1f, 0.54f, 0.48f));
                    break;
                case "escena.casa.velas":
                    // Hubo temporal y se cortó la luz: dos velas encendidas en la mesa.
                    foreach (float z in new[] { 0.2f, 0.36f })
                    {
                        kit.Shape(PrimitiveType.Cylinder, "Vela", new Vector3(0.63f, 0.51f, z), new Vector3(0.05f, 0.07f, 0.05f), Palette.Paper);
                        kit.Ball("Llama", new Vector3(0.63f, 0.6f, z), 0.035f, new Color(1f, 0.72f, 0.25f));
                    }

                    break;
                case "escena.casa.ropa_entrevista":
                    // La ropa para la entrevista lista en un colgador: camisa, chaqueta y corbata.
                    kit.Shape(PrimitiveType.Cylinder, "Pie del colgador", new Vector3(-0.75f, 0.8f, -0.35f), new Vector3(0.04f, 0.8f, 0.04f), Palette.DarkWood);
                    kit.Shape(PrimitiveType.Cylinder, "Base del colgador", new Vector3(-0.75f, 0.02f, -0.35f), new Vector3(0.36f, 0.02f, 0.36f), Palette.DarkWood);
                    kit.Box("Chaqueta", new Vector3(-0.75f, 1.25f, -0.45f), new Vector3(0.46f, 0.6f, 0.06f), new Color(0.22f, 0.26f, 0.36f));
                    kit.Box("Camisa", new Vector3(-0.75f, 1.32f, -0.485f), new Vector3(0.14f, 0.44f, 0.02f), Color.white);
                    kit.Box("Corbata", new Vector3(-0.75f, 1.3f, -0.5f), new Vector3(0.05f, 0.34f, 0.01f), Palette.PlayerMark);
                    break;
                case "escena.casa.mochila_emergencia":
                    // La mochila de emergencia que armaste, lista junto a la puerta.
                    kit.Box("Mochila de emergencia", new Vector3(0.35f, 0.22f, 0.65f), new Vector3(0.3f, 0.44f, 0.4f), new Color(0.95f, 0.50f, 0.15f));
                    kit.Box("Franja reflectante", new Vector3(0.505f, 0.28f, 0.65f), new Vector3(0.01f, 0.05f, 0.4f), new Color(0.85f, 0.88f, 0.90f));
                    kit.Shape(PrimitiveType.Cylinder, "Linterna", new Vector3(0.35f, 0.47f, 0.55f), new Vector3(0.06f, 0.05f, 0.06f), new Color(0.20f, 0.20f, 0.22f));
                    break;
                case "escena.casa.grietas":
                    // No reparaste la casa después del terremoto: grietas en zigzag en el muro.
                    Vector3[] crack =
                    {
                        new Vector3(0.06f, 2.3f, 0.8f), new Vector3(0.06f, 2.0f, 0.95f), new Vector3(0.06f, 1.7f, 0.82f),
                        new Vector3(0.06f, 1.4f, 1.0f), new Vector3(0.06f, 1.1f, 0.9f),
                    };
                    for (int i = 0; i < crack.Length - 1; i++)
                    {
                        Vector3 from = crack[i];
                        Vector3 to = crack[i + 1];
                        GameObject piece = kit.Box("Grieta", (from + to) / 2f, new Vector3(0.01f, Vector3.Distance(from, to), 0.025f), new Color(0.18f, 0.16f, 0.15f));
                        piece.transform.localRotation = Quaternion.Euler(Mathf.Atan2(to.z - from.z, to.y - from.y) * Mathf.Rad2Deg, 0f, 0f);
                    }

                    break;
                case "escena.casa.bolsas_super":
                    // Tu compra: dos bolsas de papel en el suelo, al lado de la cocina, con lo que compraste asomando.
                    for (int i = 0; i < 2; i++)
                    {
                        Vector3 bag = new Vector3(0.62f + (i * 0.34f), 0f, -0.12f - (i * 0.06f));
                        GameObject paper = kit.Box("Bolsa del supermercado", bag + new Vector3(0f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 0.2f), new Color(0.79f, 0.64f, 0.45f));
                        paper.transform.localRotation = Quaternion.Euler(0f, -10f + (i * 22f), 0f);
                    }

                    kit.Ball("Lechuga", new Vector3(0.58f, 0.42f, -0.12f), 0.14f, new Color(0.44f, 0.70f, 0.42f));
                    kit.Ball("Tomate", new Vector3(0.68f, 0.41f, -0.1f), 0.1f, new Color(0.91f, 0.42f, 0.36f));
                    kit.Box("Pan", new Vector3(1.0f, 0.43f, -0.18f), new Vector3(0.16f, 0.08f, 0.1f), new Color(0.90f, 0.72f, 0.45f));
                    break;
                case "escena.casa.bebidas":
                    // Compras con antojos: bebidas de dos litros junto a las bolsas.
                    for (int i = 0; i < 2; i++)
                    {
                        Vector3 bottle = new Vector3(1.28f + (i * 0.13f), 0.16f, -0.1f);
                        kit.Shape(PrimitiveType.Cylinder, "Bebida", bottle, new Vector3(0.11f, 0.16f, 0.11f), new Color(0.35f, 0.18f, 0.12f));
                        kit.Shape(PrimitiveType.Cylinder, "Etiqueta de la bebida", bottle, new Vector3(0.115f, 0.04f, 0.115f), Palette.Alarm);
                        kit.Shape(PrimitiveType.Cylinder, "Tapa de la bebida", bottle + new Vector3(0f, 0.18f, 0f), new Vector3(0.04f, 0.02f, 0.04f), Color.white);
                    }

                    break;
                case "escena.casa.remedios":
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 bottle = new Vector3(0.45f + (i * 0.12f), 0.52f, -0.28f);
                        kit.Shape(PrimitiveType.Cylinder, "Remedio", bottle, new Vector3(0.08f, 0.08f, 0.08f), Color.white);
                        kit.Shape(PrimitiveType.Cylinder, "Tapa del remedio", bottle + new Vector3(0f, 0.09f, 0f), new Vector3(0.085f, 0.015f, 0.085f), Palette.Alarm);
                    }

                    break;
                case "escena.casa.flores":
                    kit.Shape(PrimitiveType.Cylinder, "Florero", new Vector3(0f, 1.27f, -0.18f), new Vector3(0.16f, 0.12f, 0.16f), new Color(0.45f, 0.65f, 0.80f));
                    kit.Ball("Flor", new Vector3(-0.07f, 1.47f, -0.18f), 0.13f, new Color(0.95f, 0.45f, 0.55f));
                    kit.Ball("Flor", new Vector3(0.08f, 1.5f, -0.2f), 0.12f, new Color(0.98f, 0.85f, 0.30f));
                    kit.Ball("Flor", new Vector3(0f, 1.56f, -0.16f), 0.12f, new Color(0.90f, 0.55f, 0.85f));
                    break;
                case "escena.casa.desorden":
                    Clothes(kit, new Vector3(0.3f, 0.02f, -0.2f), 20f, new Color(0.55f, 0.30f, 0.30f));
                    Clothes(kit, new Vector3(-0.9f, 0.02f, 0.1f), -35f, new Color(0.30f, 0.35f, 0.55f));
                    Clothes(kit, new Vector3(1.2f, 0.03f, 1.2f), 60f, new Color(0.45f, 0.45f, 0.42f));
                    break;
                case "escena.casa.juguetes":
                    kit.Ball("Pelota", new Vector3(1.0f, 0.12f, -0.1f), 0.24f, new Color(0.95f, 0.55f, 0.20f));
                    kit.Box("Cubo de juguete", new Vector3(1.45f, 0.08f, -0.3f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.30f, 0.70f, 0.40f));
                    kit.Box("Cubo de juguete", new Vector3(1.6f, 0.08f, -0.15f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.35f, 0.50f, 0.90f));
                    kit.Box("Cubo de juguete", new Vector3(1.52f, 0.24f, -0.22f), new Vector3(0.16f, 0.16f, 0.16f), new Color(0.95f, 0.80f, 0.25f));
                    break;
                case "escena.casa.foto_pareja":
                    Picture(kit, "Foto con la pareja", Vector3.zero, new Vector2(0.5f, 0.4f), new Color(0.95f, 0.75f, 0.78f));
                    break;
                case "escena.casa.foto_recuerdo":
                    // En el muro lateral, con una cinta negra en la esquina: alguien que ya no está.
                    kit.Box("Foto del recuerdo", new Vector3(0.03f, 1.7f, 0f), new Vector3(0.04f, 0.5f, 0.4f), Palette.Frame);
                    kit.Box("Retrato", new Vector3(0.05f, 1.7f, 0f), new Vector3(0.02f, 0.4f, 0.3f), new Color(0.75f, 0.72f, 0.68f));
                    GameObject ribbon = kit.Box("Cinta negra", new Vector3(0.06f, 1.88f, 0.15f), new Vector3(0.02f, 0.05f, 0.22f), Color.black);
                    ribbon.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                    kit.Shape(PrimitiveType.Cylinder, "Vela", new Vector3(0.25f, 0.15f, 0f), new Vector3(0.08f, 0.15f, 0.08f), Palette.Paper);
                    break;
                case "escena.casa.mecedora":
                    RockingChair(kit, Vector3.zero);
                    break;
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
        private static void Picture(ShapeKit kit, string name, Vector3 center, Vector2 size, Color inside) =>
            Picture(kit, name, center, size, kit.Box(name + " (lámina)", center + new Vector3(0f, 0f, -0.025f), new Vector3(size.x * 0.84f, size.y * 0.8f, 0.02f), inside));

        private static void Picture(ShapeKit kit, string name, Vector3 center, Vector2 size, Material inside) =>
            Picture(kit, name, center, size, kit.Box(name + " (lámina)", center + new Vector3(0f, 0f, -0.025f), new Vector3(size.x * 0.84f, size.y * 0.8f, 0.02f), inside));

        private static void Picture(ShapeKit kit, string name, Vector3 center, Vector2 size, GameObject sheet)
        {
            kit.Box(name, center, new Vector3(size.x, size.y, 0.04f), Palette.Frame);
            sheet.name = name + " (lámina)";
        }

        private static void Clothes(ShapeKit kit, Vector3 floor, float turn, Color color)
        {
            GameObject piece = kit.Box("Ropa tirada", floor, new Vector3(0.5f, 0.04f, 0.32f), color);
            piece.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
        }

        private static void RockingChair(ShapeKit kit, Vector3 floor)
        {
            Material wood = Palette.Wood;
            kit.Box("Asiento de la mecedora", floor + new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.08f, 0.6f), wood);
            GameObject back = kit.Box("Respaldo de la mecedora", floor + new Vector3(0f, 0.85f, 0.3f), new Vector3(0.6f, 0.75f, 0.06f), wood);
            back.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            kit.Box("Pata de la mecedora", floor + new Vector3(-0.25f, 0.22f, 0f), new Vector3(0.05f, 0.44f, 0.05f), wood);
            kit.Box("Pata de la mecedora", floor + new Vector3(0.25f, 0.22f, 0f), new Vector3(0.05f, 0.44f, 0.05f), wood);
            kit.Box("Balancín", floor + new Vector3(-0.25f, 0.03f, 0f), new Vector3(0.05f, 0.05f, 0.8f), Palette.DarkWood);
            kit.Box("Balancín", floor + new Vector3(0.25f, 0.03f, 0f), new Vector3(0.05f, 0.05f, 0.8f), Palette.DarkWood);
        }
    }
}
