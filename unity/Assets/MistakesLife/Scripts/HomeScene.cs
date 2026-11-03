#nullable enable

using System.Collections.Generic;
using MistakesLife.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La casa del jugador en 3D, estilo maqueta: una pieza vista desde arriba en diagonal,
    /// con muebles hechos de formas simples y una figura por persona.
    /// Lo que se ve sale de <see cref="HomeView"/> (núcleo): el nivel del hogar decide
    /// colores y muebles, y la etapa de vida decide cuna, cama o escritorio.
    /// Se dibuja dentro de un rectángulo de la pantalla que le indica <see cref="LifeScreen"/>.
    /// </summary>
    public sealed class HomeScene : MonoBehaviour
    {
        private static readonly Color Sky = new Color(0.80f, 0.88f, 0.94f);

        private readonly List<Material> _materials = new List<Material>();
        private Camera _camera = null!;
        private Transform _room = null!;
        private RectTransform? _viewport;
        private string? _shown;

        /// <summary>El rectángulo de la interfaz donde se ve la casa.</summary>
        public RectTransform? Viewport
        {
            get => _viewport;
            set => _viewport = value;
        }

        /// <summary>Crea la escena (cámaras, luz y raíz de la pieza).</summary>
        public static HomeScene Create()
        {
            var go = new GameObject(nameof(HomeScene));
            HomeScene scene = go.AddComponent<HomeScene>();
            scene.Setup();
            return scene;
        }

        private void Setup()
        {
            // Cámara de fondo: pinta toda la pantalla del color de la interfaz.
            // Va después de cualquier cámara que ya traiga la escena (profundidad -1), así la tapa.
            Camera background = new GameObject("Camara de fondo").AddComponent<Camera>();
            background.transform.SetParent(transform, false);
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = UiFactory.Background;
            background.cullingMask = 0;
            background.depth = -0.5f;

            // Cámara de la casa: vista de maqueta, en diagonal desde arriba.
            _camera = new GameObject("Camara de la casa").AddComponent<Camera>();
            _camera.transform.SetParent(transform, false);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Sky;
            _camera.fieldOfView = 30f;
            _camera.depth = 0;
            _camera.transform.position = new Vector3(3.6f, 5.4f, -6.4f);
            _camera.transform.LookAt(new Vector3(-0.2f, 0.5f, 0.5f));

            Light sun = new GameObject("Sol").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.intensity = 0.75f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            // Luz ambiente pareja (sin cielo), para que los colores no se quemen.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);

            _room = new GameObject("Pieza").transform;
            _room.SetParent(transform, false);
        }

        /// <summary>Dibuja la casa; si no cambió desde la última vez, no hace nada.</summary>
        public void Show(HomeView home)
        {
            if (home.Signature == _shown)
            {
                return;
            }

            _shown = home.Signature;
            Clear();
            BuildRoom(home);
            BuildFurniture(home);
            BuildPeople(home.Members);
        }

        private void LateUpdate()
        {
            if (_viewport == null)
            {
                return;
            }

            // El lienzo es de pantalla completa: sus esquinas ya están en píxeles.
            var corners = new Vector3[4];
            _viewport.GetWorldCorners(corners);
            float width = corners[2].x - corners[0].x;
            float height = corners[2].y - corners[0].y;
            if (width > 1f && height > 1f)
            {
                _camera.pixelRect = new Rect(corners[0].x, corners[0].y, width, height);
            }
        }

        private void BuildRoom(HomeView home)
        {
            int level = home.HouseholdLevel;
            float width = 6f + (level / 25f);
            const float depth = 5f;

            Color floor = level < 35 ? new Color(0.62f, 0.62f, 0.60f) : new Color(0.66f, 0.50f, 0.34f);
            Color wall = level < 35 ? new Color(0.86f, 0.74f, 0.60f) : level < 65 ? new Color(0.86f, 0.80f, 0.68f) : new Color(0.90f, 0.88f, 0.82f);

            Box("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(width, 0.1f, depth), floor);
            Box("Muro del fondo", new Vector3(0f, 1.3f, (depth / 2f) + 0.05f), new Vector3(width, 2.6f, 0.1f), wall);
            Box("Muro lateral", new Vector3((-width / 2f) - 0.05f, 1.3f, 0f), new Vector3(0.1f, 2.6f, depth + 0.2f), wall * 0.92f);
            Box("Ventana", new Vector3(width / 4f, 1.6f, (depth / 2f) - 0.01f), new Vector3(1.4f, 0.9f, 0.04f), new Color(0.62f, 0.80f, 0.92f));
            Box("Puerta", new Vector3(-width / 2f + 0.01f, 1.0f, -depth / 4f), new Vector3(0.04f, 2.0f, 0.9f), new Color(0.45f, 0.32f, 0.22f));

            if (level >= 50)
            {
                Box("Alfombra", new Vector3(0.6f, 0.01f, -0.6f), new Vector3(2.4f, 0.02f, 1.6f), new Color(0.70f, 0.42f, 0.36f));
            }
        }

        private void BuildFurniture(HomeView home)
        {
            int level = home.HouseholdLevel;
            float left = -2.4f;

            // Rincón del jugador según su etapa.
            switch (home.PlayerStage)
            {
                case LifeStage.Baby:
                    Box("Cuna", new Vector3(left, 0.35f, 1.6f), new Vector3(1.0f, 0.7f, 0.6f), new Color(0.95f, 0.92f, 0.85f));
                    Box("Colchon de la cuna", new Vector3(left, 0.55f, 1.6f), new Vector3(0.9f, 0.1f, 0.5f), new Color(0.75f, 0.85f, 0.95f));
                    break;
                case LifeStage.Child:
                case LifeStage.Teen:
                    Box("Cama", new Vector3(left, 0.25f, 1.4f), new Vector3(0.9f, 0.5f, 1.9f), new Color(0.40f, 0.55f, 0.75f));
                    Box("Escritorio", new Vector3(left + 1.6f, 0.4f, 2.1f), new Vector3(1.2f, 0.8f, 0.6f), new Color(0.72f, 0.58f, 0.40f));
                    break;
                default:
                    // De adulto, otro adulto en la casa es la pareja: cama de dos plazas.
                    bool couple = false;
                    foreach (HomeMemberView member in home.Members)
                    {
                        couple |= !member.IsPlayer && member.Stage >= LifeStage.Adult;
                    }

                    Box("Cama", new Vector3(left + 0.3f, 0.25f, 1.4f), new Vector3(couple ? 1.6f : 0.9f, 0.5f, 1.9f), new Color(0.55f, 0.45f, 0.62f));
                    break;
            }

            // Comedor: siempre hay una mesa.
            Box("Mesa", new Vector3(1.6f, 0.4f, 0.9f), new Vector3(1.4f, 0.08f, 0.9f), new Color(0.58f, 0.42f, 0.28f));
            Box("Pata de la mesa", new Vector3(1.6f, 0.2f, 0.9f), new Vector3(0.12f, 0.4f, 0.12f), new Color(0.40f, 0.28f, 0.18f));

            if (level >= 25)
            {
                Box("Sillon", new Vector3(0.4f, 0.3f, -1.6f), new Vector3(1.8f, 0.6f, 0.7f), new Color(0.35f, 0.45f, 0.40f));
            }

            if (level >= 40)
            {
                Box("Tele", new Vector3(0.4f, 0.9f, 2.35f), new Vector3(1.2f, 0.7f, 0.06f), new Color(0.12f, 0.12f, 0.14f));
            }

            if (level >= 60)
            {
                Box("Macetero", new Vector3(2.8f, 0.25f, 2.0f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.60f, 0.38f, 0.26f));
                Ball("Planta", new Vector3(2.8f, 0.8f, 2.0f), 0.7f, new Color(0.30f, 0.55f, 0.30f));
            }
        }

        private void BuildPeople(IReadOnlyList<HomeMemberView> members)
        {
            float spacing = 0.9f;
            float start = -((members.Count - 1) * spacing) / 2f;
            for (int i = 0; i < members.Count; i++)
            {
                HomeMemberView member = members[i];
                Vector3 position = new Vector3(start + (i * spacing), 0f, -0.4f);
                Person(member, position, i);
            }
        }

        /// <summary>Una figura: cuerpo, cabeza y, para el jugador, un círculo amarillo a sus pies.</summary>
        private void Person(HomeMemberView member, Vector3 feet, int index)
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
            GameObject torso = Shape(PrimitiveType.Capsule, member.Name, feet + new Vector3(0f, body / 2f, 0f), new Vector3(body * 0.45f, body / 2f, body * 0.45f), shirt);
            Ball("Cabeza", feet + new Vector3(0f, body + (head / 2f), 0f), head, skin).transform.SetParent(torso.transform, true);
            float hairSize = member.IsFemale ? head * 1.12f : head * 1.02f;
            Ball("Pelo", feet + new Vector3(0f, body + (head * 0.62f), 0.04f), hairSize, hair).transform.SetParent(torso.transform, true);

            if (member.IsPlayer)
            {
                Shape(PrimitiveType.Cylinder, "Marca del jugador", feet + new Vector3(0f, 0.02f, 0f), new Vector3(0.7f, 0.01f, 0.7f), new Color(0.98f, 0.82f, 0.25f));
            }
        }

        private GameObject Box(string name, Vector3 position, Vector3 size, Color color) =>
            Shape(PrimitiveType.Cube, name, position, size, color);

        private GameObject Ball(string name, Vector3 position, float size, Color color) =>
            Shape(PrimitiveType.Sphere, name, position, new Vector3(size, size, size), color);

        private GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            Destroy(shape.GetComponent<Collider>());
            shape.transform.SetParent(_room, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            Renderer renderer = shape.GetComponent<Renderer>();
            Material material = renderer.material;
            material.color = color;
            _materials.Add(material);
            return shape;
        }

        private void Clear()
        {
            for (int i = _room.childCount - 1; i >= 0; i--)
            {
                Destroy(_room.GetChild(i).gameObject);
            }

            // Los materiales copiados no se borran solos.
            foreach (Material material in _materials)
            {
                Destroy(material);
            }

            _materials.Clear();
        }
    }
}
