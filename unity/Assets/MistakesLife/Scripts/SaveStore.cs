#nullable enable

using System.IO;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Dónde queda la partida en el teléfono: un solo archivo en la carpeta privada del juego
    /// (<c>Application.persistentDataPath</c>). El contenido lo arma el núcleo
    /// (<c>LifeSession.SaveToBytes</c>); aquí solo se escribe y se lee.
    /// </summary>
    public static class SaveStore
    {
        private const string FileName = "partida.mlsave";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Escribe primero un archivo temporal y después lo cambia por el bueno:
        /// si el teléfono se apaga a la mitad, el guardado anterior sigue sano.
        /// </summary>
        public static void Write(byte[] data)
        {
            string temp = FilePath + ".tmp";
            File.WriteAllBytes(temp, data);
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }

            File.Move(temp, FilePath);
        }

        /// <summary>Los bytes de la partida guardada, o null si no hay.</summary>
        public static byte[]? Read() => File.Exists(FilePath) ? File.ReadAllBytes(FilePath) : null;

        /// <summary>
        /// Aparta un guardado que no se pudo cargar (no se borra, por si se puede rescatar)
        /// para que el juego empiece una vida nueva.
        /// </summary>
        public static void SetAsideBroken()
        {
            if (File.Exists(FilePath))
            {
                string broken = FilePath + ".danado";
                if (File.Exists(broken))
                {
                    File.Delete(broken);
                }

                File.Move(FilePath, broken);
            }
        }
    }
}
