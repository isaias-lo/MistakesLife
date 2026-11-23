#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
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
                TextSetup.Ensure();
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
                saved += Screens(content, folder);
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

                    if (view == SceneView.Room && name.EndsWith("22anos", StringComparison.Ordinal))
                    {
                        // La misma pieza girada (giro de 360°): desde el otro lado, con los muros de adelante escondidos.
                        foreach (int degrees in new[] { 120, 220 })
                        {
                            scene.Orbit(degrees);
                            Texture2D turned = scene.RenderToTexture(Width, Height);
                            File.WriteAllBytes(Path.Combine(folder, $"{name}-room-giro{degrees}.png"), turned.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(turned);
                            scene.Orbit(-degrees);
                            count++;
                        }
                    }
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
                ("oficio.vendedor", "lugar_trabajo.tienda", "tienda", 0, Grande),
                ("oficio.garzon", "lugar_trabajo.restoran", "restoran", 0, "empresa.tamano.pequena"),
                ("oficio.maestro", "lugar_trabajo.obra", "obra", 0, Grande),
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
            var objects = new System.Collections.Generic.List<string>();
            foreach (SceneObjectDefinition item in content.SceneObjects)
            {
                if (item.Place == "casa")
                {
                    objects.Add(item.Id);
                }
            }

            int count = 0;
            // La tercera es la vejez: pareja de 85 y 78 años (pelo gris, lentes y, a los 85, bastón).
            // Las últimas tres son lo que se puede arrendar: pieza, departamento y casa.
            foreach ((int level, int happiness, int age, string name, string? housing) in new (int, int, int, string, string?)[]
            {
                (30, 90, 40, "casa-todo-nivel30", null), (75, 15, 40, "casa-todo-nivel75-triste", null), (50, 70, 85, "casa-vejez", null),
                (40, 70, 26, "arriendo-pieza", "vivienda.pieza"), (40, 70, 30, "arriendo-departamento", "vivienda.departamento"), (40, 70, 34, "arriendo-casa", "vivienda.casa"),
            })
            {
                LifeStage stage = age >= 65 ? LifeStage.Elder : LifeStage.Adult;
                var player = new HomeMemberView(1, string.Empty, string.Empty, age, stage, false, true, happiness);
                var partner = new HomeMemberView(2, string.Empty, string.Empty, age - 7, stage, true, false, happiness);
                var home = new HomeView("comuna.puente_alto", level, stage, new[] { player, partner }, objects, housing);
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

        /// <summary>
        /// Capturas de la interfaz (no solo del 3D): la pantalla de la vida con un evento
        /// esperando respuesta y el menú de inicio, del tamaño de un celular (1080 x 1920).
        /// Sirven para revisar letras, colores y tamaños sin abrir el juego.
        /// </summary>
        private static int Screens(ContentDatabase content, string folder)
        {
            var life = new LifePresenter(new LifeSession(content, 1));
            if (!LiveUntil(life, content, "carrera.software.", 22))
            {
                return 0;
            }

            int count = 0;

            // Una semana tranquila: el diario, el estado en palabras y "Vivir la semana".
            if (life.Phase == LifePhase.Living)
            {
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-semana.png"));
            }

            // El celular con sus apps y la bandeja del correo, a los 22 años.
            if (!life.Phone().HasPhone)
            {
                Debug.LogWarning("Capturas: a los 22 la vida no tiene celular; pantalla-celular.png sale sin apps.");
            }

            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-celular.png"), phone: PhoneCapture.Home);
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-correo.png"), phone: PhoneCapture.Mail);

            // Mensajito: la lista de chats con la gente de su vida, y la conversación de más arriba.
            if (life.Chats().Count == 0)
            {
                Debug.LogWarning("Capturas: a los 22 nadie le ha escrito por Mensajito; pantalla-mensajito.png sale vacía.");
            }

            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-mensajito.png"), phone: PhoneCapture.Chats);
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-chat.png"), phone: PhoneCapture.Chat);

            // Tu dinero: el papel del cajero con los movimientos del último mes.
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-dinero.png"), money: true);

            // La carpeta del juzgado, con un juicio por deudas que ya avanzó unas semanas.
            count += CourtFolder(content, folder);

            // Los estudios: la libreta y el curso del colegio, y la libreta, la sección y la malla de la U.
            count += Studies(content, folder);

            // Tu equipo del trabajo: la jefatura y los colegas con nombre.
            count += Team(content, folder);

            // Lo que puedes hacer con el tiempo parado: Estudios a los 14, con las tardes libres del mes.
            count += Actions(content, folder);

            // Tu gente: la lista de cercanos y conocidos, y la ficha de una persona.
            count += People(content, folder);

            // Hasta que aparezca un evento, para que se vea la tarjeta de papel.
            for (int week = 0; week < 5 * 52 && life.Phase == LifePhase.Living; week++)
            {
                life.Session.Advance(1);
            }

            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-vida.png"));
            count += CaptureScreen(content, life, true, Path.Combine(folder, "pantalla-menu.png"));
            count += WitnessCard(content, folder);
            count += MomentScenes(content, folder);
            return count;
        }

        /// <summary>
        /// La escena sigue lo que te pasa (D5c3): la primera carta que pasa en tu casa con alguien,
        /// la primera en tu sala o tu pega, y la primera en un lugar del barrio, cada una con la
        /// escena en ese lugar y la persona del momento destacada.
        /// </summary>
        private static int MomentScenes(ContentDatabase content, string folder)
        {
            var files = new Dictionary<string, string>
            {
                [MomentSceneView.Home] = "pantalla-momento-casa.png",
                [MomentSceneView.Section] = "pantalla-momento-lugar.png",
                [MomentSceneView.Out] = "pantalla-momento-salida.png",
            };
            int count = 0;
            foreach (ulong seed in new ulong[] { 1, 2, 3 })
            {
                var life = new LifePresenter(new LifeSession(content, seed));
                for (int step = 0; step < 600 && files.Count > 0 && life.Phase != LifePhase.Over && life.Session.PlayerAgeInYears < 40; step++)
                {
                    if (life.Phase == LifePhase.Living)
                    {
                        life.AdvanceUntilSomething();
                        continue;
                    }

                    if (life.Phase != LifePhase.Choosing)
                    {
                        break;
                    }

                    MomentSceneView? scene = life.MomentScene();
                    if (scene != null && scene.PersonId != null && files.TryGetValue(scene.Scene, out string? file))
                    {
                        count += CaptureScreen(content, life, false, Path.Combine(folder, file));
                        files.Remove(scene.Scene);
                    }

                    life.Choose(0);
                }
            }

            foreach (string file in files.Values)
            {
                Debug.LogWarning("Capturas: no apareció un momento para " + file + ".");
            }

            // El cierre de un mes: la escena queda en el lugar de lo que más marcó, junto al resumen.
            var month = new LifePresenter(new LifeSession(content, 1));
            for (int step = 0; step < 400 && month.Phase != LifePhase.Over && month.Session.PlayerAgeInYears < 40; step++)
            {
                if (month.Phase == LifePhase.Choosing)
                {
                    month.Choose(0);
                    continue;
                }

                int start = month.Session.World.Date.TotalWeeks;
                month.AdvanceMonth();
                while (month.Phase == LifePhase.Choosing)
                {
                    month.Choose(0);
                }

                int weeks = month.Session.World.Date.TotalWeeks - start;
                MomentSceneView? scene = month.MonthScene(weeks);
                if (scene != null && scene.PersonId != null && scene.Scene != MomentSceneView.Home)
                {
                    count += CaptureScreen(content, month, false, Path.Combine(folder, "pantalla-mes-escena.png"), monthWeeks: weeks);
                    return count;
                }
            }

            Debug.LogWarning("Capturas: ningún mes dejó la escena en un lugar para pantalla-mes-escena.png.");

            return count;
        }

        /// <summary>
        /// La carta de un evento que se recuerda y tiene testigo (el amigo que pide plata):
        /// muestra el sello "ESTO SE RECUERDA" y la línea "Lo va a notar ..." bajo la opción.
        /// Se juega eligiendo siempre la primera opción hasta que aparece una carta así.
        /// </summary>
        private static int WitnessCard(ContentDatabase content, string folder)
        {
            var life = new LifePresenter(new LifeSession(content, 1));
            for (int step = 0; step < 400 && life.Phase != LifePhase.Over && life.Session.PlayerAgeInYears < 45; step++)
            {
                if (life.Phase == LifePhase.Living)
                {
                    life.AdvanceUntilSomething();
                    continue;
                }

                if (life.Phase != LifePhase.Choosing)
                {
                    break;
                }

                EventView card = life.Snapshot().PendingEvent!;
                if (card.Remembered && card.NoticedBy.Any(line => line.Length > 0))
                {
                    return CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-carta.png"));
                }

                life.Choose(0);
            }

            Debug.LogWarning("Capturas: no apareció una carta con testigo para pantalla-carta.png.");
            return 0;
        }

        /// <summary>
        /// Los estudios: una vida en Chile a los 13 (libreta y curso del colegio) que después
        /// estudia software y a los 20 muestra su libreta del semestre, su sección y su malla.
        /// </summary>
        private static int Studies(ContentDatabase content, string folder)
        {
            var life = new LifePresenter(new LifeSession(content, 1, "pais.chile"));
            int count = 0;
            if (LiveUntil(life, content, "carrera.software.", 13) && life.Phase == LifePhase.Living)
            {
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-libreta.png"), studies: 0);
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-curso.png"), studies: 1);
                // D5c: tu sala que se toca, sola y tocando a un compañero.
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-lugar-sala.png"), place: "estudios");
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-lugar-sala-persona.png"), place: "estudios", placePerson: true);
            }

            if (LiveUntil(life, content, "carrera.software.", 20) && life.Phase == LifePhase.Living && life.StudyPlan() != null)
            {
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-libreta-u.png"), studies: 0);
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-seccion.png"), studies: 1);
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-malla.png"), studies: 2);
                count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-lugar-aula.png"), place: "estudios");
            }
            else
            {
                Debug.LogWarning("Capturas: a los 20 la vida no estudia una carrera; no salen la sección ni la malla.");
            }

            return count;
        }

        /// <summary>Tu equipo del trabajo: la primera vida en Chile que a los 25 a 35 trabaja con equipo.</summary>
        private static int Team(ContentDatabase content, string folder)
        {
            for (ulong seed = 1; seed <= 10; seed++)
            {
                var life = new LifePresenter(new LifeSession(content, seed, "pais.chile"));
                for (int age = 25; age <= 35; age += 2)
                {
                    if (!LiveUntil(life, content, null, age) || life.Phase != LifePhase.Living)
                    {
                        break;
                    }

                    if (life.Team() != null)
                    {
                        int count = CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-equipo.png"), team: true);
                        // D5c: tu pega que se toca, sola y tocando a un colega.
                        count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-lugar-pega.png"), place: "trabajo");
                        count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-lugar-pega-persona.png"), place: "trabajo", placePerson: true);
                        return count;
                    }
                }
            }

            Debug.LogWarning("Capturas: ninguna vida trabajó con equipo; no sale pantalla-equipo.png.");
            return 0;
        }

        /// <summary>Lo que puedes hacer en Estudios a los 14 (D3): el contador del mes y la lista con lo que da cada cosa.</summary>
        private static int Actions(ContentDatabase content, string folder)
        {
            var life = new LifePresenter(new LifeSession(content, 1, "pais.chile"));
            if (!LiveUntil(life, content, null, 14))
            {
                return 0;
            }

            // Hasta un mes tranquilo con algo que hacer (con un momento encima, primero se decide).
            for (int week = 0; week < 52; week++)
            {
                if (life.Phase == LifePhase.Choosing)
                {
                    life.Choose(Pick(life, content, null));
                }
                else if (life.Phase == LifePhase.Living && life.Actions("estudios").Count == 0)
                {
                    life.Session.Advance(1);
                }
                else
                {
                    break;
                }
            }

            if (life.Phase != LifePhase.Living || life.Actions("estudios").Count == 0)
            {
                Debug.LogWarning("Capturas: a los 14 no hubo nada que hacer en Estudios; no sale pantalla-acciones.png.");
                return 0;
            }

            return CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-acciones.png"), actions: "estudios");
        }

        /// <summary>Tu gente (D4a) a los 16: la lista de cercanos y conocidos, y la ficha de la primera persona.</summary>
        private static int People(ContentDatabase content, string folder)
        {
            var life = new LifePresenter(new LifeSession(content, 1, "pais.chile"));
            if (!LiveUntil(life, content, null, 16) || life.Phase != LifePhase.Living)
            {
                Debug.LogWarning("Capturas: la vida no llegó a los 16; no sale pantalla-gente.png.");
                return 0;
            }

            int count = CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-gente.png"), people: 0);
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-gente-ficha.png"), people: 1);
            // D5c2: el mapa de tu barrio, el mall con el cine elegido, elegir con quién y la salida (esta va al final: hace la salida).
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-barrio.png"), map: true);
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-barrio-lugar.png"), map: true, mapPlace: "lugar.barrio.mall");
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-barrio-elegir.png"), map: true, mapPlace: "lugar.barrio.mall", mapChoose: true);
            count += CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-barrio-salida.png"), map: true, mapPlace: "lugar.barrio.mall", mapGo: true);
            return count;
        }

        /// <summary>
        /// La carpeta del juzgado: una vida en Chile hasta los 30 a la que le abren un juicio por
        /// deudas, y unas semanas después, para que se vea la línea del proceso avanzando.
        /// </summary>
        private static int CourtFolder(ContentDatabase content, string folder)
        {
            var session = new LifeSession(content, 1, "pais.chile");
            var life = new LifePresenter(session);
            if (!LiveUntil(life, content, null, 30))
            {
                return 0;
            }

            Effect.Parse("abrir_causa('causa.chile.cobro_deuda')").Apply(new EventScope(session.World, content, session.World.Player, null, readOnly: false));
            for (int week = 0; week < 12 && life.Phase == LifePhase.Living; week++)
            {
                session.Advance(1);
            }

            return CaptureScreen(content, life, false, Path.Combine(folder, "pantalla-juzgado.png"), court: true);
        }

        /// <summary>Qué parte del celular se abre encima de la vida en una captura.</summary>
        private enum PhoneCapture
        {
            None,
            Home,
            Mail,
            Chats,
            Chat,
        }

        /// <summary>Saca una captura de la pantalla de la vida (con el menú o el celular encima si se pide) y la borra.</summary>
        private static int CaptureScreen(ContentDatabase content, LifePresenter life, bool menu, string file, PhoneCapture phone = PhoneCapture.None, bool money = false, bool court = false, int studies = -1, bool team = false, string? actions = null, int people = -1, string? place = null, bool placePerson = false, bool map = false, string? mapPlace = null, bool mapChoose = false, bool mapGo = false, int monthWeeks = 0)
        {
            var root = new GameObject("Captura de pantalla");
            try
            {
                LifeScreen screen = root.AddComponent<LifeScreen>();
                screen.ShowForCapture(content, life, menu);
                if (phone != PhoneCapture.None)
                {
                    screen.OpenPhoneForCapture(phone == PhoneCapture.Mail, phone == PhoneCapture.Chats || phone == PhoneCapture.Chat, phone == PhoneCapture.Chat);
                }

                if (money)
                {
                    screen.OpenMoneyForCapture();
                }

                if (court)
                {
                    screen.OpenCourtForCapture();
                }

                if (studies >= 0)
                {
                    screen.OpenStudiesForCapture(studies);
                }

                if (team)
                {
                    screen.OpenTeamForCapture();
                }

                if (actions != null)
                {
                    screen.OpenActionsForCapture(actions);
                }

                if (people >= 0)
                {
                    screen.OpenPeopleForCapture(people == 1);
                }

                if (place != null && !screen.OpenPlaceForCapture(place, placePerson))
                {
                    Debug.LogWarning($"Capturas: la vida no tiene lugar de '{place}'; {Path.GetFileName(file)} sale sin él.");
                }

                if (monthWeeks > 0)
                {
                    screen.ShowMonthForCapture(monthWeeks);
                }

                if (map && !screen.OpenMapForCapture(mapPlace, mapChoose, mapGo))
                {
                    Debug.LogWarning($"Capturas: la vida no tiene mapa del barrio (o '{mapPlace}'); {Path.GetFileName(file)} sale sin él.");
                }

                File.WriteAllBytes(file, RenderInterface(1080, 1920));
                return 1;
            }
            finally
            {
                // La pantalla crea su lienzo y su escena 3D en la raíz: se borra todo lo nuevo.
                foreach (GameObject item in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (item != null && item.transform.parent == null && item.GetComponent<Camera>() == null)
                    {
                        UnityEngine.Object.DestroyImmediate(item);
                    }
                }
            }
        }

        /// <summary>
        /// Dibuja la pantalla como en el celular y devuelve el PNG: todos los lienzos de la
        /// interfaz con una cámara propia, y la casa 3D como imagen dentro de su ventana.
        /// Esa cámara se pone lejos de la maqueta para que los muebles no tapen la interfaz.
        /// </summary>
        private static byte[] RenderInterface(int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            Camera camera = new GameObject("Camara de la captura").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 10000f, 0f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiFactory.Background;
            camera.targetTexture = target;
            var pictures = new List<Texture2D>();
            try
            {
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 10f;
                }

                // Dos pasadas: la primera mide los textos y la segunda acomoda las columnas con esas medidas.
                for (int pass = 0; pass < 2; pass++)
                {
                    Canvas.ForceUpdateCanvases();
                    foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    {
                        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                    }
                }

                // La casa se dibuja aparte, del tamaño de su ventana, y se pone como imagen al fondo
                // de esa ventana: así queda bajo los botones "Casa" y "Barrio", como en el celular.
                foreach (HomeScene home in UnityEngine.Object.FindObjectsByType<HomeScene>(FindObjectsSortMode.None))
                {
                    RectTransform? window = home.Viewport;
                    if (window == null || !window.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var corners = new Vector3[4];
                    window.GetWorldCorners(corners);
                    Vector3 min = camera.WorldToScreenPoint(corners[0]);
                    Vector3 max = camera.WorldToScreenPoint(corners[2]);
                    int w = Mathf.Max(1, Mathf.RoundToInt(max.x - min.x));
                    int h = Mathf.Max(1, Mathf.RoundToInt(max.y - min.y));
                    // La casa se centra en el hueco libre entre la barra de arriba y la hoja de abajo.
                    float shift = 0f;
                    if (home.Focus != null)
                    {
                        var focus = new Vector3[4];
                        home.Focus.GetWorldCorners(focus);
                        float center = (camera.WorldToScreenPoint(focus[0]).y + camera.WorldToScreenPoint(focus[2]).y) / 2f;
                        shift = (((center - min.y) / h) * 2f) - 1f;
                    }

                    Texture2D picture = home.RenderToTexture(w, h, shift);
                    pictures.Add(picture);

                    RectTransform holder = UiFactory.Child(window, "Casa capturada");
                    UiFactory.Stretch(holder);
                    holder.SetAsFirstSibling();
                    holder.gameObject.AddComponent<UnityEngine.UI.RawImage>().texture = picture;
                }

                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                byte[] png = image.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(image);
                return png;
            }
            finally
            {
                foreach (Texture2D picture in pictures)
                {
                    UnityEngine.Object.DestroyImmediate(picture);
                }

                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
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
