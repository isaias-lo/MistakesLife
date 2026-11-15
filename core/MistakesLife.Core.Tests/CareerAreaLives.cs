using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Worlds;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Tests
{
    /// <summary>
    /// Vidas que empiezan a trabajar en cada área de cada carrera a los 25 años y siguen
    /// hasta los 60, decidiendo solas. Sirven para revisar que los eventos de áreas poco
    /// comunes (por ejemplo, peritajes forenses) se puedan vivir: en vidas al azar casi
    /// nadie llega a ellas.
    /// </summary>
    internal static class CareerAreaLives
    {
        /// <summary>Marcas que piden las áreas especializadas de los paquetes.</summary>
        private static readonly string[] RequirementFlags =
        {
            "certificacion_ciberseguridad", "certificacion_datos", "diplomado_tributacion", "curso_legislacion_laboral", "especialidad_forense",
        };

        /// <summary>Ids de los eventos que aparecieron en alguna de esas vidas.</summary>
        public static HashSet<string> SeenEvents(ContentDatabase content, int livesPerArea, int startAge = 25, int finalAge = 60)
        {
            var seen = new HashSet<string>();
            foreach (CareerDefinition career in content.Careers)
            {
                // Un oficio no se estudia: se entra directo al área.
                CareerLevelDefinition? best = career.Levels.OrderByDescending(l => l.SalaryRelative).FirstOrDefault();
                string workFlag = career.WorkFlag ?? "trabaja_" + career.Id.Substring(career.Id.IndexOf('.') + 1);
                foreach (CareerAreaDefinition area in career.Areas)
                {
                    for (int i = 0; i < livesPerArea; i++)
                    {
                        var session = new LifeSession(content, 500 + (ulong)i);
                        Play(session, startAge);
                        if (!session.World.Player.IsAlive)
                        {
                            continue;
                        }

                        var effects = new List<string>();
                        if (session.World.Player.Studies != null)
                        {
                            effects.Add("jugador.desertar()");
                        }

                        if (best != null)
                        {
                            effects.Add($"estudiar('{best.Id}')");
                            effects.Add("jugador.titularse()");
                        }

                        effects.AddRange(RequirementFlags.Select(f => $"jugador.marcar('{f}')"));
                        effects.Add("jugador.desmarcar('buscando_trabajo')");
                        effects.Add("jugador.marcar('trabajando')");
                        effects.Add($"jugador.marcar('{workFlag}')");
                        effects.Add($"jugador.trabajar_en('{area.Id}')");
                        var scope = new EventScope(session.World, content, session.World.Player, null, readOnly: false);
                        foreach (string effect in effects)
                        {
                            Effect.Parse(effect).Apply(scope);
                        }

                        Play(session, finalAge);
                        seen.UnionWith(session.World.EventHistory.Select(r => r.EventId));
                    }
                }
            }

            return seen;
        }

        /// <summary>
        /// Una vida por carrera que se titula a los 25 con todas las semillas de su carrera
        /// plantadas (por ejemplo, haber roto la confidencialidad de un paciente en la
        /// práctica). Sirve para los eventos que vienen de una semilla que se planta en
        /// cerca del 1 % de las vidas: sin esto, que aparezcan depende de la suerte.
        /// </summary>
        public static HashSet<string> CareerSeedEvents(ContentDatabase content, int startAge = 25, int finalAge = 40)
        {
            var seen = new HashSet<string>();
            ulong lifeSeed = 900;
            foreach (CareerDefinition career in content.Careers.Where(c => !c.IsOccupation))
            {
                var session = new LifeSession(content, lifeSeed++);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                CareerLevelDefinition best = career.Levels.OrderByDescending(l => l.SalaryRelative).First();
                string shortName = career.Id.Substring("carrera.".Length);
                var effects = new List<string>();
                if (session.World.Player.Studies != null)
                {
                    effects.Add("jugador.desertar()");
                }

                effects.Add($"estudiar('{best.Id}')");
                effects.Add("jugador.titularse()");
                effects.Add("jugador.desmarcar('buscando_trabajo')");
                effects.Add("jugador.marcar('trabajando')");
                effects.Add($"jugador.marcar('trabaja_{shortName}')");
                effects.Add($"jugador.trabajar_en('{career.Areas[0].Id}')");
                effects.AddRange(content.SeedTypes
                    .Where(s => s.Id.StartsWith("semilla." + shortName + "."))
                    .Select(s => $"plantar('{s.Id}')"));
                var scope = new EventScope(session.World, content, session.World.Player, null, readOnly: false);
                foreach (string effect in effects)
                {
                    Effect.Parse(effect).Apply(scope);
                }

                Play(session, finalAge);
                seen.UnionWith(session.World.EventHistory.Select(r => r.EventId));
            }

            return seen;
        }

        /// <summary>
        /// Vidas de personas que trabajan con la salud muy baja a los 40 años, para los eventos
        /// de dejar el trabajo por salud y volver al recuperarse, que en una vida al azar le
        /// pasan a cerca del 1 %.
        /// </summary>
        public static HashSet<string> SickWorkerEvents(ContentDatabase content, int lives, int startAge = 40, int finalAge = 55)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 700 + (ulong)i);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                var scope = new EventScope(session.World, content, session.World.Player, null, readOnly: false);
                foreach (string effect in new[] { "jugador.desmarcar('buscando_trabajo')", "jugador.marcar('trabajando')", "jugador.trabajar(sueldo_titulo())", "jugador.salud = 30" })
                {
                    Effect.Parse(effect).Apply(scope);
                }

                int before = session.World.EventHistory.Count;
                Play(session, finalAge);
                seen.UnionWith(session.World.EventHistory.Skip(before).Select(r => r.EventId));
            }

            return seen;
        }

        /// <summary>
        /// Vidas de personas que trabajan por su cuenta (a honorarios) desde los 30, para los
        /// eventos de quien trabaja sin equipo (conocer a alguien del rubro), que en una vida al
        /// azar le pasan a menos del 1 %: casi todos trabajan con jefe y colegas.
        /// </summary>
        public static HashSet<string> FreelanceWorkerEvents(ContentDatabase content, int lives, int startAge = 30, int finalAge = 40)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 750 + (ulong)i);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                var scope = new EventScope(session.World, content, session.World.Player, null, readOnly: false);
                foreach (string effect in new[] { "jugador.desmarcar('buscando_trabajo')", "jugador.marcar('trabajando')", "jugador.marcar('trabaja_honorarios')", "jugador.trabajar(sueldo_titulo(), 'honorarios')" })
                {
                    Effect.Parse(effect).Apply(scope);
                }

                int before = session.World.EventHistory.Count;
                Play(session, finalAge);
                seen.UnionWith(session.World.EventHistory.Skip(before).Select(r => r.EventId));
            }

            return seen;
        }

        /// <summary>
        /// Vidas de personas que ya ascendieron una vez y andan mal en el trabajo (desempeño
        /// bajo y en la mira de su jefatura) después de ascender, para el evento de bajar de cargo,
        /// que en una vida al azar le pasa a menos del 1 %.
        /// </summary>
        public static HashSet<string> StrugglingWorkerEvents(ContentDatabase content, int lives, int startAge = 30, int finalAge = 42)
        {
            var seen = new HashSet<string>();
            CareerDefinition career = content.Careers.First(c => !c.IsOccupation);
            CareerLevelDefinition best = career.Levels.OrderByDescending(l => l.SalaryRelative).First();
            string shortName = career.Id.Substring("carrera.".Length);
            CareerAreaDefinition area = career.Areas.First();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 800 + (ulong)i);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                var effects = new List<string>();
                if (session.World.Player.Studies != null)
                {
                    effects.Add("jugador.desertar()");
                }

                effects.Add($"estudiar('{best.Id}')");
                effects.Add("jugador.titularse()");
                effects.Add("jugador.desmarcar('buscando_trabajo')");
                effects.Add("jugador.marcar('trabajando')");
                effects.Add($"jugador.marcar('trabaja_{shortName}')");
                effects.Add($"jugador.trabajar_en('{area.Id}')");
                Apply(session, effects);

                // Trabaja bien unos años, hasta que puede ascender; después de ascender, le va mal.
                for (int year = startAge + 1; year < finalAge && session.World.Player.IsAlive && session.World.Player.Job?.Position != null; year++)
                {
                    Apply(session, new[] { "jugador.desempeno = 95" });
                    Play(session, year);
                    var check = new EventScope(session.World, content, session.World.Player, null, readOnly: true);
                    if (session.World.Player.IsAlive && session.World.Player.Job?.Position != null
                        && Expression.Parse("jugador.puede_ascender").Evaluate(check).AsBool("prueba"))
                    {
                        Apply(session, new[] { "jugador.ascender()", "jugador.desempeno = 30", "jugador.marcar('en_la_mira')" });
                        int before = session.World.EventHistory.Count;
                        Play(session, finalAge);
                        seen.UnionWith(session.World.EventHistory.Skip(before).Select(r => r.EventId));
                        break;
                    }
                }
            }

            return seen;
        }

        /// <summary>
        /// Vidas que trabajan muy bien y ascienden cada vez que pueden, hasta llegar a lo más
        /// alto de la carrera (en vidas al azar, pocas llegan): para el evento de la cima.
        /// </summary>
        public static HashSet<string> TopOfCareerEvents(ContentDatabase content, int lives, int startAge = 28, int finalAge = 60)
        {
            var seen = new HashSet<string>();
            CareerDefinition career = content.Careers.First(c => !c.IsOccupation);
            CareerLevelDefinition best = career.Levels.OrderByDescending(l => l.SalaryRelative).First();
            string shortName = career.Id.Substring("carrera.".Length);
            CareerAreaDefinition area = career.Areas.First();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 1300 + (ulong)i);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                var effects = new List<string>();
                if (session.World.Player.Studies != null)
                {
                    effects.Add("jugador.desertar()");
                }

                effects.Add($"estudiar('{best.Id}')");
                effects.Add("jugador.titularse()");
                effects.AddRange(RequirementFlags.Select(f => $"jugador.marcar('{f}')"));
                effects.Add("jugador.desmarcar('buscando_trabajo')");
                effects.Add("jugador.marcar('trabajando')");
                effects.Add($"jugador.marcar('trabaja_{shortName}')");
                effects.Add($"jugador.trabajar_en('{area.Id}')");
                Apply(session, effects);

                int before = session.World.EventHistory.Count;
                for (int year = startAge + 1; year < finalAge && session.World.Player.IsAlive && session.World.Player.Job?.Position != null; year++)
                {
                    Apply(session, new[] { "jugador.desempeno = 95" });
                    Play(session, year);
                    var check = new EventScope(session.World, content, session.World.Player, null, readOnly: true);
                    if (session.World.Player.IsAlive && session.World.Player.Job?.Position != null
                        && Expression.Parse("jugador.puede_ascender").Evaluate(check).AsBool("prueba"))
                    {
                        // Con dos caminos (por ejemplo, jefatura o especialista) toma el primero.
                        var paths = Economy.CareerLadder.EligiblePromotions(content, session.World.Player.Job!, check);
                        Apply(session, new[] { $"jugador.ascender('{paths[0].Id}')" });
                    }

                    if (session.World.Player.IsAlive && Expression.Parse("jugador.en_la_cima").Evaluate(check).AsBool("prueba"))
                    {
                        Play(session, System.Math.Min(finalAge, year + 2));
                        break;
                    }
                }

                seen.UnionWith(session.World.EventHistory.Skip(before).Select(r => r.EventId));
            }

            return seen;
        }

        /// <summary>
        /// Vidas de personas de 30 años con dos hijos chicos que se separan de su pareja,
        /// hasta los 50, para los caminos poco comunes de la pensión de alimentos y el
        /// cuidado personal (el juicio por los niños, el registro de deudores): en una vida
        /// al azar, pocas personas se separan con hijos chicos. En la mitad, los niños se van
        /// a vivir con su otro papá o mamá.
        /// </summary>
        public static HashSet<string> SeparatedParentEvents(ContentDatabase content, int lives, int startAge = 30, int finalAge = 50)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < lives; i++)
            {
                seen.UnionWith(SeparatedParentEventsFrom(content, 1100 + (ulong)i, startAge, finalAge));
            }

            return seen;
        }

        /// <summary>Una de esas vidas (vacía si no nace en Chile o muere antes).</summary>
        public static HashSet<string> SeparatedParentEventsFrom(ContentDatabase content, ulong lifeSeed, int startAge = 30, int finalAge = 50)
        {
            var session = new LifeSession(content, lifeSeed);
            Play(session, startAge);
            // Si quedó un evento por decidir (por ejemplo, con la pareja), se decide antes de
            // separarse: si no, su opción hablaría de una pareja que ya no está.
            var decider = new AutoDecider(content);
            var chooser = new GameRandom(session.World.Seed).Fork("prueba.separados");
            while (session.World.Player.IsAlive && session.Pending != null)
            {
                session.Choose(decider.Choose(session.World, session.Pending, chooser));
            }

            if (!session.World.Player.IsAlive || session.World.Player.Residence?.CountryId != "pais.chile")
            {
                return new HashSet<string>();
            }

            var effects = new List<string>();
            if (session.World.Player.PartnerId == null)
            {
                effects.Add("jugador.formar_pareja()");
            }

            effects.AddRange(new[] { "jugador.tener_hijo()", "jugador.tener_hijo()", "jugador.terminar_pareja()" });
            if (lifeSeed % 2 == 1)
            {
                // La mitad deja a los niños con su otro papá o mamá: así también hay vidas que pagan la pensión.
                effects.Add("jugador.hijos_con_el_otro()");
            }

            Apply(session, effects);
            int before = session.World.EventHistory.Count;
            Play(session, finalAge);
            return new HashSet<string>(session.World.EventHistory.Skip(before).Select(r => r.EventId));
        }

        /// <summary>
        /// Vidas en Chile cuyos papás tienen ahorros cuando el jugador cumple 40, hasta los
        /// 75, para la posesión efectiva de una herencia: en vidas al azar los papás (que no
        /// llevan sus cuentas) casi nunca dejan dinero.
        /// </summary>
        public static HashSet<string> InheritanceEvents(ContentDatabase content, int lives, int startAge = 40, int finalAge = 75)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 1300 + (ulong)i);
                Play(session, startAge);
                Character player = session.World.Player;
                if (!player.IsAlive || player.Residence?.CountryId != "pais.chile")
                {
                    continue;
                }

                foreach (int? parentId in new[] { player.MotherId, player.FatherId })
                {
                    if (parentId.HasValue && session.World.TryGetCharacter(parentId.Value, out Character? parent) && parent.IsAlive)
                    {
                        parent.Money = 20000000;
                    }
                }

                int before = session.World.EventHistory.Count;
                Play(session, finalAge);
                seen.UnionWith(session.World.EventHistory.Skip(before).Select(r => r.EventId));
            }

            return seen;
        }

        private static void Apply(LifeSession session, IEnumerable<string> effects)
        {
            var scope = new EventScope(session.World, session.Content, session.World.Player, null, readOnly: false);
            foreach (string effect in effects)
            {
                Effect.Parse(effect).Apply(scope);
            }
        }

        /// <summary>
        /// Vidas que viven con un amigo (rediseño R6d): a los 26 arriendan un departamento y le
        /// proponen a una amistad vivir juntos, con o sin permiso del dueño, para los caminos que
        /// salen de compartir el arriendo (que no le alcance, el préstamo, que el dueño se entere).
        /// En una vida al azar casi nadie lo pide.
        /// </summary>
        public static HashSet<string> RoommateEvents(ContentDatabase content, int lives, int startAge = 26, int finalAge = 40)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < lives; i++)
            {
                var session = new LifeSession(content, 900 + (ulong)i);
                Play(session, startAge);
                if (!session.World.Player.IsAlive)
                {
                    continue;
                }

                World world = session.World;
                var scope = new EventScope(world, content, world.Player, null, readOnly: false);
                var friendsBefore = RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend).Select(c => c.Id).ToList();
                foreach (string effect in new[] { "jugador.dejar_de_compartir()", "jugador.marcar('vive_independiente')", "jugador.arrendar('vivienda.departamento')", "jugador.conocer('amistad')" })
                {
                    Effect.Parse(effect).Apply(scope);
                }

                int friend = RelationshipRules.WithBond(world, world.Player, RelationshipBond.Friend).Select(c => c.Id).Except(friendsBefore).First();
                int before = world.EventHistory.Count;
                if (session.Request("evento.futuro.vivir_juntos", friend))
                {
                    // La mitad con permiso del dueño y la otra mitad sin.
                    session.Choose(session.Pending!.AvailableOptions[i % 2]);
                }

                Play(session, finalAge);
                seen.UnionWith(world.EventHistory.Skip(before).Select(r => r.EventId));
            }

            return seen;
        }

        private static void Play(LifeSession session, int age)
        {
            var decider = new AutoDecider(session.Content);
            var chooser = new GameRandom(session.World.Seed).Fork("prueba.areas");
            int target = age * GameDate.WeeksPerYear;
            // También se contesta lo que quedó pendiente al llegar a la edad: si no, un evento
            // de antes (por ejemplo, una oferta de ascenso) se contestaría después de que la
            // prueba cambie a mano la vida (por ejemplo, ascendiendo), y ya no calzaría.
            while (session.World.Player.IsAlive && (session.World.Player.AgeInWeeks(session.World.Date) < target || session.Pending != null))
            {
                PendingEvent? pending = session.Pending;
                if (pending != null)
                {
                    session.Choose(decider.Choose(session.World, pending, chooser));
                    continue;
                }

                session.Advance(target - session.World.Player.AgeInWeeks(session.World.Date));
            }
        }
    }
}
