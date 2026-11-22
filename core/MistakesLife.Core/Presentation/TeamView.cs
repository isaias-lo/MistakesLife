using System.Collections.Generic;

namespace MistakesLife.Core.Presentation
{
    /// <summary>Tu equipo del trabajo, listo para mostrar: la jefatura, los colegas con nombre y lo que puedes decidir tú.</summary>
    public sealed class TeamView
    {
        public TeamView(ClassmateView? boss, IReadOnlyList<ClassmateView> colleagues, IReadOnlyList<ClassmateView> left)
            : this(boss, colleagues, left, new ActionView[0], null)
        {
        }

        public TeamView(ClassmateView? boss, IReadOnlyList<ClassmateView> colleagues, IReadOnlyList<ClassmateView> left, IReadOnlyList<ActionView> actions, string? status)
            : this(boss, colleagues, left, actions, status, null, null, null, new WorkTaskView[0])
        {
        }

        public TeamView(ClassmateView? boss, IReadOnlyList<ClassmateView> colleagues, IReadOnlyList<ClassmateView> left, IReadOnlyList<ActionView> actions, string? status, int? bossView, string? bossTitle, string? bossPhrase, IReadOnlyList<WorkTaskView> tasks)
        {
            BossTitle = bossTitle;
            BossView = bossView;
            BossPhrase = bossPhrase;
            Tasks = tasks;
            Actions = actions;
            Status = status;
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

        /// <summary>Lo que puedes decidir tú en la pega ahora (pedir aumento, vacaciones, renunciar).</summary>
        public IReadOnlyList<ActionView> Actions { get; }

        /// <summary>
        /// Una línea sobre tu contrato ("12 días hábiles de vacaciones ganados" o "Te vas en 3
        /// semanas"), o null si no trabajas con contrato.
        /// </summary>
        public string? Status { get; }

        /// <summary>Cómo te ve tu jefa, de 0 a 100 (D5b); null si no tienes un cargo.</summary>
        public int? BossView { get; }

        /// <summary>El nombre de la barra ("Cómo te ve tu jefa"); null sin cargo.</summary>
        public string? BossTitle { get; }

        /// <summary>La frase que explica la barra ("Confía en ti para lo difícil"); null sin cargo.</summary>
        public string? BossPhrase { get; }

        /// <summary>Las tareas de este mes, por plazo (D5b); vacía si tu pega no trae tareas.</summary>
        public IReadOnlyList<WorkTaskView> Tasks { get; }
    }
}
