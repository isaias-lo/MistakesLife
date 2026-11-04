#nullable enable

using System;
using System.IO;
using MistakesLife.Core.Content;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using UnityEditor;
using UnityEngine;

namespace MistakesLife.Unity.Editor
{
    /// <summary>
    /// Saca capturas de las escenas 3D (pieza, barrio y trabajo) de algunas vidas fijas
    /// a distintas edades, y las deja como PNG en la carpeta "capturas" del repositorio.
    /// Sirve para revisar el diseño sin abrir el juego: lo corre GitHub
    /// (.github/workflows/capturas.yml) o el menú "MistakesLife > Sacar capturas".
    /// No es parte del juego.
    /// </summary>
    public static class SceneCaptures
    {
        /// <summary>Números de partida fijos: siempre salen las mismas vidas, así se comparan cambios.</summary>
        private static readonly ulong[] Seeds = { 1, 7, 42 };

        /// <summary>Edades en que se saca la foto: guagua, niño, adolescente, joven y adulto.</summary>
        private static readonly int[] Ages = { 1, 8, 15, 22, 35 };

        /// <summary>Tamaño de la ventana 3D en un teléfono de 1080 de ancho.</summary>
        private const int Width = 1016;
        private const int Height = 560;

        [MenuItem("MistakesLife/Sacar capturas")]
        public static void CaptureFromMenu() => Capture();

        /// <summary>Punto de entrada para el armado automático (parámetro -executeMethod).</summary>
        public static void Capture()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "capturas"));
            Directory.CreateDirectory(folder);
            int saved = 0;
            try
            {
                ContentDatabase content = CoreLoader.LoadContent();
                foreach (ulong seed in Seeds)
                {
                    var presenter = new LifePresenter(new LifeSession(content, seed));
                    foreach (int age in Ages)
                    {
                        if (!LiveUntil(presenter, age))
                        {
                            break;
                        }

                        saved += Photograph(presenter.Snapshot(), folder, $"vida{seed}-{age:00}anos");
                    }
                }

                Debug.Log($"Capturas: {saved} imágenes en {folder}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Finish(false);
                return;
            }

            Finish(saved > 0);
        }

        /// <summary>
        /// Avanza la vida semana a semana hasta la edad pedida, eligiendo siempre la primera
        /// opción. Devuelve false si la vida terminó antes.
        /// </summary>
        private static bool LiveUntil(LifePresenter presenter, int age)
        {
            for (int week = 0; week < 200 * 52 && presenter.Session.PlayerAgeInYears < age; week++)
            {
                switch (presenter.Phase)
                {
                    case LifePhase.Choosing:
                        presenter.Choose(0);
                        break;
                    case LifePhase.Living:
                        presenter.Session.Advance(1);
                        break;
                    default:
                        return false;
                }
            }

            return presenter.Session.PlayerAgeInYears >= age;
        }

        /// <summary>Una foto por vista (pieza, barrio y, si trabaja, el trabajo).</summary>
        private static int Photograph(LifeSnapshot life, string folder, string name)
        {
            HomeScene scene = HomeScene.Create();
            int count = 0;
            try
            {
                scene.Show(life.Home, life.DailyPlace, life.Workplace);
                foreach (SceneView view in new[] { SceneView.Room, SceneView.Neighborhood, SceneView.Work })
                {
                    if (view == SceneView.Work && life.Workplace == null)
                    {
                        continue;
                    }

                    scene.View = view;
                    Texture2D image = scene.RenderToTexture(Width, Height);
                    File.WriteAllBytes(Path.Combine(folder, $"{name}-{view.ToString().ToLowerInvariant()}.png"), image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    count++;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scene.gameObject);
            }

            return count;
        }

        private static void Finish(bool ok)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }
    }
}
