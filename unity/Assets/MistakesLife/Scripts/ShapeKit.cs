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
            Object.Destroy(shape.GetComponent<Collider>());
            shape.transform.SetParent(Parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            Material material = shape.GetComponent<Renderer>().material;
            material.color = color;
            _materials.Add(material);
            return shape;
        }

        /// <summary>Borra todas las formas de un contenedor.</summary>
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>Borra los materiales creados hasta ahora.</summary>
        public void ReleaseMaterials()
        {
            foreach (Material material in _materials)
            {
                Object.Destroy(material);
            }

            _materials.Clear();
        }
    }
}
