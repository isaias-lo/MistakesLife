#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Presentation;
using UnityEngine;

namespace MistakesLife.Unity
{
    /// <summary>
    /// Arma el lugar de una sección que se toca (D5c, boceto BEstudios y BTrabajo): tu sala del
    /// colegio o tu aula de la u con tu curso y tu profe, o tu pega con tu equipo y tu jefa.
    /// Cada persona va dentro de un objeto "persona.&lt;id&gt;" y cada cosa que se toca dentro de
    /// "lugar.cosa.*", para que <see cref="HomeScene.Pick"/> sepa qué se tocó. Como en la casa,
    /// cada mueble tiene su ancla (Resources/Lugares/sala.prefab o aula.prefab) y su id para
    /// cambiarlo por un modelo ("sala.pupitre", "aula.pupitre"...).
    /// </summary>
    public static class PlaceBuilder
    {
        /// <summary>Cómo empieza el nombre del objeto de una persona: "persona.1234".</summary>
        public const string PersonPrefix = "persona.";

        private const float Width = 8f;
        private const float Depth = 6f;

        /// <summary>Los puestos de la sala: 3 filas de 4, mirando a la pizarra del muro izquierdo.</summary>
        private const int Seats = 12;

        /// <summary>Tu puesto: al medio, para que se vea rodeado de tu curso.</summary>
        private const int PlayerSeat = 4;

        public static void Build(ShapeKit kit, PlaceView place)
        {
            if (place.Workplace != null)
            {
                OfficeBuilder.Build(kit, place.Workplace, place);
                return;
            }

            Classroom(kit, place);
        }

        /// <summary>Dibuja algo dentro de un objeto con nombre (su id), para poder tocarlo.</summary>
        public static GameObject Named(ShapeKit kit, string id, Action<ShapeKit> draw)
        {
            var holder = new GameObject(id);
            holder.transform.SetParent(kit.Parent, false);
            Transform previous = kit.Parent;
            kit.Parent = holder.transform;
            try
            {
                draw(kit);
            }
            finally
            {
                kit.Parent = previous;
            }

            return holder;
        }

        /// <summary>Dónde va cada cosa de la sala si nadie movió las anclas.</summary>
        public static IReadOnlyDictionary<string, Vector3> DefaultAnchors()
        {
            var anchors = new Dictionary<string, Vector3>
            {
                ["ancla.ventana"] = new Vector3(1.0f, 0f, Depth / 2f),
                ["ancla.pizarra"] = new Vector3((-Width / 2f) + 0.03f, 0f, 0.2f),
                ["ancla.escritorio_profe"] = new Vector3(-2.9f, 0f, -1.7f),
                ["ancla.profe"] = new Vector3(-3.2f, 0f, 1.0f),
            };

            float[] columns = { -1.4f, 0.4f, 2.2f };
            float[] rows = { 2.0f, 0.7f, -0.6f, -1.9f };
            int seat = 1;
            foreach (float z in rows)
            {
                foreach (float x in columns)
                {
                    anchors[$"ancla.puesto_{seat}"] = new Vector3(x, 0f, z);
                    seat++;
                }
            }

            return anchors;
        }

