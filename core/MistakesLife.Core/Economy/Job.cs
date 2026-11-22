using System;

namespace MistakesLife.Core.Economy
{
    /// <summary>
    /// El trabajo de un personaje: tipo y sueldo bruto mensual. El sueldo se
    /// guarda en dinero de los paquetes y se lleva a precios del último
    /// reajuste: entre reajustes, la inflación se come parte del sueldo.
    /// Si es un trabajo de su carrera, también tiene área, cargo y desempeño.
    /// </summary>
    public sealed class Job
    {
        /// <summary>Desempeño con que se empieza en un trabajo nuevo.</summary>
        public const int StartingPerformance = 50;

        private int _performance = StartingPerformance;

        public Job(JobKind kind, decimal baseSalary, double priceIndex)
            : this(kind, baseSalary, priceIndex, null)
        {
        }

        public Job(JobKind kind, decimal baseSalary, double priceIndex, CareerPosition? position)
        {
            if (baseSalary < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSalary), "El sueldo no puede ser negativo.");
            }

            Kind = kind;
            BaseSalary = baseSalary;
            AdjustedPriceIndex = priceIndex;
            Position = position;
        }

        /// <summary>Área y cargo en su carrera, o null si es un trabajo fuera de una carrera.</summary>
        public CareerPosition? Position { get; }

        /// <summary>
        /// Número de la empresa donde trabaja (ver <see cref="Worlds.World.CompanyById"/>), o
        /// null si el trabajo no es en una empresa de su carrera.
        /// </summary>
        public int? CompanyId { get; internal set; }

        /// <summary>Meses en el cargo actual (la experiencia que pide un ascenso).</summary>
        public int MonthsInPosition { get; internal set; }

        /// <summary>
        /// Meses con este empleador (la antigüedad): sigue al ascender o bajar de cargo en la
        /// misma empresa y vuelve a cero en un trabajo nuevo. Con ella se calcula una
        /// indemnización por años de servicio.
        /// </summary>
        public int MonthsInJob { get; internal set; }

        /// <summary>
        /// Qué tan bien trabaja, de 0 a 100. Lo cambian las decisiones en los eventos y,
        /// poco a poco, su responsabilidad. Un ascenso pide un mínimo.
        /// </summary>
        public int Performance
        {
            get => _performance;
            set => _performance = Math.Max(0, Math.Min(100, value));
        }

        /// <summary>
        /// En un oficio con metas (ver <see cref="Work.OccupationRules"/>): cuánto cumplió de su
        /// meta el último mes, en porcentaje (100 = justo la meta). 0 si no tiene meta.
        /// </summary>
        public int GoalPercent { get; internal set; }

        /// <summary>
        /// Meses seguidos bajo la meta (vuelve a cero el mes que la cumple). Los eventos lo
        /// pueden volver a cero (después de un llamado de atención se empieza a contar de nuevo).
        /// </summary>
        public int MonthsUnderGoal { get; set; }

        /// <summary>Meses seguidos cumpliendo la meta (vuelve a cero el mes que no la cumple).</summary>
        public int MonthsOnGoal { get; internal set; }

        /// <summary>
        /// Llamados de atención y amonestaciones que lleva en este trabajo (los suman los
        /// eventos: 1 = llamado de palabra, 2 = amonestación por escrito). No se borran.
        /// </summary>
        public int Warnings { get; set; }

        /// <summary>
        /// Lo variable del último mes que entra en la liquidación (comisión o trato), en la
        /// moneda de hoy: es imponible y suma al sueldo bruto.
        /// </summary>
        public decimal VariablePay { get; internal set; }

        /// <summary>
        /// Las propinas del último mes, en la moneda de hoy: las pagan los clientes, no son
        /// sueldo (no son imponibles ni entran en la liquidación) y llegan directo a su dinero.
        /// </summary>
        public decimal Tips { get; internal set; }

        /// <summary>
        /// El desafío que está viviendo este mes (la marca que gana si lo logra, por ejemplo
        /// "desafio_vendedor_encargado"), o null. Se decide al cerrar el mes: ver
        /// <see cref="Work.OccupationRules.ChallengeGoal"/>.
        /// </summary>
        public string? Challenge { get; set; }

        /// <summary>
        /// Días de vacaciones legales ganados y no tomados (ver <see cref="Work.VacationRules"/>),
        /// en la unidad del país (hábiles en Chile, corridos en Argentina). Solo con contrato.
        /// </summary>
        public double VacationDays { get; internal set; }

        /// <summary>
        /// Si renunció con aviso: la semana (contada desde el inicio del calendario, como
        /// <see cref="Time.GameDate.TotalWeeks"/>) de su último día. Null si no ha renunciado.
        /// </summary>
        public int? LeavingWeek { get; internal set; }

        /// <summary>
        /// Las tareas de este mes (D5b, ver <see cref="Work.WorkTaskRules"/>): dos o tres de su
        /// carrera, con su plazo. Se renuevan al empezar cada mes. Vacía si su carrera no trae tareas.
        /// </summary>
        public System.Collections.Generic.List<Work.WorkTask> Tasks { get; } = new System.Collections.Generic.List<Work.WorkTask>();

        public JobKind Kind { get; }

        /// <summary>Sueldo bruto mensual en dinero de los paquetes.</summary>
        public decimal BaseSalary { get; internal set; }

        /// <summary>Índice de precios del último reajuste (o de cuando empezó).</summary>
        public double AdjustedPriceIndex { get; private set; }

        /// <summary>Meses desde el último reajuste.</summary>
        public int MonthsSinceAdjustment { get; private set; }

        /// <summary>Sueldo bruto mensual de hoy, en la moneda del país.</summary>
        public decimal Salary => Math.Round(BaseSalary * (decimal)AdjustedPriceIndex, MidpointRounding.AwayFromZero);

        /// <summary>Cierra la meta de un mes (ver <see cref="Work.OccupationRules.CloseMonth"/>).</summary>
        internal void CloseGoal(int percent, decimal variablePay, decimal tips)
        {
            GoalPercent = percent;
            bool met = percent >= Work.OccupationRules.Goal;
            MonthsUnderGoal = met ? 0 : MonthsUnderGoal + 1;
            MonthsOnGoal = met ? MonthsOnGoal + 1 : 0;
            if (MonthsOnGoal >= Work.OccupationRules.MonthsToClearWarning && Warnings > 0)
            {
                // Medio año cumpliendo deja atrás la última amonestación.
                Warnings--;
                MonthsOnGoal = 0;
            }

            VariablePay = variablePay;
            Tips = tips;
        }

        /// <summary>Un trabajo que no es un oficio no tiene meta ni nada variable.</summary>
        internal void ClearGoal()
        {
            GoalPercent = 0;
            MonthsUnderGoal = 0;
            MonthsOnGoal = 0;
            VariablePay = 0;
            Tips = 0;
        }

        /// <summary>Repone el estado del reajuste al cargar una partida.</summary>
        internal void Restore(double adjustedPriceIndex, int monthsSinceAdjustment)
        {
            AdjustedPriceIndex = adjustedPriceIndex;
            MonthsSinceAdjustment = monthsSinceAdjustment;
        }

        /// <summary>Pasa un mes; si ya tocaba, el sueldo se reajusta a los precios de hoy.</summary>
        internal void AdvanceMonth(double priceIndex, int adjustmentMonths)
        {
            MonthsInJob++;
            MonthsSinceAdjustment++;
            if (MonthsSinceAdjustment >= adjustmentMonths)
            {
                AdjustedPriceIndex = priceIndex;
                MonthsSinceAdjustment = 0;
            }
        }
    }
}
