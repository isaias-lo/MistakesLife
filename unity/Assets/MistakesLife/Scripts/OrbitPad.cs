#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MistakesLife.Unity
{
    /// <summary>
    /// El hueco de la pantalla donde se ve la maqueta 3D: arrastrar el dedo de lado gira la
    /// cámara alrededor (una pasada de todo el ancho de la pantalla es una vuelta entera, 360°),
    /// y tocar sin arrastrar avisa dónde se tocó, para abrir el objeto que está ahí
    /// (la mesa abre Tu dinero, el celular abre el celular...).
    /// Los botones que están encima (Casa, Barrio, Trabajo) siguen recibiendo sus toques.
    /// </summary>
    public sealed class OrbitPad : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        /// <summary>La escena que gira.</summary>
        public HomeScene? Home { get; set; }

        /// <summary>Se tocó la maqueta sin arrastrar, en este punto de la pantalla (en píxeles).</summary>
        public Action<Vector2>? Tapped { get; set; }

        /// <summary>Se giró la maqueta por primera vez desde que se abrió la pantalla.</summary>
        public Action? Turned { get; set; }

        private bool _turned;

        /// <summary>Pone el hueco a recibir toques: una imagen invisible que lo cubre entero.</summary>
        public static OrbitPad AddTo(RectTransform area, HomeScene home)
        {
            Image hit = area.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            OrbitPad pad = area.gameObject.AddComponent<OrbitPad>();
            pad.Home = home;
            return pad;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Nada que hacer: lo importante pasa en OnDrag. Unity pide este método para
            // saber que aquí se arrastra y no tratar el arrastre como un toque.
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Home == null || Screen.width <= 0)
            {
                return;
            }

            Home.Orbit(eventData.delta.x / Screen.width * 360f);
            if (!_turned)
            {
                _turned = true;
                Turned?.Invoke();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging)
            {
                Tapped?.Invoke(eventData.position);
            }
        }
    }
}
