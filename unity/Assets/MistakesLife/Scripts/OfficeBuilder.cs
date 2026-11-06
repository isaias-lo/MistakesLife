#nullable enable

using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma el lugar de trabajo del jugador estilo maqueta. El tipo de lugar lo dice el
    /// paquete de la carrera (<see cref="WorkplaceView.Kind"/>); aquí solo se dibuja:
    /// oficina abierta, oficina con cubículos, consulta, oficina común o, sin título,
    /// un local de atención. Un título que paga más se nota en un lugar más cuidado.
    /// </summary>
    public static class OfficeBuilder
    {
        private const float Width = 8f;
        private const float Depth = 6f;

        private static readonly Color DarkScreen = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color Desk = new Color(0.82f, 0.78f, 0.70f);
        private static readonly Color Chair = new Color(0.22f, 0.24f, 0.28f);
        private static readonly Color Paper = new Color(0.97f, 0.97f, 0.95f);

        public static void Build(ShapeKit kit, WorkplaceView work)
        {
            bool nice = work.SalaryRelative >= 2.5;
            Shell(kit, nice);

            switch (work.Kind)
            {
                case "lugar_trabajo.oficina_abierta":
                    OpenOffice(kit, work);
                    break;
                case "lugar_trabajo.oficina_cubiculos":
                    Cubicles(kit, work);
                    break;
                case "lugar_trabajo.consulta":
                    Practice(kit, work);
                    break;
                default:
                    if (work.CareerId == null)
                    {
                        Counter(kit, work);
                    }
                    else
                    {
                        PlainOffice(kit, work);
                    }

                    break;
            }

            if (nice)
            {
                Plant(kit, new Vector3(3.5f, 0f, 1.5f));
            }

            Sign(kit, work.CompanySizeId);
        }

        /// <summary>
        /// El letrero con el nombre de la empresa en el muro del fondo: chico y simple en una
        /// mediana, más grande y con una franja de color en una grande. Las chicas no tienen.
        /// </summary>
        private static void Sign(ShapeKit kit, string? sizeId)
        {
            bool big = sizeId == "empresa.tamano.grande";
            if (!big && sizeId != "empresa.tamano.mediana")
            {
                return;
            }

            float width = big ? 1.8f : 1.1f;
            Vector3 at = new Vector3(-2.3f, 2.35f, (Depth / 2f) - 0.02f);
            kit.Box("Letrero", at, new Vector3(width, 0.38f, 0.04f), new Color(0.20f, 0.22f, 0.26f));
            if (big)
            {
                kit.Box("Franja del letrero", at + new Vector3(0f, -0.12f, -0.01f), new Vector3(width, 0.08f, 0.04f), UiFactory.Accent);
            }
        }

        /// <summary>Piso, dos muros y una ventana; más luz y madera si el título paga bien.</summary>
        private static void Shell(ShapeKit kit, bool nice)
        {
            Color floor = nice ? new Color(0.66f, 0.52f, 0.38f) : new Color(0.55f, 0.57f, 0.60f);
            Color wall = nice ? new Color(0.93f, 0.92f, 0.88f) : new Color(0.84f, 0.84f, 0.80f);
            kit.Box("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(Width, 0.1f, Depth), floor);
            kit.Box("Muro del fondo", new Vector3(0f, 1.4f, (Depth / 2f) + 0.05f), new Vector3(Width, 2.8f, 0.1f), wall);
            kit.Box("Muro lateral", new Vector3((-Width / 2f) - 0.05f, 1.4f, 0f), new Vector3(0.1f, 2.8f, Depth + 0.2f), wall * 0.92f);
            float window = nice ? 3.2f : 1.6f;
            kit.Box("Ventana", new Vector3(1.4f, 1.7f, (Depth / 2f) - 0.01f), new Vector3(window, 1.0f, 0.04f), new Color(0.62f, 0.80f, 0.92f));
        }

        /// <summary>Ingeniería de software y parecidas: mesas largas compartidas, pantallas y una pizarra.</summary>
        private static void OpenOffice(ShapeKit kit, WorkplaceView work)
        {
            for (int row = 0; row < 2; row++)
            {
                float z = row == 0 ? 1.2f : -0.8f;
                kit.Box("Mesa compartida", new Vector3(0.2f, 0.72f, z), new Vector3(5.2f, 0.06f, 1.0f), Desk);
                for (int seat = 0; seat < 3; seat++)
                {
                    float x = -1.5f + (seat * 1.7f);
                    kit.Box("Pantalla", new Vector3(x, 1.05f, z + 0.3f), new Vector3(0.7f, 0.42f, 0.04f), DarkScreen);
                    kit.Box("Silla", new Vector3(x, 0.45f, z - 0.75f), new Vector3(0.5f, 0.1f, 0.5f), Chair);
                }
            }

            kit.Box("Pizarra", new Vector3((-Width / 2f) + 0.02f, 1.5f, 0.3f), new Vector3(0.04f, 1.1f, 2.0f), Color.white);
            kit.Ball("Puf", new Vector3(3.2f, 0.3f, -1.3f), 0.7f, new Color(0.90f, 0.55f, 0.30f));

            Player(kit, work, new Vector3(-1.5f, 0f, 0.0f));
            SecondScreen(kit, new Vector3(-1.5f, 1.05f, 1.5f), work.Rank);
            if (work.Rank >= 3)
            {
                // Rincón propio en la punta de la mesa: vidrio entre las dos filas y una planta junto al muro.
                Glass(kit, new Vector3(-2.6f, 0.9f, 0.2f), new Vector3(0.05f, 1.8f, 0.9f));
                Plant(kit, new Vector3(-3.5f, 0f, 0.2f));
            }
            Colleague(kit, work, new Vector3(0.2f, 0f, 0.0f), 1);
            Colleague(kit, work, new Vector3(1.9f, 0f, -2.0f), 2);
            Colleague(kit, work, new Vector3(-1.5f, 0f, -2.0f), 3);
        }

        /// <summary>Contabilidad y parecidas: cubículos con paneles, papeles y archivadores.</summary>
        private static void Cubicles(ShapeKit kit, WorkplaceView work)
        {
            Color panel = new Color(0.55f, 0.62f, 0.70f);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -1.8f : 0.8f;
                float z = i < 2 ? 1.0f : -1.2f;
                kit.Box("Panel", new Vector3(x, 0.65f, z + 0.85f), new Vector3(2.2f, 1.3f, 0.06f), panel);
                kit.Box("Panel", new Vector3(x - 1.1f, 0.65f, z + 0.2f), new Vector3(0.06f, 1.3f, 1.3f), panel);
                kit.Box("Escritorio", new Vector3(x, 0.72f, z + 0.5f), new Vector3(2.0f, 0.06f, 0.6f), Desk);
                kit.Box("Pantalla", new Vector3(x + 0.4f, 1.0f, z + 0.65f), new Vector3(0.6f, 0.38f, 0.04f), DarkScreen);
                kit.Box("Papeles", new Vector3(x - 0.5f, 0.8f, z + 0.45f), new Vector3(0.4f, 0.1f, 0.3f), Paper);
            }

            for (int i = 0; i < 3; i++)
            {
                kit.Box("Archivador", new Vector3(2.6f + (i * 0.55f) - 0.55f, 0.6f, 2.6f), new Vector3(0.5f, 1.2f, 0.6f), new Color(0.50f, 0.52f, 0.55f));
            }

            Player(kit, work, new Vector3(-1.8f, 0f, 0.6f));
            SecondScreen(kit, new Vector3(-1.4f, 1.0f, 1.65f), work.Rank);
            if (work.Rank >= 3)
            {
                // Cubículo de esquina: los paneles del jugador suben con vidrio y la planta queda afuera, junto al muro.
                Glass(kit, new Vector3(-1.8f, 1.6f, 1.85f), new Vector3(2.2f, 0.6f, 0.05f));
                Glass(kit, new Vector3(-2.9f, 1.6f, 1.2f), new Vector3(0.05f, 0.6f, 1.3f));
                Plant(kit, new Vector3(-3.5f, 0f, -0.3f));
            }
            Colleague(kit, work, new Vector3(0.8f, 0f, 0.6f), 1);
            Colleague(kit, work, new Vector3(0.8f, 0f, -1.6f), 2);
        }

        /// <summary>Psicología y parecidas: dos sillones frente a frente, diván, libros y el título en la pared.</summary>
        private static void Practice(ShapeKit kit, WorkplaceView work)
        {
            Color armchair = new Color(0.45f, 0.35f, 0.50f);
            kit.Box("Alfombra", new Vector3(0f, 0.01f, 0f), new Vector3(3.6f, 0.02f, 2.6f), new Color(0.78f, 0.62f, 0.48f));
            kit.Box("Sillon del terapeuta", new Vector3(-1.4f, 0.35f, 0.2f), new Vector3(0.9f, 0.7f, 0.9f), armchair);
            kit.Box("Sillon del paciente", new Vector3(1.4f, 0.35f, 0.2f), new Vector3(0.9f, 0.7f, 0.9f), armchair * 1.1f);
            kit.Box("Mesa de centro", new Vector3(0f, 0.25f, 0.2f), new Vector3(0.8f, 0.5f, 0.5f), new Color(0.55f, 0.40f, 0.28f));
            kit.Box("Pañuelos", new Vector3(0f, 0.55f, 0.2f), new Vector3(0.25f, 0.12f, 0.15f), Paper);
            kit.Box("Divan", new Vector3(-2.6f, 0.3f, 2.0f), new Vector3(1.8f, 0.6f, 0.8f), new Color(0.35f, 0.45f, 0.40f));
            kit.Box("Libreria", new Vector3(2.4f, 1.0f, 2.7f), new Vector3(1.8f, 2.0f, 0.4f), new Color(0.50f, 0.36f, 0.24f));
            for (int i = 0; i < 4; i++)
            {
                Color book = i % 2 == 0 ? new Color(0.70f, 0.30f, 0.28f) : new Color(0.28f, 0.42f, 0.62f);
                kit.Box("Libros", new Vector3(1.8f + (i * 0.4f), 0.5f + ((i % 2) * 0.7f), 2.45f), new Vector3(0.32f, 0.35f, 0.1f), book);
            }

            kit.Box("Titulo enmarcado", new Vector3(-0.6f, 1.8f, (Depth / 2f) - 0.01f), new Vector3(0.6f, 0.45f, 0.04f), new Color(0.95f, 0.90f, 0.75f));

            Player(kit, work, new Vector3(-1.4f, 0f, -0.5f));
            Colleague(kit, work, new Vector3(1.4f, 0f, -0.5f), 1);
        }

        /// <summary>Con título pero sin tipo de lugar en su paquete: un escritorio y un archivador.</summary>
        private static void PlainOffice(ShapeKit kit, WorkplaceView work)
        {
            kit.Box("Escritorio", new Vector3(-0.6f, 0.72f, 1.2f), new Vector3(2.0f, 0.06f, 0.9f), Desk);
            kit.Box("Pantalla", new Vector3(-0.6f, 1.05f, 1.5f), new Vector3(0.7f, 0.42f, 0.04f), DarkScreen);
            kit.Box("Silla", new Vector3(-0.6f, 0.45f, 0.4f), new Vector3(0.5f, 0.1f, 0.5f), Chair);
            kit.Box("Archivador", new Vector3(2.4f, 0.6f, 2.6f), new Vector3(0.5f, 1.2f, 0.6f), new Color(0.50f, 0.52f, 0.55f));
            Player(kit, work, new Vector3(-0.6f, 0f, -0.3f));
            SecondScreen(kit, new Vector3(-0.6f, 1.05f, 1.5f), work.Rank);
            if (work.Rank >= 3)
            {
                // Oficina propia: vidrio a los dos lados del escritorio, sin tocarlo, y una planta en la esquina.
                Glass(kit, new Vector3(-2.1f, 0.6f, 0.4f), new Vector3(0.05f, 1.2f, 1.8f));
                Glass(kit, new Vector3(1.1f, 0.6f, 0.4f), new Vector3(0.05f, 1.2f, 1.8f));
                Plant(kit, new Vector3(-2.7f, 0f, 1.8f));
            }
            Colleague(kit, work, new Vector3(2.4f, 0f, -1.4f), 1);
        }

        /// <summary>Sin título: un local de atención con mostrador, estantes y caja.</summary>
        private static void Counter(ShapeKit kit, WorkplaceView work)
        {
            kit.Box("Mostrador", new Vector3(0f, 0.5f, 0.6f), new Vector3(4.0f, 1.0f, 0.8f), new Color(0.75f, 0.30f, 0.25f));
            kit.Box("Caja", new Vector3(1.2f, 1.12f, 0.6f), new Vector3(0.5f, 0.25f, 0.4f), DarkScreen);
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 0.5f + (shelf * 0.7f);
                kit.Box("Estante", new Vector3(-0.4f, y, 2.75f), new Vector3(5.0f, 0.06f, 0.4f), new Color(0.60f, 0.46f, 0.32f));
                for (int item = 0; item < 6; item++)
                {
                    // Sin amarillo: ese color es solo de la marca del jugador.
                    Color product = item % 3 == 0 ? new Color(0.85f, 0.35f, 0.45f) : item % 3 == 1 ? new Color(0.35f, 0.60f, 0.85f) : new Color(0.55f, 0.75f, 0.40f);
                    kit.Box("Producto", new Vector3(-2.4f + (item * 0.8f), y + 0.18f, 2.75f), new Vector3(0.35f, 0.3f, 0.3f), product);
                }
            }

            Player(kit, work, new Vector3(0f, 0f, 1.6f));
            Colleague(kit, work, new Vector3(0.4f, 0f, -0.8f), 1);
        }

        /// <summary>
        /// Lo que se gana subiendo de cargo (el rango lo calcula el núcleo): desde el segundo
        /// ascenso, una segunda pantalla. Desde el tercero, cada tipo de lugar arma su propio
        /// rincón (vidrio y planta) donde no choque con los muebles.
        /// </summary>
        private static void SecondScreen(ShapeKit kit, Vector3 screen, int rank)
        {
            if (rank >= 2)
            {
                kit.Box("Segunda pantalla", screen + new Vector3(0.75f, 0f, 0f), new Vector3(0.7f, 0.42f, 0.04f), DarkScreen);
            }
        }

        /// <summary>Un panel de vidrio con marco oscuro arriba, para que se distinga del fondo claro.</summary>
        private static void Glass(ShapeKit kit, Vector3 center, Vector3 size)
        {
            kit.Box("Vidrio", center, size, new Color(0.58f, 0.78f, 0.90f));
            Vector3 top = center + new Vector3(0f, size.y / 2f, 0f);
            Vector3 frame = new Vector3(Mathf.Max(size.x, 0.08f), 0.06f, Mathf.Max(size.z, 0.08f));
            kit.Box("Marco", top, frame, new Color(0.30f, 0.32f, 0.36f));
        }

        /// <summary>
        /// El jugador. En el trabajo solo lleva la bolita sobre la cabeza: el círculo del piso
        /// queda bajo los muebles y se asomaba lejos de él, entre paneles.
        /// </summary>
        private static void Player(ShapeKit kit, WorkplaceView work, Vector3 feet)
        {
            RoomBuilder.Person(kit, work.Player, feet, 0, markerSize: 0f);
        }

        /// <summary>
        /// Un colega, si la empresa tiene gente para él: en una microempresa trabaja uno
        /// más, en una pequeña dos; en una mediana, grande o sin empresa, todos los del lugar.
        /// </summary>
        private static void Colleague(ShapeKit kit, WorkplaceView work, Vector3 feet, int index)
        {
            int limit = work.CompanySizeId == "empresa.tamano.micro" ? 1
                : work.CompanySizeId == "empresa.tamano.pequena" ? 2
                : int.MaxValue;
            if (index > limit)
            {
                return;
            }

            var person = new HomeMemberView(-index, string.Empty, string.Empty, 35, LifeStage.Adult, index % 2 == 0, false);
            RoomBuilder.Person(kit, person, feet, index);
        }

        private static void Plant(ShapeKit kit, Vector3 at)
        {
            kit.Box("Macetero", at + new Vector3(0f, 0.25f, 0f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.60f, 0.38f, 0.26f));
            kit.Ball("Planta", at + new Vector3(0f, 0.8f, 0f), 0.7f, new Color(0.30f, 0.55f, 0.30f));
        }
    }
}
