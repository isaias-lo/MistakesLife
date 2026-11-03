#nullable enable

using MistakesLife.Core.Content;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Carga el contenido del juego desde el archivo único de paquetes
    /// (Assets/MistakesLife/Resources/packs.bundle.json, lo crea tools/MistakesLife.UnityExport).
    /// </summary>
    public static class CoreLoader
    {
        /// <summary>Nombre del archivo dentro de Resources, sin extensión.</summary>
        public const string BundleResource = "packs.bundle";

        public static ContentDatabase LoadContent()
        {
            TextAsset? asset = Resources.Load<TextAsset>(BundleResource);
            if (asset == null)
            {
                throw new System.InvalidOperationException(
                    "Falta packs.bundle.json. Usa el menú MistakesLife > Actualizar núcleo y paquetes.");
            }

            return PackLoader.LoadBundle(PackBundle.FromJson(asset.text));
        }
    }
}
