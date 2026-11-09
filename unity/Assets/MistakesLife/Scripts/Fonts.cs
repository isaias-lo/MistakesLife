#nullable enable

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Las letras del juego para TextMeshPro. Los archivos .ttf están en
    /// Resources/Fuentes (todas de Google Fonts, con licencia OFL: ver la carpeta Licencias).
    /// Cada letra se convierte en una fuente de TextMeshPro la primera vez que se usa,
    /// así no hay que preparar nada a mano en el editor.
    /// Para cambiar una letra basta reemplazar su archivo .ttf (con el mismo nombre).
    /// </summary>
    public static class Fonts
    {
        private const string Folder = "Fuentes/";

        private static readonly Dictionary<Face, TMP_FontAsset?> Loaded = new Dictionary<Face, TMP_FontAsset?>();

        /// <summary>Los archivos de cada letra (sin ".ttf").</summary>
        public static readonly IReadOnlyDictionary<Face, string> Files = new Dictionary<Face, string>
        {
            { Face.Body, "Figtree-Regular" },
            { Face.Title, "Fraunces-SemiBold" },
            { Face.Mono, "IBMPlexMono-Regular" },
            { Face.Hand, "GochiHand-Regular" },
        };

        /// <summary>La versión gruesa del texto normal, para lo que va en negrita.</summary>
        public const string BodyBold = "Figtree-Bold";

        /// <summary>La fuente de TextMeshPro de esa letra, o null si falta su archivo (se usa la de TextMeshPro).</summary>
        public static TMP_FontAsset? Get(Face face)
        {
            if (Loaded.TryGetValue(face, out TMP_FontAsset? asset))
            {
                return asset;
            }

            asset = Create(Files[face]);
            if (asset != null && face == Face.Body)
            {
                // La negrita usa la Figtree gruesa de verdad, no una engordada a la fuerza.
                TMP_FontAsset? bold = Create(BodyBold);
                if (bold != null && asset.fontWeightTable != null && asset.fontWeightTable.Length > 7)
                {
                    // La posición 7 de la tabla es el peso 700 (negrita).
                    asset.fontWeightTable[7].regularTypeface = bold;
                }
            }

            if (asset != null && face != Face.Body)
            {
                // Si a una letra le falta un signo (por ejemplo, un emoji), lo dibuja la normal.
                TMP_FontAsset? body = Get(Face.Body);
                if (body != null)
                {
                    asset.fallbackFontAssetTable = new List<TMP_FontAsset> { body };
                }
            }

            Loaded[face] = asset;
            return asset;
        }

        private static TMP_FontAsset? Create(string file)
        {
            Font? font = Resources.Load<Font>(Folder + file);
            if (font == null)
            {
                Debug.LogError($"Falta la letra {Folder}{file}.ttf en Resources.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = file;
            return asset;
        }
    }
}
