#nullable enable

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MistakesLife.Unity.Editor
{
    /// <summary>
    /// Menú para empezar a editar un lugar en el editor de Unity (docs/diseno-app.md, sección 7).
    /// Crea Resources/Lugares/pieza.prefab con las anclas de siempre ("ancla.mesa", "ancla.cama"...),
    /// cada una con un cubito de guía para verla y moverla. Desde ese momento el juego usa
    /// las posiciones de ese archivo: mover un ancla mueve el mueble y los objetos que van en ella.
    /// No es parte del juego.
    /// </summary>
    public static class PlaceTemplates
    {
        private const string Folder = "Assets/MistakesLife/Resources/Lugares";

        [MenuItem("MistakesLife/Crear anclas de la pieza")]
        public static void CreateRoomAnchors()
        {
            // Una pieza de nivel medio (ancho 8 m) como referencia.
            Save(RoomBuilder.Place, RoomBuilder.DefaultAnchors(50));
        }

        private static void Save(string place, IReadOnlyDictionary<string, Vector3> anchors)
        {
            string path = $"{Folder}/{place}.prefab";
            if (File.Exists(path) && !EditorUtility.DisplayDialog("MistakesLife", $"{path} ya existe. ¿Reemplazarlo con las anclas de siempre?", "Reemplazar", "Cancelar"))
            {
                return;
            }

            Directory.CreateDirectory(Folder);
            var root = new GameObject(place);
            try
            {
                foreach (KeyValuePair<string, Vector3> anchor in anchors)
                {
                    var point = new GameObject(anchor.Key);
                    point.transform.SetParent(root.transform, false);
                    point.transform.localPosition = anchor.Value;

                    // Cubito de guía: solo para ver el ancla en el editor (el juego lee solo la posición).
                    GameObject guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    guide.name = "Guia";
                    guide.transform.SetParent(point.transform, false);
                    guide.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                    Object.DestroyImmediate(guide.GetComponent<Collider>());
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.Refresh();
                Debug.Log($"Anclas guardadas en {path}. Ábrelo y mueve las anclas: el juego las usa al tiro.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
