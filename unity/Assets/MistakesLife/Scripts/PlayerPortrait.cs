#nullable enable

using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// La cara de tu personaje para la cabecera y tu ficha (D6-1a): la misma figura del 3D, armada
    /// lejos de la escena y fotografiada por una cámara propia en una imagen chica. Se vuelve a
    /// sacar la foto solo cuando cambia algo que se ve (etapa de la vida, ánimo, bastón).
    /// </summary>
    public sealed class PlayerPortrait
    {
        /// <summary>Capa propia: solo la ve la cámara del retrato.</summary>
        private const int Layer = 30;

        /// <summary>Lejos de todo, para que no aparezca en la pieza ni en el barrio.</summary>
        private static readonly Vector3 Studio = new Vector3(0f, -500f, 0f);

        /// <summary>Hacia dónde mira la cara de las figuras (ver RoomBuilder.FaceTurn).</summary>
        private static readonly Vector3 FaceDirection = Quaternion.Euler(0f, -30f, 0f) * Vector3.back;

        private readonly Transform _root;
        private readonly Camera _camera;
        private string _key = string.Empty;

        /// <param name="owner">Dueño de la figura y la cámara: al destruirlo se va todo.</param>
        public PlayerPortrait(Transform owner)
        {
            _root = new GameObject("Retrato del jugador").transform;
            _root.SetParent(owner, false);
            _root.position = Studio;
            Texture = new RenderTexture(256, 256, 16) { antiAliasing = 4, name = "Retrato" };
            _camera = new GameObject("Camara del retrato").AddComponent<Camera>();
            _camera.transform.SetParent(_root, false);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = UiFactory.Option;
            _camera.cullingMask = 1 << Layer;
            _camera.fieldOfView = 30f;
            _camera.targetTexture = Texture;
            _camera.enabled = false;
            foreach (Camera other in Camera.allCameras)
            {
                if (other != _camera)
                {
                    other.cullingMask &= ~(1 << Layer);
                }
            }
        }

        /// <summary>La foto, para ponerla en una RawImage.</summary>
        public RenderTexture Texture { get; }

        /// <summary>Saca la foto de tu personaje (si algo que se ve cambió desde la última).</summary>
        public void Show(HomeMemberView player)
        {
            string key = $"{player.Id}|{player.Stage}|{player.IsFemale}|{player.AgeInYears >= 80}|{player.Happiness / 25}";
            if (key == _key)
            {
                return;
            }

            _key = key;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                Transform child = _root.GetChild(i);
                if (child != _camera.transform)
                {
                    Object.Destroy(child.gameObject);
                }
            }

            var holder = new GameObject("Figura").transform;
            holder.SetParent(_root, false);
            var kit = new ShapeKit(holder);
            RoomBuilder.Person(kit, player, Studio, 0, 0f);

            Transform? head = null;
            foreach (Transform part in holder.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = Layer;
                if (part.name == "Marca sobre la cabeza")
                {
                    part.gameObject.SetActive(false);
                }
                else if (part.name == "Cabeza")
                {
                    head = part;
                }
            }

            if (head == null)
            {
                return;
            }

            // La cabeza y un poco de hombros, de frente a la cara.
            float size = head.lossyScale.y;
            Vector3 target = head.position + new Vector3(0f, -size * 0.25f, 0f);
            _camera.transform.position = target + (FaceDirection * size * 4.6f) + new Vector3(0f, size * 0.35f, 0f);
            _camera.transform.LookAt(target);
            _camera.Render();
        }
    }
}
