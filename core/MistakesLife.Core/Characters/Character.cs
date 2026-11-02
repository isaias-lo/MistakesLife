using System;
using System.Collections.Generic;
using MistakesLife.Core.Time;

namespace MistakesLife.Core.Characters
{
    /// <summary>
    /// Un personaje del mundo: el jugador o cualquier otra persona.
    /// </summary>
    public sealed class Character
    {
        public Character(int id, string firstName, string lastName, Sex sex, GameDate birthDate, Personality personality, Stats stats)
        {
            Id = id;
            FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
            Gender = sex == Sex.Female ? Gender.Female : Gender.Male;
            GenderIdentity = Gender;
            LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
            Sex = sex;
            BirthDate = birthDate;
            Personality = personality ?? throw new ArgumentNullException(nameof(personality));
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        /// <summary>Número único dentro del mundo. Nunca se reutiliza.</summary>
        public int Id { get; }

        public string FirstName { get; private set; }

        /// <summary>Cambia el nombre de pila (por ejemplo, al cambiar el nombre registral).</summary>
        public void ChangeFirstName(string firstName) => FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));

        public string LastName { get; }

        /// <summary>Sexo asignado al nacer. Decide lo biológico (por ejemplo, el embarazo).</summary>
        public Sex Sex { get; }

        /// <summary>Género con que vive y figura en su registro. Al nacer, el que corresponde a su sexo.</summary>
        public Gender Gender { get; set; }

        /// <summary>
        /// Género que siente (oculto). Si no coincide con <see cref="Gender"/>, el
        /// personaje puede descubrirlo y vivir ese camino con los eventos.
        /// </summary>
        public Gender GenderIdentity { get; set; }

        /// <summary>Orientación sexual (oculta).</summary>
        public Orientation Orientation { get; set; } = Orientation.Heterosexual;

        public GameDate BirthDate { get; }

        public Personality Personality { get; }

        public Stats Stats { get; }

        /// <summary>Id de la madre, si existe en el mundo.</summary>
        public int? MotherId { get; set; }

        /// <summary>Id del padre, si existe en el mundo.</summary>
        public int? FatherId { get; set; }

        /// <summary>
        /// La otra madre o el otro padre, en familias de dos madres o dos padres
        /// (por ahora, por adopción). Si no hay, null.
        /// </summary>
        public int? SecondParentId { get; set; }

        /// <summary>Si llegó a su familia por adopción.</summary>
        public bool Adopted { get; set; }

        /// <summary>Si <paramref name="id"/> es su madre o su padre (biológico o por adopción).</summary>
        public bool IsChildOf(int id) => MotherId == id || FatherId == id || SecondParentId == id;

        /// <summary>
        /// Id de su pareja actual, o null si no tiene. Siempre va en ambos sentidos:
        /// si A tiene pareja B, B tiene pareja A.
        /// </summary>
        public int? PartnerId { get; set; }

        /// <summary>Dónde vive: país, ciudad y comuna (ids de los paquetes).</summary>
        public Residence? Residence { get; set; }

        /// <summary>
        /// Nivel socioeconómico del hogar donde creció, de 0 a 100.
        /// Influye en colegio, oportunidades y eventos.
        /// </summary>
        public int HouseholdLevel { get; set; } = 50;

        /// <summary>
        /// Dinero disponible (su saldo), en la moneda del país donde vive. Puede
        /// quedar negativo: eso es estar en deuda.
        /// </summary>
        public decimal Money { get; set; }

        /// <summary>Su trabajo actual (tipo y sueldo), o null si no trabaja.</summary>
        public Economy.Job? Job { get; set; }

        /// <summary>Su última liquidación de sueldo, o null si nunca ha recibido sueldo.</summary>
        public Economy.Payslip? LastPayslip { get; set; }

        private readonly List<Economy.AnnualIncome> _incomes = new List<Economy.AnnualIncome>();

        /// <summary>Lo ganado en un año (para la declaración de impuestos), o null si no ganó nada.</summary>
        public Economy.AnnualIncome? IncomeOf(int year) => _incomes.Find(i => i.Year == year);

