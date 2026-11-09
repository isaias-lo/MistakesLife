#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Un modelo por id (docs/diseno-app.md, sección 7). Para cada objeto de la maqueta
    /// (un mueble como "mueble.mesa" o un objeto de escena como "escena.casa.diploma")
    /// se busca primero un modelo en Resources/Modelos/&lt;id&gt;.prefab. Si existe, se usa ese;
    /// si no, se dibuja la forma simple de reserva. Así se cambia cómo se ve un objeto
    /// arrastrando un modelo con ese nombre, sin tocar código.
    /// </summary>
    public static class Models
    {
        /// <summary>Modelos ya buscados (también los que no existen, para no buscarlos de nuevo).</summary>
        private static readonly Dictionary<string, GameObject?> Found = new Dictionary<string, GameObject?>();

        /// <summary>
        /// Pone el objeto <paramref name="id"/> en el ancla. Si no hay modelo, <paramref name="fallback"/>
        /// dibuja la forma simple con <paramref name="kit"/>, en posiciones medidas desde el ancla.
        /// </summary>
        public static GameObject Place(ShapeKit kit, Transform anchor, string id, Action<ShapeKit> fallback)
        {
            var holder = new GameObject(id);
            holder.transform.SetParent(anchor, false);

            GameObject? model = Find(id);
            if (model != null)
            {
                GameObject copy = Object.Instantiate(model, holder.transform, false);
                copy.name = id + " (modelo)";
                return holder;
            }

            Transform previous = kit.Parent;
            kit.Parent = holder.transform;
            try
            {
                fallback(kit);
            }
            finally
            {
                kit.Parent = previous;
            }

            return holder;
        }

        /// <summary>El modelo del id, o null si nadie lo ha hecho todavía.</summary>
        public static GameObject? Find(string id)
        {
            if (!Found.TryGetValue(id, out GameObject? model))
            {
                model = Resources.Load<GameObject>("Modelos/" + id);
                Found[id] = model;
            }

            return model;
        }
    }
}
