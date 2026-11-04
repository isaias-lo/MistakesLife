#nullable enable

using MistakesLife.Core.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La vida del jugador en 3D, estilo maqueta: la pieza (muebles y una figura por persona),
    /// el barrio (la cuadra con el camino adonde va esa semana) o el lugar de trabajo,
    /// vistos desde arriba en diagonal.
    /// Lo que se ve sale de <see cref="HomeView"/> (núcleo): el nivel del hogar decide
    /// colores y muebles, y la etapa de vida decide cuna, cama o escritorio.
    /// Se dibuja dentro de un rectángulo de la pantalla que le indica <see cref="LifeScreen"/>.
    /// </summary>
    public sealed class HomeScene : MonoBehaviour
    {
        private static readonly Color Sky = new Color(0.80f, 0.88f, 0.94f);

        private Camera _camera = null!;
        private Transform _room = null!;
        private Transform _block = null!;
        private Transform _work = null!;
        private ShapeKit _kit = null!;
        private SceneView _view = SceneView.Room;
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
            _block = new GameObject("Barrio").transform;
            _block.SetParent(transform, false);
            _work = new GameObject("Trabajo").transform;
            _work.SetParent(transform, false);
            _kit = new ShapeKit(_room);
            ApplyMode();
        }

        /// <summary>Qué se está viendo: la pieza, el barrio o el trabajo.</summary>
        public SceneView View
        {
            get => _view;
            set
            {
                _view = value;
                ApplyMode();
            }
        }

        /// <summary>
        /// Dibuja la casa, el barrio y, si trabaja, el lugar de trabajo;
        /// si nada cambió desde la última vez, no hace nada.
        /// </summary>
        public void Show(HomeView home, DailyPlace place, WorkplaceView? workplace)
        {
            string signature = home.Signature + "|" + place + "|" + (workplace?.Signature ?? "-");
            if (signature == _shown)
            {
                return;
            }

            _shown = signature;
            Clear();

            _kit.Parent = _room;
            RoomBuilder.BuildRoom(_kit, home);
            RoomBuilder.BuildFurniture(_kit, home);
            RoomBuilder.BuildPeople(_kit, home.Members);

            _kit.Parent = _block;
            NeighborhoodBuilder.Build(_kit, home, place);

            if (workplace != null)
            {
                _kit.Parent = _work;
                OfficeBuilder.Build(_kit, workplace);
            }
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

        private void Clear()
        {
            ShapeKit.ClearChildren(_room);
            ShapeKit.ClearChildren(_block);
            ShapeKit.ClearChildren(_work);
            _kit.ReleaseMaterials();
        }

        /// <summary>Mueve la cámara y muestra solo lo que se está viendo.</summary>
        private void ApplyMode()
        {
            _room.gameObject.SetActive(_view == SceneView.Room);
            _block.gameObject.SetActive(_view == SceneView.Neighborhood);
            _work.gameObject.SetActive(_view == SceneView.Work);
            switch (_view)
            {
                case SceneView.Neighborhood:
                    _camera.transform.position = new Vector3(10f, 19f, -24f);
                    _camera.transform.LookAt(new Vector3(-1f, 0f, 0f));
                    break;
                case SceneView.Work:
                    _camera.transform.position = new Vector3(4.4f, 6.4f, -7.6f);
                    _camera.transform.LookAt(new Vector3(-0.2f, 0.5f, 0.4f));
                    break;
                default:
                    _camera.transform.position = new Vector3(3.6f, 5.4f, -6.4f);
                    _camera.transform.LookAt(new Vector3(-0.2f, 0.5f, 0.5f));
                    break;
            }
        }
    }
}