        /// <summary>Suma una liquidación a lo ganado en ese año.</summary>
        public void AddIncome(int year, Economy.Payslip payslip)
        {
            Economy.AnnualIncome? income = IncomeOf(year);
            if (income == null)
            {
                income = new Economy.AnnualIncome(year);
                _incomes.Add(income);
            }

            income.Add(payslip);
        }

        /// <summary>Olvida los ingresos de ese año y de los anteriores (ya se declararon).</summary>
        public void ForgetIncomeUntil(int year) => _incomes.RemoveAll(i => i.Year <= year);

        /// <summary>Lo que pagó en gastos el último mes, línea por línea.</summary>
        public IReadOnlyList<Economy.PayslipLine> LastExpenses { get; set; } = Array.Empty<Economy.PayslipLine>();

        /// <summary>Meses seguidos que terminó con el dinero en negativo (estrés financiero).</summary>
        public int MonthsInRed { get; set; }

        /// <summary>Declaración anual calculada que el jugador todavía no acepta, o null.</summary>
        public Economy.TaxReturn? PendingTaxReturn { get; set; }

        /// <summary>
        /// Lo ahorrado en su cuenta de pensión (por ejemplo en la AFP), en dinero
        /// del inicio del mundo: así la inflación no lo achica.
        /// </summary>
        public decimal PensionFund { get; set; }

        /// <summary>Su pensión mensual, en dinero del inicio del mundo, o null si no se ha jubilado.</summary>
        public decimal? Pension { get; set; }

        /// <summary>Si pidió la pensión del Estado para personas mayores (en Chile, la PGU).</summary>
        public bool ReceivesUniversalPension { get; set; }

        /// <summary>Lo que recibió de pensiones el último mes, a precios de ese mes.</summary>
        public decimal LastPensionPayment { get; set; }

        /// <summary>Etapa educativa actual (id del paquete del país), o null si no estudia.</summary>
        public string? EducationStageId { get; set; }

        private readonly SortedSet<string> _flags = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Marcas simples sobre el personaje, como "postulando" o "fumador".
        /// Las ponen y quitan los efectos de los eventos.
        /// </summary>
        public IReadOnlyCollection<string> Flags => _flags;

        public bool HasFlag(string flag) => _flags.Contains(flag);

        public void SetFlag(string flag) => _flags.Add(flag);

        public void ClearFlag(string flag) => _flags.Remove(flag);

        /// <summary>La carrera que estudia ahora, o null si no estudia.</summary>
        public CareerStudies? Studies { get; set; }

        private readonly List<string> _degrees = new List<string>();

        /// <summary>Niveles de carrera en que se tituló (ids de nivel), en orden.</summary>
        public IReadOnlyList<string> Degrees => _degrees;

        /// <summary>Termina los estudios actuales con su título.</summary>
        public void Graduate()
        {
            if (Studies == null)
            {
                throw new InvalidOperationException($"{this} no está estudiando.");
            }

            _degrees.Add(Studies.LevelId);
            Studies = null;
        }

        public bool IsAlive { get; private set; } = true;

        public GameDate? DeathDate { get; private set; }

        /// <summary>
        /// Por qué murió (id de causa, por ejemplo "causa.natural"), o null si sigue vivo.
        /// </summary>
        public string? DeathCauseId { get; private set; }

        /// <summary>Edad en semanas en la fecha indicada.</summary>
        public int AgeInWeeks(GameDate date) => Math.Max(0, date.WeeksSince(BirthDate));

        /// <summary>Edad en años cumplidos en la fecha indicada.</summary>
        public int AgeInYears(GameDate date) => AgeInWeeks(date) / GameDate.WeeksPerYear;

        public void Die(GameDate date) => Die(date, CauseNatural);

        /// <summary>Muere en esa fecha por esa causa. Si ya estaba muerto, no cambia nada.</summary>
        public void Die(GameDate date, string causeId)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            DeathDate = date;
            DeathCauseId = causeId ?? throw new ArgumentNullException(nameof(causeId));
        }

        /// <summary>Causa de muerte por edad y salud (la que usa la tabla de mortalidad).</summary>
        public const string CauseNatural = "causa.natural";

        public override string ToString() => $"{FirstName} {LastName} (#{Id})";
    }
}
