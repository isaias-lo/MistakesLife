using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Tu equipo del trabajo, listo para mostrar: la jefatura y los colegas con nombre.</summary>
    public sealed class TeamView
    {
        public TeamView(ClassmateView? boss, IReadOnlyList<ClassmateView> colleagues, IReadOnlyList<ClassmateView> left)
        {
            Boss = boss;
            Colleagues = colleagues;
            Left = left;
        }

        /// <summary>Tu jefe o jefa (con lo último que vivieron); null si no hay.</summary>
        public ClassmateView? Boss { get; }

        /// <summary>Los colegas, del que más quieres al que menos.</summary>
        public IReadOnlyList<ClassmateView> Colleagues { get; }

        /// <summary>Quienes dejaron el equipo este último año (se muestran en gris, con "Se fue").</summary>
        public IReadOnlyList<ClassmateView> Left { get; }
    }
}
