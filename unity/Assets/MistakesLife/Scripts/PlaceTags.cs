#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Pone cada nombre del lugar (D5c) justo encima de su persona o su cosa, en cada cuadro:
    /// así siguen a la figura cuando se gira la maqueta. Si la figura no se ve, su nombre se esconde.
    /// </summary>
    public sealed class PlaceTags : MonoBehaviour
    {
        /// <summary>Cuánto más arriba de la cabeza va el nombre, en unidades de la interfaz.</summary>
        private const float Lift = 12f;

        private readonly List<(string Id, RectTransform Tag)> _tags = new List<(string Id, RectTransform Tag)>();

        /// <summary>La escena donde están las figuras.</summary>
        public HomeScene? Home { get; set; }

        /// <summary>Los nombres a seguir: el id del objeto en la escena y su etiqueta.</summary>
        public void Follow(IEnumerable<(string Id, RectTransform Tag)> tags)
        {
            _tags.Clear();
            _tags.AddRange(tags);
            LateUpdate();
        }

        private void LateUpdate()
        {
            RectTransform? window = Home?.Viewport;
            if (Home == null || window == null)
            {
                return;
            }

            var corners = new Vector3[4];
            window.GetWorldCorners(corners);
            float up = Lift * window.lossyScale.y;
            foreach ((string id, RectTransform tag) in _tags)
            {
                if (tag == null)
                {
                    continue;
                }

                bool seen = Home.TopOf(id, out Vector2 point);
                tag.gameObject.SetActive(seen);
                if (seen)
                {
                    tag.position = new Vector3(
                        Mathf.Lerp(corners[0].x, corners[2].x, point.x),
                        Mathf.Lerp(corners[0].y, corners[2].y, point.y) + up,
                        tag.position.z);
                }
            }
        }
    }
}
