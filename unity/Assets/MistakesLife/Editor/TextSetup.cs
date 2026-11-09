#nullable enable

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MistakesLife.Unity.Editor
{
    /// <summary>
    /// Deja listo TextMeshPro, que usa toda la interfaz (docs/diseno-app.md, "Letras"):
    /// 1. Importa sus "recursos esenciales" (ajustes y sombreadores) si el proyecto no los tiene.
    ///    Es lo mismo que el menú Window > TextMeshPro > Import TMP Essential Resources.
    /// 2. Pide que sus sombreadores vayan siempre en el APK, porque las letras se crean
    ///    al abrir el juego (ver Fonts) y Unity no sabría que se usan.
    /// Corre sola al abrir el editor, al armar el APK y al sacar capturas. No es parte del juego.
    /// </summary>
    [InitializeOnLoad]
    public static class TextSetup
    {
        /// <summary>Los sombreadores con que TextMeshPro dibuja las letras.</summary>
        private static readonly string[] Shaders =
        {
            "TextMeshPro/Mobile/Distance Field",
            "TextMeshPro/Distance Field",
        };

        static TextSetup()
        {
            // Después de que el editor termine de cargar (no en medio de una compilación).
            EditorApplication.delayCall += () => Ensure();
        }

        [MenuItem("MistakesLife/Preparar letras")]
        public static void EnsureFromMenu() => Ensure();

        /// <summary>Devuelve true si TextMeshPro quedó listo.</summary>
        public static bool Ensure()
        {
            if (!HasEssentials())
            {
                ImportEssentials();
            }

            bool ready = HasEssentials();
            if (!ready)
            {
                Debug.LogError("TextMeshPro: no se pudieron importar sus recursos esenciales. Ábrelos a mano en Window > TextMeshPro > Import TMP Essential Resources.");
            }

            IncludeShaders();
            return ready;
        }

        private static bool HasEssentials() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        /// <summary>
        /// Llama al importador de TextMeshPro sin ventanas. Se busca por su nombre para no
        /// depender de en qué versión de Unity cambió de lugar.
        /// </summary>
        private static void ImportEssentials()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? importer = assembly.GetType("TMPro.TMP_PackageResourceImporter");
                MethodInfo? import = importer?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
                if (import != null)
                {
                    Debug.Log("TextMeshPro: importando sus recursos esenciales.");
                    import.Invoke(null, new object[] { true, false, false });
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    return;
                }
            }

            Debug.LogError("TextMeshPro: no se encontró su importador de recursos.");
        }

        /// <summary>Agrega los sombreadores de TextMeshPro a "Always Included Shaders" (Project Settings > Graphics).</summary>
        private static void IncludeShaders()
        {
            UnityEngine.Object? settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null)
            {
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty list = serialized.FindProperty("m_AlwaysIncludedShaders");
            bool changed = false;
            foreach (string name in Shaders)
            {
                Shader? shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"TextMeshPro: no se encontró el sombreador {name}.");
                    continue;
                }

                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    present |= list.GetArrayElementAtIndex(i).objectReferenceValue == shader;
                }

                if (!present)
                {
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                    changed = true;
                }
            }

            if (changed)
            {
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