        /// <summary>
        /// La sala (colegio) o el aula (u): piso, dos muros con ventana, la pizarra en el muro
        /// izquierdo, el escritorio y la figura de quien hace clases, y los pupitres con tu curso.
        /// En el colegio es cálida (madera, pizarra verde); en la u, más sobria (gris, pizarra blanca).
        /// </summary>
        private static void Classroom(ShapeKit kit, PlaceView place)
        {
            bool university = place.Id == "lugar.sala_u";
            string name = university ? "aula" : "sala";
            Anchors anchors = Anchors.Create(kit.Parent, name, DefaultAnchors());

            Color floor = university ? new Color(0.60f, 0.61f, 0.63f) : new Color(0.70f, 0.55f, 0.38f);
            Color wall = university ? new Color(0.88f, 0.88f, 0.86f) : new Color(0.93f, 0.89f, 0.80f);
            Color desk = university ? new Color(0.86f, 0.85f, 0.82f) : new Color(0.79f, 0.65f, 0.48f);
            Color chair = university ? new Color(0.26f, 0.28f, 0.32f) : new Color(0.36f, 0.42f, 0.55f);
            Color board = university ? new Color(0.96f, 0.96f, 0.94f) : new Color(0.20f, 0.36f, 0.28f);

            kit.Box("Piso", new Vector3(0f, -0.05f, 0f), new Vector3(Width, 0.1f, Depth), floor);
            kit.Box("Muro del fondo", new Vector3(0f, 1.4f, (Depth / 2f) + 0.05f), new Vector3(Width, 2.8f, 0.1f), wall);
            kit.Box("Muro lateral", new Vector3((-Width / 2f) - 0.05f, 1.4f, 0f), new Vector3(0.1f, 2.8f, Depth + 0.2f), wall * 0.92f);
            Models.Place(kit, anchors["ancla.ventana"], name + ".ventana", k =>
                k.Box("Ventana", new Vector3(0f, 1.7f, -0.01f), new Vector3(3.6f, 1.0f, 0.04f), new Color(0.62f, 0.80f, 0.92f)));

            // La pizarra se toca (si viene una prueba, ahí está escrita).
            Models.Place(kit, anchors["ancla.pizarra"], PlaceView.Board, k =>
            {
                k.Box("Marco de la pizarra", new Vector3(0.02f, 1.55f, 0f), new Vector3(0.05f, 1.25f, 3.1f), Palette.DarkWood);
                k.Box("Pizarra", new Vector3(0.05f, 1.55f, 0f), new Vector3(0.03f, 1.1f, 2.9f), board);
            }, name + ".pizarra");

            Models.Place(kit, anchors["ancla.escritorio_profe"], name + ".escritorio_profe", k =>
            {
                k.Box("Cubierta", new Vector3(0f, 0.76f, 0f), new Vector3(1.4f, 0.07f, 0.75f), Palette.Wood);
                k.Box("Frente", new Vector3(0.62f, 0.38f, 0f), new Vector3(0.06f, 0.72f, 0.7f), Palette.DarkWood);
            });

            if (place.Lead != null)
            {
                PlacePersonView teacher = place.Lead;
                Vector3 feet = anchors["ancla.profe"].localPosition;
                Named(kit, PersonPrefix + teacher.Id, k => RoomBuilder.Person(k, teacher.Figure, feet, 0));
            }

            bool notebook = place.Things.Any(t => t.Id == PlaceView.Notebook);
            int next = 0;
            for (int seat = 1; seat <= Seats; seat++)
            {
                Transform anchor = anchors[$"ancla.puesto_{seat}"];
                Models.Place(kit, anchor, name + ".pupitre", k =>
                {
                    k.Box("Cubierta del pupitre", new Vector3(0f, 0.72f, 0f), new Vector3(0.85f, 0.05f, 0.6f), desk);
                    k.Box("Pata del pupitre", new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.7f, 0.5f), chair);
                    k.Box("Silla", new Vector3(0.6f, 0.42f, 0f), new Vector3(0.42f, 0.05f, 0.42f), chair);
                    k.Box("Respaldo", new Vector3(0.82f, 0.68f, 0f), new Vector3(0.05f, 0.5f, 0.42f), chair);
                });

                // Cada uno de pie detrás de su pupitre, mirando a la pizarra (la cara va hacia la cámara).
                Vector3 person = anchor.localPosition + new Vector3(0.6f, 0f, 0f);
                if (seat == PlayerSeat + 1)
                {
                    RoomBuilder.Person(kit, place.Player, person, 0, markerSize: 0f);
                    if (notebook)
                    {
                        Vector3 top = anchor.localPosition + new Vector3(-0.1f, 0.77f, 0f);
                        Named(kit, PlaceView.Notebook, k =>
                            k.Box("Libreta", top, new Vector3(0.3f, 0.04f, 0.22f), new Color(0.17f, 0.29f, 0.48f)));
                    }

                    continue;
                }

                if (next < place.People.Count)
                {
                    PlacePersonView mate = place.People[next];
                    int index = next + 1;
                    Named(kit, PersonPrefix + mate.Id, k => RoomBuilder.Person(k, mate.Figure, person, index));
                    next++;
                }
            }
        }
    }
}
