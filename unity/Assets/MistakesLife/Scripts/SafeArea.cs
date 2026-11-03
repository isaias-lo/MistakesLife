#nullable enable

using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Ajusta un panel a la zona segura de la pantalla, para que la cámara frontal
    /// o los bordes curvos del celular no tapen botones ni textos.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect _applied;

        private void Update()
        {
            Rect area = Screen.safeArea;
            if (area == _applied || Screen.width == 0 || Screen.height == 0)
            {
                return;
            }

            _applied = area;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
