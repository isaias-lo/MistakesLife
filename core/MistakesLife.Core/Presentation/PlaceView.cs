using System.Collections.Generic;
using System.Linq;

namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// El lugar de una sección, listo para dibujar y tocar (D5c, "cada sección es un lugar que
    /// se toca", docs/diseno-juego.md): tu sala del colegio, tu aula de la u o tu pega, con la
    /// gente real que está ahí y las cosas que se pueden tocar. Lo da <see cref="LifePresenter.Place"/>.
    /// </summary>
    public sealed class PlaceView
    {
        /// <summary>Tu libreta de notas.</summary>
        public const string Notebook = "lugar.cosa.libreta";

        /// <summary>La pizarra, con la prueba que viene.</summary>
        public const string Board = "lugar.cosa.pizarra";

        /// <summary>Tu agenda de la pega, con las tareas del mes.</summary>
        public const string Agenda = "lugar.cosa.agenda";

        public PlaceView(string id, string title, string hint, HomeMemberView player, PlacePersonView? lead, IReadOnlyList<PlacePersonView> people, IReadOnlyList<PlaceThingView> things, IReadOnlyList<ActionView> leadActions, WorkplaceView? workplace)
        {
            Id = id;
            Title = title;
            Hint = hint;
            Player = player;
            Lead = lead;
            People = people;
            Things = things;
            LeadActions = leadActions;
            Workplace = workplace;
        }

        /// <summary>Qué lugar es, con el mismo id que usan los momentos: "lugar.sala", "lugar.sala_u" o "lugar.trabajo".</summary>
        public string Id { get; }

        /// <summary>El título de arriba ("Tu sala · 8º básico B").</summary>
        public string Title { get; }

        /// <summary>La ayuda sobre la escena ("Toca a alguien o algo de tu sala").</summary>
        public string Hint { get; }

        /// <summary>El jugador, para dibujar su figura en su puesto.</summary>
        public HomeMemberView Player { get; }

        /// <summary>La profe o el profe jefe, o tu jefa o jefe; null si no hay.</summary>
        public PlacePersonView? Lead { get; }

        /// <summary>Tus compañeros o colegas, del que más quieres al que menos.</summary>
        public IReadOnlyList<PlacePersonView> People { get; }

        /// <summary>Lo que se puede tocar además de la gente.</summary>
        public IReadOnlyList<PlaceThingView> Things { get; }

        /// <summary>Lo que le puedes pedir a quien manda (aumento, vacaciones, renuncia); vacío en el colegio y la u.</summary>
        public IReadOnlyList<ActionView> LeadActions { get; }

        /// <summary>Tu lugar de trabajo (qué tipo de oficina se dibuja); null en el colegio y la u.</summary>
        public WorkplaceView? Workplace { get; }

        /// <summary>Si es el colegio o la u (se dibuja una sala); si no, es la pega.</summary>
        public bool IsClassroom => Workplace == null;

        /// <summary>Resumen corto para saber si hay que volver a dibujar el lugar.</summary>
        public string Signature =>
            string.Join("|", new[] { Id, Workplace?.Signature ?? "-", Lead?.Id.ToString() ?? "-" }
                .Concat(People.Select(p => p.Id + ":" + p.Figure.Mood))
                .Concat(Things.Select(t => t.Id)));
    }
}
