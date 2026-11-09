#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Fábrica de formas simples de colores (cubos, esferas, cápsulas) para las escenas estilo maqueta.
    /// Guarda los materiales que crea para poder borrarlos (Unity no los borra solo).
    /// Los materiales de <see cref="Palette"/> no se borran: son compartidos por todo el juego.
    /// </summary>
    public sealed class ShapeKit
    {
        private readonly List<Material> _materials = new List<Material>();
        private readonly Dictionary<Color, Material> _byColor = new Dictionary<Color, Material>();

        public ShapeKit(Transform parent)
        {
            Parent = parent;
        }

        /// <summary>Material de base de todas las formas (Resources/FormaBase.mat).</summary>
        private static Material? _baseMaterial;

        private static Material? BaseMaterial
        {
            get
            {
                if (_baseMaterial == null)
                {
                    _baseMaterial = Resources.Load<Material>("FormaBase");
                }

                return _baseMaterial;
            }
        }

        /// <summary>Dónde se cuelgan las formas nuevas.</summary>
        public Transform Parent { get; set; }

        public GameObject Box(string name, Vector3 position, Vector3 size, Color color) =>
            Shape(PrimitiveType.Cube, name, position, size, color);

        public GameObject Box(string name, Vector3 position, Vector3 size, Material material) =>
            Shape(PrimitiveType.Cube, name, position, size, material);

        public GameObject Ball(string name, Vector3 position, float size, Color color) =>
            Shape(PrimitiveType.Sphere, name, position, new Vector3(size, size, size), color);

        public GameObject Ball(string name, Vector3 position, float size, Material material) =>
            Shape(PrimitiveType.Sphere, name, position, new Vector3(size, size, size), material);

        /// <summary>Forma de un color suelto. Las formas del mismo color comparten material.</summary>
        public GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color) =>
            Shape(type, name, position, scale, ColorMaterial(color));

        /// <summary>Forma con un material dado, por ejemplo uno de <see cref="Palette"/>.</summary>
        public GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            Kill(shape.GetComponent<Collider>());
            shape.transform.SetParent(Parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            return shape;
        }

        /// <summary>
        /// Material nuevo de un color, copiado de FormaBase (en Resources) y no del material que
        /// trae la forma: en el APK, Unity deja fuera los "shaders" que ningún archivo usa, y las
        /// formas se veían de color magenta. FormaBase obliga a incluir el shader Standard.
        /// </summary>
        public static Material NewMaterial(Color color)
        {
            Material template = BaseMaterial ?? new Material(Shader.Find("Standard")!);
            return new Material(template) { color = color };
        }

        private Material ColorMaterial(Color color)
        {
            if (!_byColor.TryGetValue(color, out Material? material))
            {
                material = NewMaterial(color);
                _byColor[color] = material;
                _materials.Add(material);
            }

            return material;
        }

        /// <summary>Borra todas las formas de un contenedor.</summary>
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Kill(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>Borra los materiales creados hasta ahora.</summary>
        public void ReleaseMaterials()
        {
            foreach (Material material in _materials)
            {
                Kill(material);
            }

            _materials.Clear();
            _byColor.Clear();
        }

        /// <summary>
        /// Borra un objeto: durante el juego con Destroy; en el editor (por ejemplo,
        /// al sacar capturas automáticas) Unity exige DestroyImmediate.
        /// </summary>
        private static void Kill(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
