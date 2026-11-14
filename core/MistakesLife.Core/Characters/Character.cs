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

        /// <summary>
        /// Con quién está unido ante la ley (casado o con unión civil), o null. Sigue
        /// aunque se separen de hecho: la unión solo se termina con lo que diga la ley
        /// (un divorcio, un aviso de término). Ver <see cref="UnionTypeId"/>.
        /// </summary>
        public int? SpouseId { get; set; }

        /// <summary>Qué unión tiene ("union.chile.matrimonio"...), o null si no tiene.</summary>
        public string? UnionTypeId { get; set; }

        private readonly SortedDictionary<int, Relationship> _relationships = new SortedDictionary<int, Relationship>();

        /// <summary>
        /// Lo que siente por otras personas (cariño, confianza, respeto y rencor) y
        /// qué son (amigos, colegas, rivales, ex), ordenado por id. Solo están las
        /// personas con quienes ya pasó algo: para el resto vale lo que diga
        /// <see cref="Life.RelationshipRules.Feelings"/> según el lazo familiar.
        /// </summary>
        public IReadOnlyCollection<Relationship> Relationships => _relationships.Values;

        /// <summary>Lo que siente por esa persona, o null si todavía no pasó nada entre ellos.</summary>
        public Relationship? RelationshipWith(int otherId) =>
            _relationships.TryGetValue(otherId, out Relationship? relationship) ? relationship : null;

        /// <summary>Guarda lo que siente por otra persona (reemplaza lo anterior).</summary>
        public void SetRelationship(Relationship relationship)
        {
            if (relationship == null)
            {
                throw new ArgumentNullException(nameof(relationship));
            }

            if (relationship.OtherId == Id)
            {
                throw new InvalidOperationException($"{this} no puede tener una relación consigo mismo.");
            }

            _relationships[relationship.OtherId] = relationship;
        }

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

        private Economy.Job? _job;

        /// <summary>
        /// Su trabajo actual (tipo y sueldo), o null si no trabaja. Al dejar un trabajo de su
        /// carrera se recuerda el cargo (<see cref="LastPosition"/>): con esa experiencia
        /// vuelve a buscar.
        /// </summary>
        public Economy.Job? Job
        {
            get => _job;
            set
            {
                if (_job?.Position != null && value?.Position == null)
                {
                    LastPosition = _job.Position;
                }

                _job = value;
            }
        }

        /// <summary>
        /// El último cargo que tuvo en su carrera y ya no tiene (porque dejó ese trabajo), o
        /// null. Sirve para volver a trabajar sin partir de cero.
        /// </summary>
        public Economy.CareerPosition? LastPosition { get; internal set; }

        /// <summary>
        /// Su búsqueda de trabajo (qué busca, cómo y cómo le va), o null si no está
        /// buscando. Ver <see cref="Economy.JobSearchRules"/>.
        /// </summary>
        public Economy.JobSearch? JobSearch { get; set; }

        /// <summary>Su celular, o null si no tiene. Ver <see cref="Devices.PhoneRules"/>.</summary>
        public Devices.Phone? Phone { get; set; }

        /// <summary>Su cuenta de correo, o null si no tiene. Ver <see cref="Devices.MailSystem"/>.</summary>
        public Devices.MailAccount? Mail { get; set; }

        /// <summary>Sus conversaciones de Mensajito, o null si nunca tuvo la app. Ver <see cref="Devices.ChatSystem"/>.</summary>
        public Devices.ChatInbox? Chats { get; set; }

        /// <summary>Su última liquidación de sueldo, o null si nunca ha recibido sueldo.</summary>
        public Economy.Payslip? LastPayslip { get; set; }

        private readonly List<Economy.AnnualIncome> _incomes = new List<Economy.AnnualIncome>();

        /// <summary>Lo ganado en un año (para la declaración de impuestos), o null si no ganó nada.</summary>
        public Economy.AnnualIncome? IncomeOf(int year) => _incomes.Find(i => i.Year == year);

        /// <summary>Suma una liquidación a lo ganado en ese año.</summary>
        public void AddIncome(int year, Economy.Payslip payslip)
        {
            if (payslip.Kind == Economy.JobKind.Informal)
            {
                // Lo ganado sin contrato no queda registrado: no entra en la declaración.
                return;
            }

            Economy.AnnualIncome? income = IncomeOf(year);
            if (income == null)
            {
                income = new Economy.AnnualIncome(year);
                _incomes.Add(income);
            }

            income.Add(payslip);
        }

        /// <summary>Todos los años con ingresos que aún no se declaran (para guardar la partida).</summary>
        internal IReadOnlyList<Economy.AnnualIncome> Incomes => _incomes;

        /// <summary>Repone un año de ingresos al cargar una partida.</summary>
        internal void RestoreIncome(Economy.AnnualIncome income) => _incomes.Add(income);

        /// <summary>Olvida los ingresos de ese año y de los anteriores (ya se declararon).</summary>
        public void ForgetIncomeUntil(int year) => _incomes.RemoveAll(i => i.Year <= year);

        /// <summary>Lo que pagó en gastos el último mes, línea por línea.</summary>
        public IReadOnlyList<Economy.PayslipLine> LastExpenses { get; set; } = Array.Empty<Economy.PayslipLine>();

        /// <summary>El contrato de arriendo que firmó, o null si no arrienda (vive con su familia o en casa propia).</summary>
        public Economy.Lease? Lease { get; set; }

        /// <summary>
        /// La compra de comida que eligió el jugador (id de "economia.supermercado"), o
        /// null si todavía no la decide. Se cobra cada mes mientras vive por su cuenta.
        /// </summary>
        public string? GroceryBasketId { get; set; }

        /// <summary>
        /// Los servicios que contrató: id del servicio → opción elegida
        /// ("servicio.celular" → "servicio.celular.prepago"). Se cobran cada mes.
        /// </summary>
        public SortedDictionary<string, string> Services { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Meses seguidos que terminó con el dinero en negativo (estrés financiero).</summary>
        public int MonthsInRed { get; set; }

        /// <summary>Declaración anual calculada que el jugador todavía no acepta, o null.</summary>
        public Economy.TaxReturn? PendingTaxReturn { get; set; }

        /// <summary>
        /// Lo ahorrado en su cuenta de pensión (por ejemplo en la AFP), en dinero
        /// del inicio del mundo: así la inflación no lo achica.
        /// </summary>
        public decimal PensionFund { get; set; }

        /// <summary>
        /// Meses en que cotizó para su pensión (con contrato). Los meses sin
        /// trabajo o con trabajo informal son "lagunas" y no suman.
        /// </summary>
        public int ContributedMonths { get; set; }

        /// <summary>
        /// En los países con jubilación de reparto: el sueldo bruto promedio de sus
        /// últimos años con aportes, en dinero del inicio del mundo. Con él se calcula
        /// la pensión. Se actualiza cada mes con aporte (promedio móvil).
        /// </summary>
        public decimal PensionSalaryBase { get; set; }

        /// <summary>Su pensión mensual, en dinero del inicio del mundo, o null si no se ha jubilado.</summary>
        public decimal? Pension { get; set; }

        /// <summary>Si pidió la pensión del Estado para personas mayores (en Chile, la PGU).</summary>
        public bool ReceivesUniversalPension { get; set; }

        /// <summary>Lo que recibió de pensiones el último mes, a precios de ese mes.</summary>
        public decimal LastPensionPayment { get; set; }

        private readonly List<Economy.Debt> _debts = new List<Economy.Debt>();

        /// <summary>Sus deudas (una por tipo).</summary>
        public IReadOnlyList<Economy.Debt> Debts => _debts;

        public void AddDebt(Economy.Debt debt) => _debts.Add(debt);

        public void RemoveDebt(Economy.Debt debt) => _debts.Remove(debt);

        /// <summary>Lo que pagó en cuotas de deudas el último mes.</summary>
        public decimal LastDebtPayment { get; set; }

        /// <summary>Si está en el registro de morosos de su país (en Chile, DICOM).</summary>
        public bool Delinquent { get; set; }

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

        /// <summary>Repone un título al cargar una partida.</summary>
        internal void RestoreDegree(string levelId) => _degrees.Add(levelId);

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

        /// <summary>Repone la muerte tal como se guardó (al cargar una partida).</summary>
        internal void RestoreDeath(GameDate date, string causeId)
        {
            IsAlive = false;
            DeathDate = date;
            DeathCauseId = causeId ?? throw new ArgumentNullException(nameof(causeId));
        }

        /// <summary>Causa de muerte por edad y salud (la que usa la tabla de mortalidad).</summary>
        public const string CauseNatural = "causa.natural";

        public override string ToString() => $"{FirstName} {LastName} (#{Id})";
    }
}
