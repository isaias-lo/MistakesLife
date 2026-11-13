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
        private RectTransform? _focus;
        private string? _shown;

        /// <summary>Cuánto giró el jugador la cámara alrededor de la maqueta (0 = la vista de siempre).</summary>
        private float _yaw;

        /// <summary>
        /// Cuánto se ve a lo ancho: el ancho que tenía la ventana 3D de antes (1016 x 560 con 30°
        /// de alto). Con la pantalla a lo alto se abre el ángulo vertical para que la pieza quepa
        /// igual a lo ancho, un poco más cerca (sobraba cielo a los lados). En las capturas
        /// apaisadas el ángulo no baja de 30°, así que quedan exactamente como antes.
        /// </summary>
        private const float HalfWidthTan = 0.445f;

        /// <summary>El rectángulo de la interfaz donde se ve la casa.</summary>
        public RectTransform? Viewport
        {
            get => _viewport;
            set => _viewport = value;
        }

        /// <summary>
        /// La parte de la pantalla que queda libre entre la barra de arriba y la hoja de abajo:
        /// la casa se centra ahí, aunque el 3D se dibuje en toda la pantalla.
        /// </summary>
        public RectTransform? Focus
        {
            get => _focus;
            set => _focus = value;
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

        /// <summary>Qué se está viendo: la pieza, el barrio o el trabajo. Al cambiar, la cámara vuelve a su vista de siempre.</summary>
        public SceneView View
        {
            get => _view;
            set
            {
                _view = value;
                _yaw = 0f;
                ApplyMode();
            }
        }

        /// <summary>Cuánto está girada la cámara, de 0 a 360 grados.</summary>
        public float Yaw => _yaw;

        /// <summary>
        /// Gira la cámara alrededor de la maqueta (positivo, hacia la derecha), sin límite:
        /// se puede dar la vuelta entera. Los muros que quedan entre la cámara y la pieza se
        /// esconden, como en una maqueta a la que se le saca una pared para mirar adentro.
        /// </summary>
        public void Orbit(float degrees)
        {
            _yaw = Mathf.Repeat(_yaw + degrees, 360f);
            ApplyMode();
        }

        /// <summary>
        /// El objeto de la maqueta que está bajo un punto de la pantalla (en píxeles): su id, como
        /// "mueble.mesa" o "escena.casa.celular", o null si ahí no hay nada con id. Si hay varios
        /// (el celular sobre la mesa), gana el más chico: es el que se quiso tocar.
        /// </summary>
        public string? Pick(Vector2 screenPoint)
        {
            Transform root = _view switch
            {
                SceneView.Neighborhood => _block,
                SceneView.Work => _work,
                _ => _room,
            };

            string? best = null;
            float bestArea = float.MaxValue;
            foreach (Transform item in root.GetComponentsInChildren<Transform>())
            {
                if (!item.name.StartsWith("escena.", System.StringComparison.Ordinal) && !item.name.StartsWith("mueble.", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (ScreenRect(item, out Rect rect) && rect.Contains(screenPoint) && rect.width * rect.height < bestArea)
                {
                    // Un modelo hecho a mano se llama "<id> (modelo)": cuenta como su id.
                    int space = item.name.IndexOf(' ');
                    best = space > 0 ? item.name.Substring(0, space) : item.name;
                    bestArea = rect.width * rect.height;
                }
            }

            return best;
        }

        /// <summary>El rectángulo de la pantalla que ocupa un objeto (todas sus formas visibles).</summary>
        private bool ScreenRect(Transform item, out Rect rect)
        {
            rect = default;
            bool any = false;
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            foreach (Renderer shape in item.GetComponentsInChildren<Renderer>())
            {
                if (!shape.enabled)
                {
                    continue;
                }

                Bounds bounds = shape.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    Vector3 screen = _camera.WorldToScreenPoint(point);
                    if (screen.z <= 0f)
                    {
                        continue;
                    }

                    min = Vector2.Min(min, screen);
                    max = Vector2.Max(max, screen);
                    any = true;
                }
            }

            if (any)
            {
                rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            return any;
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
            Anchors anchors = RoomBuilder.CreateAnchors(_room, home);
            RoomBuilder.BuildRoom(_kit, home, anchors);
            RoomBuilder.BuildFurniture(_kit, home, anchors);
            RoomBuilder.BuildPeople(_kit, home.Members);
            SceneObjectBuilder.BuildRoomObjects(_kit, home, anchors);

            _kit.Parent = _block;
            NeighborhoodBuilder.Build(_kit, home, place);

            if (workplace != null)
            {
                _kit.Parent = _work;
                OfficeBuilder.Build(_kit, workplace);
            }

            HideWallsInFront();
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
        public Texture2D RenderToTexture(int width, int height, float shift = 0f)
        {
            var target = new RenderTexture(width, height, 24);
            RenderTexture? previous = RenderTexture.active;
            _camera.targetTexture = target;
            Fit(width / (float)height, shift);
            _camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            _camera.targetTexture = null;
            _camera.ResetProjectionMatrix();
            target.Release();
            DestroyImmediate(target);
            return image;
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
                Fit(width / height, FocusShift(corners[0].y, height));
            }
        }

        /// <summary>
        /// Cuánto subir la imagen (de -1 a 1, en alto de pantalla) para que la casa quede al centro
        /// del hueco libre (<see cref="Focus"/>) y no detrás de la barra o de la hoja.
        /// </summary>
        public float FocusShift(float viewportBottom, float viewportHeight)
        {
            if (_focus == null || viewportHeight <= 1f)
            {
                return 0f;
            }

            var corners = new Vector3[4];
            _focus.GetWorldCorners(corners);
            float center = ((corners[0].y + corners[2].y) / 2f) - viewportBottom;
            return ((center / viewportHeight) * 2f) - 1f;
        }

        /// <summary>
        /// Ajusta la cámara a la forma de la imagen: el ángulo vertical se abre para que a lo ancho
        /// se vea lo mismo, y la imagen se corre hacia arriba o abajo según <paramref name="shift"/>.
        /// </summary>
        private void Fit(float aspect, float shift)
        {
            _camera.ResetProjectionMatrix();
            _camera.aspect = aspect;
            _camera.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(HalfWidthTan / aspect) * Mathf.Rad2Deg, 30f, 100f);
            if (Mathf.Abs(shift) > 0.001f)
            {
                Matrix4x4 projection = _camera.projectionMatrix;
                projection[1, 2] = -shift;
                _camera.projectionMatrix = projection;
            }
        }

        private void Clear()
        {
            ShapeKit.ClearChildren(_room);
            ShapeKit.ClearChildren(_block);
            ShapeKit.ClearChildren(_work);
            _kit.ReleaseMaterials();
        }

        /// <summary>
        /// Mueve la cámara y muestra solo lo que se está viendo. Cada vista tiene su punto de
        /// siempre (desde donde se mira) y su centro (adonde se mira); el giro del jugador
        /// mueve la cámara en círculo alrededor de ese centro, a la misma altura.
        /// </summary>
        private void ApplyMode()
        {
            _room.gameObject.SetActive(_view == SceneView.Room);
            _block.gameObject.SetActive(_view == SceneView.Neighborhood);
            _work.gameObject.SetActive(_view == SceneView.Work);
            (Vector3 eye, Vector3 center) = _view switch
            {
                // Más alta y más lejos: se ve la cuadra entera y el colegio no tapa las casas.
                SceneView.Neighborhood => (new Vector3(-1f, 34f, -26f), new Vector3(-1f, 0f, 2.5f)),
                SceneView.Work => (new Vector3(4.4f, 6.4f, -7.6f), new Vector3(-0.2f, 0.5f, 0.4f)),
                _ => (new Vector3(3.6f, 5.4f, -6.4f), new Vector3(-0.2f, 0.5f, 0.5f)),
            };

            _camera.transform.position = center + (Quaternion.Euler(0f, _yaw, 0f) * (eye - center));
            _camera.transform.LookAt(center);
            HideWallsInFront();
        }

        /// <summary>
        /// Los muros (las formas que se llaman "Muro ...") que quedan entre la cámara y el centro
        /// de la pieza o del trabajo se esconden; los del otro lado se ven. Con la vista de
        /// siempre no se esconde ninguno.
        /// </summary>
        private void HideWallsInFront()
        {
            Vector3 toCamera = _camera.transform.position;
            toCamera.y = 0f;
            foreach (Transform root in new[] { _room, _work })
            {
                foreach (Renderer shape in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!shape.name.StartsWith("Muro", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Hacia dónde está el muro desde el centro de su lugar, en el piso.
                    Vector3 side = shape.transform.position - root.position;
                    side.y = 0f;
                    shape.enabled = Vector3.Dot(side.normalized, toCamera.normalized) < 0.25f;
                }
            }
        }
    }
}
