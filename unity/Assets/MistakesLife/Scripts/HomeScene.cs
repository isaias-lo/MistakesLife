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

        /// <summary>Cielo nublado, para cuando el jugador está triste.</summary>
        private static readonly Color GreySky = new Color(0.62f, 0.64f, 0.68f);

        private Light _sun = null!;

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
            _sun = sun;
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
            SceneObjectBuilder.BuildRoomObjects(_kit, home);

            _kit.Parent = _block;
            NeighborhoodBuilder.Build(_kit, home, place);

            if (workplace != null)
            {
                _kit.Parent = _work;
                OfficeBuilder.Build(_kit, workplace);
            }
        }

        /// <summary>
        /// El ánimo del jugador se nota en la luz: con felicidad baja el cielo se nubla y
        /// el sol baja; desde 60 hacia arriba, día despejado. Es solo cómo se ve el número.
        /// </summary>
        public void SetMood(int happiness)
        {
            float clear = Mathf.Clamp01(happiness / 60f);
            _camera.backgroundColor = Color.Lerp(GreySky, Sky, clear);
            _sun.intensity = Mathf.Lerp(0.5f, 0.75f, clear);
        }

        /// <summary>
        /// Dibuja la vista actual en una imagen del tamaño pedido, sin pasar por la pantalla.
        /// Lo usan las capturas automáticas para revisar el diseño sin abrir el juego.
        /// </summary>
        public Texture2D RenderToTexture(int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            RenderTexture? previous = RenderTexture.active;
            _camera.targetTexture = target;
            _camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            _camera.targetTexture = null;
            target.Release();
            DestroyImmediate(target);
            return image;
        }

        /// <summary>
        /// Dibuja la vista actual dentro de un rectángulo (en píxeles) de una imagen que ya
        /// tiene otras cosas. Lo usan las capturas de la pantalla completa.
        /// </summary>
        public void RenderInto(RenderTexture target, Rect pixels)
        {
            _camera.targetTexture = target;
            _camera.pixelRect = pixels;
            _camera.Render();
            _camera.targetTexture = null;
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
                    // Más alta y más lejos: se ve la cuadra entera y el colegio no tapa las casas.
                    _camera.transform.position = new Vector3(-1f, 34f, -26f);
                    _camera.transform.LookAt(new Vector3(-1f, 0f, 2.5f));
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
