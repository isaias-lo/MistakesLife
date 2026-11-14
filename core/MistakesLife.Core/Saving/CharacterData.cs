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

        public int? MotherId { get; set; }

        public int? FatherId { get; set; }

        public int? SecondParentId { get; set; }

        public bool Adopted { get; set; }

        public int? PartnerId { get; set; }

        public int? SpouseId { get; set; }

        public string? UnionTypeId { get; set; }

        public string? CountryId { get; set; }

        public string? CityId { get; set; }

        public string? CommuneId { get; set; }

        public int HouseholdLevel { get; set; }

        public decimal Money { get; set; }

        public JobData? Job { get; set; }

        /// <summary>Su búsqueda de trabajo, o null (desde la versión 9).</summary>
        public JobSearchData? JobSearch { get; set; }

        /// <summary>Su celular, o null (desde la versión 12).</summary>
        public PhoneData? Phone { get; set; }

        /// <summary>Su cuenta de correo, o null (desde la versión 12).</summary>
        public MailData? Mail { get; set; }

        /// <summary>Sus conversaciones de Mensajito, o null (desde la versión 15).</summary>
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

        /// <summary>Contrato de arriendo, o null si no arrienda.</summary>
        public LeaseData? Lease { get; set; }

        /// <summary>La compra de comida que eligió, o null si no ha elegido.</summary>
        public string? GroceryBasketId { get; set; }

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

        public List<string> Degrees { get; set; } = new List<string>();

        public bool IsAlive { get; set; } = true;

        public int? DeathWeek { get; set; }

        public string? DeathCauseId { get; set; }
    }
}
