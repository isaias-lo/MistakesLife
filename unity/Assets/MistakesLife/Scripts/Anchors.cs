#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Las anclas de un lugar (pieza, oficina, barrio): puntos con nombre, como "ancla.mesa",
    /// donde se ponen los muebles y los objetos. Cada lugar trae sus anclas de siempre en código;
    /// si existe Resources/Lugares/&lt;lugar&gt;.prefab, sus hijos llamados "ancla.*" mandan.
    /// Así, para cambiar dónde va algo se mueve el ancla en el editor de Unity, no un número
    /// en el código (docs/diseno-app.md, sección 7). El menú "MistakesLife/Crear anclas de la
    /// pieza" crea ese archivo para empezar.
    /// </summary>
    public sealed class Anchors
    {
        private readonly Dictionary<string, Transform> _points = new Dictionary<string, Transform>();
        private readonly Transform _root;

        private Anchors(Transform root)
        {
            _root = root;
        }

        /// <summary>Crea las anclas del lugar bajo <paramref name="parent"/>.</summary>
        public static Anchors Create(Transform parent, string place, IReadOnlyDictionary<string, Vector3> defaults)
        {
            var root = new GameObject("Anclas").transform;
            root.SetParent(parent, false);
            var anchors = new Anchors(root);
            foreach (KeyValuePair<string, Vector3> point in defaults)
            {
                anchors.Set(point.Key, point.Value, Quaternion.identity);
            }

            GameObject? template = Resources.Load<GameObject>("Lugares/" + place);
            if (template != null)
            {
                Transform model = template.transform;
                for (int i = 0; i < model.childCount; i++)
                {
                    Transform child = model.GetChild(i);
                    if (child.name.StartsWith("ancla.", System.StringComparison.Ordinal))
                    {
                        anchors.Set(child.name, child.localPosition, child.localRotation);
                    }
                }
            }

            return anchors;
        }

        /// <summary>El ancla con ese nombre.</summary>
        public Transform this[string name] => _points[name];

        private void Set(string name, Vector3 position, Quaternion rotation)
        {
            if (!_points.TryGetValue(name, out Transform? point))
            {
                point = new GameObject(name).transform;
                point.SetParent(_root, false);
                _points[name] = point;
            }

            point.localPosition = position;
            point.localRotation = rotation;
        }
    }
}
