using System;
using System.Collections.Generic;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Los datos de un país, tal como se escriben en el archivo country.json
    /// de un paquete de país.
    /// </summary>
    public sealed class CountryDefinition
    {
        /// <summary>Por ejemplo "pais.chile".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Código de moneda, por ejemplo "CLP".</summary>
        [JsonProperty("moneda")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>Qué tan probable es nacer en este país frente a otros.</summary>
        [JsonProperty("peso_nacimiento")]
        public int BirthWeight { get; set; } = 1;

        /// <summary>Edad mínima y máxima de la madre al tener un hijo.</summary>
        [JsonProperty("edad_madre_min")]
        public int MotherAgeMin { get; set; } = 18;

        [JsonProperty("edad_madre_max")]
        public int MotherAgeMax { get; set; } = 42;

        /// <summary>
        /// Si es true, el hijo lleva el primer apellido del padre y luego el de la madre
        /// (como en Chile y Argentina en la práctica).
        /// </summary>
        [JsonProperty("dos_apellidos")]
        public bool TwoSurnames { get; set; } = true;

        [JsonProperty("nombres")]
        public NamePool Names { get; set; } = new NamePool();

        [JsonProperty("ciudades")]
        public List<CityDefinition> Cities { get; set; } = new List<CityDefinition>();

        /// <summary>
        /// Día en que empieza el año escolar ("04-03" = 4 de marzo). Cada año, ese día,
        /// quien va al colegio arma su horario del año. Opcional.
        /// </summary>
        [JsonProperty("inicio_ano_escolar")]
        public string? SchoolYearStart { get; set; }

        /// <summary>El día del año (desde 0) de <see cref="SchoolYearStart"/>; null si no tiene.</summary>
        [JsonIgnore]
        public int? SchoolYearStartDay { get; internal set; }

        /// <summary>
        /// Día en que terminan las clases ("13-12"). Desde ese día y hasta
        /// <see cref="SchoolYearStart"/> son las vacaciones de verano. Opcional.
        /// </summary>
        [JsonProperty("fin_ano_escolar")]
        public string? SchoolYearEnd { get; set; }

        /// <summary>El día del año (desde 0) de <see cref="SchoolYearEnd"/>; null si no tiene.</summary>
        [JsonIgnore]
        public int? SchoolYearEndDay { get; internal set; }

        /// <summary>Las vacaciones de invierno del colegio, si el país las tiene. Opcional.</summary>
        [JsonProperty("vacaciones_invierno")]
        public DayRangeDefinition? WinterBreak { get; set; }

        /// <summary>Cómo es un curso del colegio (cuántos alumnos, con cuántos tienes trato). Opcional.</summary>
        [JsonProperty("curso")]
        public SchoolClassDefinition? SchoolClass { get; set; }

        /// <summary>Cómo se ponen las notas en el colegio (de 1,0 a 7,0 en Chile). Opcional: sin ella no hay notas.</summary>
        [JsonProperty("escala_notas")]
        public GradeScaleDefinition? GradeScale { get; set; }

        [JsonProperty("etapas_educacion")]
        public List<EducationStageDefinition> EducationStages { get; set; } = new List<EducationStageDefinition>();

        /// <summary>
        /// Tabla de mortalidad: probabilidad anual de morir por edad y sexo, de
        /// menor a mayor edad. Si falta, en este país nadie muere de causas naturales.
        /// </summary>
        [JsonProperty("mortalidad")]
        public List<MortalityRateDefinition> Mortality { get; set; } = new List<MortalityRateDefinition>();

        /// <summary>
        /// Lo que se da por vivido en alguien de cierta edad que nadie jugó (ver
        /// <see cref="PriorLifeDefinition"/>). Se usa al seguir con un hijo en el legado.
        /// </summary>
        [JsonProperty("vida_previa")]
        public List<PriorLifeDefinition> PriorLife { get; set; } = new List<PriorLifeDefinition>();

        /// <summary>
        /// Cómo forman familia los parientes que nadie juega (ver <see cref="FamilyRatesDefinition"/>).
        /// Si falta, no forman pareja ni tienen hijos.
        /// </summary>
        [JsonProperty("familia_no_jugada")]
        public FamilyRatesDefinition? NonPlayedFamily { get; set; }

        /// <summary>
        /// Las leyes del país: id de ley ("ley.matrimonio_igualitario") → valor (sí/no,
        /// un número como una edad, o un texto como una modalidad). Los eventos las leen
        /// con ley('id'), así un mismo evento sirve para todos los países.
        /// </summary>
        [JsonProperty("leyes")]
        public Dictionary<string, JToken> LawsJson { get; set; } = new Dictionary<string, JToken>();

        /// <summary>El valor de una ley tal como lo trae el paquete, o false si el país no la define.</summary>
        public bool TryGetLaw(string lawId, out Value value)
        {
            value = Value.Null;
            if (!LawsJson.TryGetValue(lawId, out JToken? token))
            {
                return false;
            }

            Value? converted = LawValue(token);
            if (converted == null)
            {
                return false;
            }

            value = converted.Value;
            return true;
        }

        /// <summary>Convierte un valor JSON de ley; null si no es sí/no, número o texto.</summary>
        internal static Value? LawValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return Value.FromBool(token.Value<bool>());
                case JTokenType.Integer:
                case JTokenType.Float:
                    return Value.FromNumber(token.Value<double>());
                case JTokenType.String:
                    return Value.FromText(token.Value<string>() ?? string.Empty);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Proporciones de orientaciones e identidades al nacer (ver <see cref="DiversityDefinition"/>).
        /// Si falta, todos nacen heterosexuales y con el género de su sexo.
        /// </summary>
        [JsonProperty("diversidad")]
        public DiversityDefinition? Diversity { get; set; }

        /// <summary>
        /// Cómo funciona el dinero en el país (ver <see cref="EconomyDefinition"/>).
        /// Si falta, los precios nunca cambian.
        /// </summary>
        [JsonProperty("economia")]
        public EconomyDefinition? Economy { get; set; }

        /// <summary>
        /// Las uniones que la ley del país reconoce (por ejemplo "union.chile.matrimonio"
        /// y "union.chile.auc"), para jugador.unirse('union.x'). Cómo se termina cada una
        /// (un divorcio en el juzgado, un aviso en el Registro Civil) lo dicen sus eventos.
        /// </summary>
        [JsonProperty("uniones_civiles")]
        public List<string> CivilUnions { get; set; } = new List<string>();

        /// <summary>
        /// La variante de cada idioma que se usa en este país, por ejemplo
        /// "es" → "es-CL". Los textos de la variante (archivos lang/es-CL.json)
        /// reemplazan a los del idioma general; los que no tenga, salen del general.
        /// </summary>
        [JsonProperty("variantes_idioma")]
        public Dictionary<string, string> LanguageVariants { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>
        /// Si esa semana hay clases: está entre <see cref="SchoolYearStart"/> y
        /// <see cref="SchoolYearEnd"/> y no son las vacaciones de invierno. Si el país no
        /// tiene calendario escolar, siempre hay clases.
        /// </summary>
        public bool IsSchoolTime(Time.GameDate date)
        {
            int week = date.DayOfYear / 7;
            if (SchoolYearStartDay.HasValue && SchoolYearEndDay.HasValue)
            {
                int start = SchoolYearStartDay.Value / 7;
                int end = SchoolYearEndDay.Value / 7;
                bool inYear = start <= end ? week >= start && week < end : week >= start || week < end;
                if (!inYear)
                {
                    return false;
                }
            }

            if (WinterBreak?.FromDay != null && WinterBreak.ToDay != null)
            {
                int from = WinterBreak.FromDay.Value / 7;
                int to = WinterBreak.ToDay.Value / 7;
                bool inBreak = from <= to ? week >= from && week <= to : week >= from || week <= to;
                if (inBreak)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Si esa semana son las vacaciones de verano (entre el fin y el inicio del año escolar).</summary>
        public bool IsSummerBreak(Time.GameDate date)
        {
            if (!SchoolYearStartDay.HasValue || !SchoolYearEndDay.HasValue)
            {
                return false;
            }

            int week = date.DayOfYear / 7;
            int start = SchoolYearStartDay.Value / 7;
            int end = SchoolYearEndDay.Value / 7;
            return start <= end ? week >= end || week < start : week >= end && week < start;
        }

        /// <summary>Cuántas semanas de clases tiene un año escolar.</summary>
        public int SchoolWeeksPerYear()
        {
            int weeks = 0;
            for (int i = 0; i < Time.GameDate.WeeksPerYear; i++)
            {
                if (IsSchoolTime(new Time.GameDate(i)))
                {
                    weeks++;
                }
            }

            return weeks;
        }

        /// <summary>La etapa que corresponde a una edad, o null.</summary>
        public EducationStageDefinition? StageForAge(int ageInYears)
        {
            foreach (EducationStageDefinition stage in EducationStages)
            {
                if (ageInYears >= stage.FromAge && ageInYears < stage.ToAge)
                {
                    return stage;
                }
            }

            return null;
        }

        /// <summary>
        /// Probabilidad de morir durante un año a esa edad, según la tabla.
        /// Entre dos filas se interpola en línea recta; antes de la primera fila
        /// vale la primera y después de la última vale la última.
        /// </summary>
        public double AnnualDeathProbability(int ageInYears, Sex sex)
        {
            if (Mortality.Count == 0)
            {
                return 0;
            }

            MortalityRateDefinition first = Mortality[0];
            if (ageInYears <= first.Age)
            {
                return Rate(first, sex);
            }

            for (int i = 1; i < Mortality.Count; i++)
            {
                MortalityRateDefinition next = Mortality[i];
                if (ageInYears <= next.Age)
                {
                    MortalityRateDefinition previous = Mortality[i - 1];
                    double share = (double)(ageInYears - previous.Age) / (next.Age - previous.Age);
                    return Rate(previous, sex) + ((Rate(next, sex) - Rate(previous, sex)) * share);
                }
            }

            return Rate(Mortality[Mortality.Count - 1], sex);
        }

        private static double Rate(MortalityRateDefinition row, Sex sex) => sex == Sex.Female ? row.Female : row.Male;
    }
}
