#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MistakesLife.Unity.Editor
{
    /// <summary>
    /// Deja listo TextMeshPro, que usa toda la interfaz (docs/diseno-app.md, "Letras"):
    /// 1. Desempaca sus "recursos esenciales" (ajustes y sombreadores) si el proyecto no los tiene.
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
        /// Desempaca los recursos esenciales de TextMeshPro, que vienen dentro de Unity como un
        /// archivo .unitypackage. No se usa el importador de Unity porque trabaja "más tarde"
        /// (en el siguiente cuadro del editor) y en el armado automático ese momento no llega.
        /// Un .unitypackage es un .tar.gz con una carpeta por recurso: "asset" (el archivo),
        /// "asset.meta" (su id) y "pathname" (dónde va dentro del proyecto).
        /// </summary>
        private static void ImportEssentials()
        {
            string? package = FindEssentialsPackage();
            if (package == null)
            {
                Debug.LogError("TextMeshPro: no se encontró el archivo de sus recursos esenciales.");
                return;
            }

            Debug.Log($"TextMeshPro: desempacando {package}.");
            var assets = new Dictionary<string, byte[]>();
            var metas = new Dictionary<string, byte[]>();
            var paths = new Dictionary<string, string>();
            foreach (KeyValuePair<string, byte[]> entry in ReadTarGz(package))
            {
                string[] parts = entry.Key.Replace('\\', '/').TrimStart('.', '/').Split('/');
                if (parts.Length != 2)
                {
                    continue;
                }

                switch (parts[1])
                {
                    case "asset": assets[parts[0]] = entry.Value; break;
                    case "asset.meta": metas[parts[0]] = entry.Value; break;
                    case "pathname": paths[parts[0]] = Encoding.UTF8.GetString(entry.Value).Split('\n')[0].Trim(); break;
                }
            }

            string root = Directory.GetParent(Application.dataPath)!.FullName;
            int written = 0;
            foreach (KeyValuePair<string, string> item in paths)
            {
                string target = Path.Combine(root, item.Value);
                if (!item.Value.StartsWith("Assets/", StringComparison.Ordinal) || item.Value.Contains(".."))
                {
                    continue;
                }

                if (assets.TryGetValue(item.Key, out byte[]? content))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.WriteAllBytes(target, content);
                    written++;
                }
                else
                {
                    Directory.CreateDirectory(target);
                }

                if (metas.TryGetValue(item.Key, out byte[]? meta))
                {
                    File.WriteAllBytes(target + ".meta", meta);
                }
            }

            Debug.Log($"TextMeshPro: {written} archivos desempacados.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>Busca el archivo de recursos esenciales dentro del paquete de TextMeshPro (ugui en Unity 6).</summary>
        private static string? FindEssentialsPackage()
        {
            foreach (string name in new[] { "com.unity.ugui", "com.unity.textmeshpro" })
            {
                UnityEditor.PackageManager.PackageInfo? info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{name}");
                string folder = info != null ? info.resolvedPath : Path.GetFullPath($"Packages/{name}");
                string file = Path.Combine(folder, "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(file))
                {
                    return file;
                }
            }

            return null;
        }

        /// <summary>Lee un .tar.gz y devuelve cada archivo con su nombre. Solo lo básico del formato tar.</summary>
        private static IEnumerable<KeyValuePair<string, byte[]>> ReadTarGz(string file)
        {
            using var gzip = new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            using var data = new MemoryStream();
            gzip.CopyTo(data);
            byte[] tar = data.ToArray();

            int offset = 0;
            string? longName = null;
            while (offset + 512 <= tar.Length)
            {
                string name = Text(tar, offset, 100);
                if (name.Length == 0)
                {
                    break;
                }

                string prefix = Text(tar, offset + 345, 155);
                if (prefix.Length > 0)
                {
                    name = prefix + "/" + name;
                }

                long size = Convert.ToInt64(Text(tar, offset + 124, 12).Trim() is { Length: > 0 } octal ? octal : "0", 8);
                char type = (char)tar[offset + 156];
                offset += 512;
                byte[] content = new byte[size];
                Array.Copy(tar, offset, content, 0, size);
                offset += (int)((size + 511) / 512 * 512);

                if (type == 'L')
                {
                    longName = Encoding.UTF8.GetString(content).TrimEnd('\0');
                    continue;
                }

                if (longName != null)
                {
                    name = longName;
                    longName = null;
                }

                if (type == '0' || type == '\0')
                {
                    yield return new KeyValuePair<string, byte[]>(name, content);
                }
            }
        }

        private static string Text(byte[] bytes, int start, int length)
        {
            int end = start;
            while (end < start + length && bytes[end] != 0)
            {
                end++;
            }

            return Encoding.UTF8.GetString(bytes, start, end - start);
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
