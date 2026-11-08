#nullable enable

using System;
using System.IO;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
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
        /// <summary>
        /// Vidas fijas (número de partida y la carrera que esa vida elige cuando puede),
        /// así siempre salen las mismas y se comparan cambios. Una por lugar de trabajo:
        /// oficina abierta, cubículos, consulta y, sin estudiar, el local de atención.
        /// </summary>
        private static readonly (ulong Seed, string? Career)[] Lives =
        {
            (1, "carrera.software."),
            (7, "carrera.contabilidad."),
            (42, "carrera.psicologia."),
            (3, null),
            (42, "carrera.contabilidad."),
        };

        private const string CareerPrefix = "carrera.";

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
                foreach ((ulong seed, string? career) in Lives)
                {
                    var presenter = new LifePresenter(new LifeSession(content, seed));
                    foreach (int age in Ages)
                    {
                        if (!LiveUntil(presenter, content, career, age))
                        {
                            break;
                        }

                        saved += Photograph(presenter.Snapshot(), folder, $"vida{seed}-{age:00}anos");
                    }
                }

                saved += Showcase(folder);
                saved += RoomShowcase(content, folder);
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
        /// Avanza la vida semana a semana hasta la edad pedida. Devuelve false si la vida terminó antes.
        /// </summary>
        private static bool LiveUntil(LifePresenter presenter, ContentDatabase content, string? career, int age)
        {
            for (int week = 0; week < 200 * 52 && presenter.Session.PlayerAgeInYears < age; week++)
            {
                switch (presenter.Phase)
                {
                    case LifePhase.Choosing:
                        presenter.Choose(Pick(presenter, content, career));
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

        /// <summary>
        /// Qué opción elige la vida de prueba: la primera de su carrera si está disponible;
        /// si no, la primera que no sea estudiar una carrera (así la vida sin carrera no estudia).
        /// </summary>
        private static int Pick(LifePresenter presenter, ContentDatabase content, string? career)
        {
            PendingEvent pending = presenter.Session.Pending!;
            EventDefinition ev = content.GetEvent(pending.EventId);
            int fallback = -1;
            for (int position = 0; position < pending.AvailableOptions.Count; position++)
            {
                string key = ev.Options[pending.AvailableOptions[position]].TextKey;
                if (career != null && key.StartsWith(career, StringComparison.Ordinal))
                {
                    return position;
                }

                if (fallback < 0 && !key.StartsWith(CareerPrefix, StringComparison.Ordinal))
                {
                    fallback = position;
                }
            }

            return fallback < 0 ? 0 : fallback;
        }

        /// <summary>Una foto por vista (pieza, barrio y, si trabaja, el trabajo).</summary>
        private static int Photograph(LifeSnapshot life, string folder, string name)
        {
            HomeScene scene = HomeScene.Create();
            int count = 0;
            try
            {
                scene.Show(life.Home, life.DailyPlace, life.Workplace);
                scene.SetMood(life.Happiness);
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

        /// <summary>
        /// Vitrina: cada tipo de lugar de trabajo con un cargo alto (rango 3), para revisar
        /// los muebles que da un ascenso aunque ninguna vida de prueba llegue tan alto.
        /// Usa la casa de la primera vida solo como fondo; la foto es de la vista Trabajo.
        /// </summary>
        private static int Showcase(string folder)
        {
            var player = new HomeMemberView(1, string.Empty, string.Empty, 40, LifeStage.Adult, false, true);
            // Rango 3 en una empresa grande (con letrero) y una microempresa para comparar.
            const string Grande = "empresa.tamano.grande";
            var kinds = new (string? Career, string? Kind, string Name, int Rank, string? Size)[]
            {
                ("carrera.software", "lugar_trabajo.oficina_abierta", "oficina_abierta-rango3", 3, Grande),
                ("carrera.contabilidad", "lugar_trabajo.oficina_cubiculos", "oficina_cubiculos-rango3", 3, Grande),
                ("carrera.psicologia", "lugar_trabajo.consulta", "consulta-rango3", 3, Grande),
                ("carrera.otra", null, "oficina_comun-rango3", 3, Grande),
                ("carrera.software", "lugar_trabajo.oficina_abierta", "oficina_abierta-micro", 0, "empresa.tamano.micro"),
            };
            var home = new HomeView("comuna.puente_alto", 50, LifeStage.Adult, new[] { player });
            int count = 0;
            foreach ((string? career, string? kind, string name, int rank, string? size) in kinds)
            {
                HomeScene scene = HomeScene.Create();
                try
                {
                    scene.Show(home, DailyPlace.Work, new WorkplaceView(career, kind, 3.0, false, player, rank, null, size));
                    scene.View = SceneView.Work;
                    Texture2D image = scene.RenderToTexture(Width, Height);
                    File.WriteAllBytes(Path.Combine(folder, $"vitrina-{name}-work.png"), image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    count++;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(scene.gameObject);
                }
            }

            return count;
        }

        /// <summary>
        /// Vitrina de la pieza con todos los objetos de la casa del paquete a la vez (aunque en
        /// una vida no salgan juntos), con día despejado y con cielo nublado, para revisar
        /// que cada objeto se vea y no choque con los muebles.
        /// </summary>
        private static int RoomShowcase(ContentDatabase content, string folder)
        {
            var player = new HomeMemberView(1, string.Empty, string.Empty, 40, LifeStage.Adult, false, true);
            var partner = new HomeMemberView(2, string.Empty, string.Empty, 40, LifeStage.Adult, true, false);
            var objects = new System.Collections.Generic.List<string>();
            foreach (SceneObjectDefinition item in content.SceneObjects)
            {
                if (item.Place == "casa")
                {
                    objects.Add(item.Id);
                }
            }

            int count = 0;
            foreach ((int level, int happiness, string name) in new[] { (30, 90, "casa-todo-nivel30"), (75, 15, "casa-todo-nivel75-triste") })
            {
                var home = new HomeView("comuna.puente_alto", level, LifeStage.Adult, new[] { player, partner }, objects);
                HomeScene scene = HomeScene.Create();
                try
                {
                    scene.Show(home, DailyPlace.Home, null);
                    scene.SetMood(happiness);
                    scene.View = SceneView.Room;
                    Texture2D image = scene.RenderToTexture(Width, Height);
                    File.WriteAllBytes(Path.Combine(folder, $"vitrina-{name}-room.png"), image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    count++;
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(scene.gameObject);
                }
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
