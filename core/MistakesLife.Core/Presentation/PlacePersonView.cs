namespace MistakesLife.Core.Presentation
{
    /// <summary>
    /// Una persona de tu sala, tu aula o tu pega, lista para dibujarla en su puesto y tocarla
    /// (D5c, boceto BEstudios): su figura, el nombre que se ve sobre ella y qué es tuyo.
    /// Al tocarla se abre lo que pueden hacer juntos con <see cref="LifePresenter.PersonCard"/>.
    /// </summary>
    public sealed class PlacePersonView
    {
        public PlacePersonView(HomeMemberView figure, string tag, string role, bool lead)
        {
            Figure = figure;
            Tag = tag;
            Role = role;
            Lead = lead;
        }

        /// <summary>La figura (id, edad, sexo y ánimo de la persona).</summary>
        public HomeMemberView Figure { get; }

        /// <summary>El id del personaje.</summary>
        public int Id => Figure.Id;

        /// <summary>El nombre corto que va sobre su cabeza ("Javiera", "Profe Marta", "Carolina · jefa").</summary>
        public string Tag { get; }

        /// <summary>Qué es tuyo, para la hoja al tocarla ("Compañera de curso", "Tu jefa", "Amistad").</summary>
        public string Role { get; }

        /// <summary>Si es quien manda en el lugar: la profe o el profe jefe, o tu jefa o jefe (va al frente).</summary>
        public bool Lead { get; }
    }
}
