#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Fábrica de formas simples de colores (cubos, esferas, cápsulas) para las escenas estilo maqueta.
    /// Guarda los materiales que crea para poder borrarlos (Unity no los borra solo).
    /// </summary>
    public sealed class ShapeKit
    {
        private readonly List<Material> _materials = new List<Material>();

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

        public GameObject Ball(string name, Vector3 position, float size, Color color) =>
            Shape(PrimitiveType.Sphere, name, position, new Vector3(size, size, size), color);

        public GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            Kill(shape.GetComponent<Collider>());
            shape.transform.SetParent(Parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            // Material propio de cada forma (sirve igual en el juego y en el editor).
            // Se copia de FormaBase (en Resources) y no del material que trae la forma:
            // en el APK, Unity deja fuera los "shaders" que ningún archivo usa, y las formas
            // se veían de color magenta. FormaBase obliga a incluir el shader Standard.
            Renderer renderer = shape.GetComponent<Renderer>();
            Material template = BaseMaterial ?? renderer.sharedMaterial;
            var material = new Material(template) { color = color };
            renderer.sharedMaterial = material;
            _materials.Add(material);
            return shape;
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
