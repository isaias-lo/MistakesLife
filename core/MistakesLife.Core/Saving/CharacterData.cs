using System.Collections.Generic;

namespace MistakesLife.Core.Saving
{
    /// <summary>Un personaje guardado, con todo lo que puede cambiar durante la vida.</summary>
    internal sealed class CharacterData
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Sex { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public string GenderIdentity { get; set; } = string.Empty;

        public string Orientation { get; set; } = string.Empty;

        public int BirthWeek { get; set; }

        /// <summary>Apertura, responsabilidad, extraversión, amabilidad y neuroticismo, en ese orden.</summary>
        public List<int> Personality { get; set; } = new List<int>();

        public int Health { get; set; }

        public int Happiness { get; set; }

        public int Smarts { get; set; }

        /// <summary>Null en guardados de antes de la versión 36 (parte descansado).</summary>
        public int? Energy { get; set; }

        /// <summary>Null en guardados de antes de la versión 36 (se calcula al seguir jugando).</summary>
        public int? Looks { get; set; }

        public int? InheritedLooks { get; set; }

        public int? MotherId { get; set; }

        public int? FatherId { get; set; }

        public int? SecondParentId { get; set; }

        public bool Adopted { get; set; }

        public int? PartnerId { get; set; }

        public int? SpouseId { get; set; }

        public string? UnionTypeId { get; set; }

        public int? LivesWithId { get; set; }

        public bool SharedCustody { get; set; }

        /// <summary>El último trabajo que dejó, o null (desde la versión 16).</summary>
        public JobData? LastJob { get; set; }

        public decimal PendingInheritance { get; set; }

        public string? CountryId { get; set; }

        public string? CityId { get; set; }

        public string? CommuneId { get; set; }

        public int HouseholdLevel { get; set; }

        public decimal Money { get; set; }

        public JobData? Job { get; set; }

        /// <summary>Su búsqueda de trabajo, o null (desde la versión 9).</summary>
        public JobSearchData? JobSearch { get; set; }

        /// <summary>Su negocio con un socio, o null (desde la versión 33).</summary>
        public VentureData? Venture { get; set; }

        /// <summary>Su celular, o null (desde la versión 12).</summary>
        public PhoneData? Phone { get; set; }

        /// <summary>Su cuenta de correo, o null (desde la versión 12).</summary>
        public MailData? Mail { get; set; }

        /// <summary>Sus conversaciones de Mensajito, o null (desde la versión 17).</summary>
        public ChatInboxData? Chats { get; set; }

        /// <summary>Área del último cargo de su carrera que ya no tiene, o null.</summary>
        public string? LastAreaId { get; set; }

        /// <summary>El último cargo de su carrera que ya no tiene, o null.</summary>
        public string? LastPositionId { get; set; }

        public PayslipData? LastPayslip { get; set; }

        public List<IncomeData> Incomes { get; set; } = new List<IncomeData>();

        public List<MoneyLineData> LastExpenses { get; set; } = new List<MoneyLineData>();

        public int MonthsInRed { get; set; }

        public TaxReturnData? PendingTaxReturn { get; set; }

        public decimal PensionFund { get; set; }

        /// <summary>Meses en que cotizó para su pensión (desde la versión 2 del formato).</summary>
        public int ContributedMonths { get; set; }

        /// <summary>Sueldo promedio para la jubilación de reparto (desde la versión 7 del formato).</summary>
        public decimal PensionSalaryBase { get; set; }

        public decimal? Pension { get; set; }

        public bool ReceivesUniversalPension { get; set; }

        public decimal LastPensionPayment { get; set; }

        /// <summary>Saldo de la cuenta del seguro de desempleo (desde la versión 46 del formato).</summary>
        public decimal UnemploymentFund { get; set; }

        public int UnemploymentContributions { get; set; }

        public decimal UnemploymentSalaryBase { get; set; }

        public UnemploymentClaimData? UnemploymentClaim { get; set; }

        /// <summary>Los duelos del jugador (desde la versión 47 del formato).</summary>
        public List<GriefData>? Griefs { get; set; }

        /// <summary>Contrato de arriendo, o null si no arrienda.</summary>
        public LeaseData? Lease { get; set; }

        /// <summary>La compra de comida que eligió, o null si no ha elegido.</summary>
        public string? GroceryBasketId { get; set; }

        /// <summary>Plan de salud privado, o null si está en la previsión pública.</summary>
        public string? HealthPlanId { get; set; }

        /// <summary>Semana (total) de su última atención de salud, o null.</summary>
        public int? LastHealthCareWeek { get; set; }

        /// <summary>Servicios contratados: id del servicio → opción.</summary>
        public Dictionary<string, string> Services { get; set; } = new Dictionary<string, string>();

        public List<DebtData> Debts { get; set; } = new List<DebtData>();

        public decimal LastDebtPayment { get; set; }

        public bool Delinquent { get; set; }

        /// <summary>Lo que siente por otras personas (desde la versión 3 del formato).</summary>
        public List<RelationshipData> Relationships { get; set; } = new List<RelationshipData>();

        public string? EducationStageId { get; set; }

        public List<string> Flags { get; set; } = new List<string>();

        public StudiesData? Studies { get; set; }

        /// <summary>En qué anda alguien que nadie juega (Characters.WorkStatus), o null si no se ha mirado.</summary>
        public string? WorkStatus { get; set; }

        /// <summary>La etapa con su pareja (Characters.CoupleStage), o null: sin pareja o viven juntos (desde la versión 40).</summary>
        public string? CoupleStage { get; set; }

        /// <summary>Desde qué semana va en esa etapa, o null (desde la versión 40).</summary>
        public int? CoupleSinceWeek { get; set; }

        /// <summary>La pareja que se le murió siendo su pareja, o null (desde la versión 40).</summary>
        public int? LatePartnerId { get; set; }

        /// <summary>La gente interesante de su horario (desde la versión 41); null si nunca apareció nadie, para que el guardado no crezca.</summary>
        public List<InterestingPersonData>? InterestingPeople { get; set; }

        /// <summary>El horario (desde la versión 19).</summary>
        public RoutineData? Routine { get; set; }

        /// <summary>Las tardes libres usadas este mes (desde la versión 37); null si no ha usado ninguna.</summary>
        public FreeTimeData? FreeTime { get; set; }

        /// <summary>Las reglas propias (desde la versión 21); null si no tiene, para que el guardado no crezca.</summary>
        public List<OwnRuleData>? OwnRules { get; set; }

        /// <summary>Lo que quiere y quiso (desde la versión 21); null si nada.</summary>
        public List<WishData>? Wishes { get; set; }

        /// <summary>El curso del colegio (desde la versión 22); null si no va a un curso.</summary>
        public SchoolClassData? SchoolClass { get; set; }

        /// <summary>El equipo del trabajo (desde la versión 29); null si no trabaja con equipo.</summary>
        public WorkTeamData? Team { get; set; }

        /// <summary>Las notas del colegio (desde la versión 23); null si nunca tuvo.</summary>
        public SchoolRecordData? SchoolRecord { get; set; }

        public List<string> Degrees { get; set; } = new List<string>();

        [System.ComponentModel.DefaultValue(true)]
        public bool IsAlive { get; set; } = true;

        public int? DeathWeek { get; set; }

        public string? DeathCauseId { get; set; }
    }
}
