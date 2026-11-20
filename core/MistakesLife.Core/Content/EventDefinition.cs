using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Un evento tal como se escribe en un paquete JSON (carpeta events/):
    /// cuándo puede pasar, qué tan probable es, qué opciones ofrece y qué cambia cada una.
    /// </summary>
    public sealed class EventDefinition
    {
        /// <summary>Id estable, por ejemplo "evento.entrevista_testigo_copia".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Clave del texto que describe la escena. Por defecto, el mismo id.</summary>
        [JsonProperty("texto")]
        public string? TextKey { get; set; }

        /// <summary>
        /// Otras formas de contar la escena cuando el evento vuelve a pasar (claves de texto).
        /// La primera vez se usa <see cref="TextKey"/>; la segunda, la primera variante; y así.
        /// Cuando se acaban, se vuelve a empezar. Así un evento que se repite no se lee igual.
        /// </summary>
        [JsonProperty("variantes")]
        public List<string> Variants { get; set; } = new List<string>();

        /// <summary>
        /// Cuántas veces como máximo puede pasar en la vida del personaje (null = sin tope).
        /// "unico": true es lo mismo que 1.
        /// </summary>
        [JsonProperty("veces_maximas")]
        public int? MaxTimes { get; set; }

        /// <summary>
        /// Si es true (y el evento pasa <see cref="With"/> alguien), <see cref="MaxTimes"/> y las
        /// <see cref="Variants"/> se cuentan con cada persona: con tu pareja de hoy el aniversario
        /// se cuenta de nuevo, aunque con la anterior ya hayas vivido todas sus formas.
        /// </summary>
        [JsonProperty("por_persona")]
        public bool PerPerson { get; set; }

        /// <summary>Cuándo puede pasar (lenguaje de condiciones). Opcional.</summary>
        [JsonProperty("condiciones")]
        public string? Conditions { get; set; }

        /// <summary>Qué tan probable es frente a otros eventos posibles.</summary>
        [JsonProperty("peso")]
        public int Weight { get; set; } = 10;

        /// <summary>Si es true, detiene el tiempo: el jugador lo vive con calma.</summary>
        [JsonProperty("dia_clave")]
        public bool KeyDay { get; set; }

        /// <summary>Si es true, solo puede pasar una vez en la vida del personaje.</summary>
        [JsonProperty("unico")]
        public bool Unique { get; set; }

        /// <summary>
        /// Semanas que deben pasar antes de que el mismo evento pueda repetirse
        /// (0 = sin espera). Evita que un evento común aparezca seguido.
        /// </summary>
        [JsonProperty("enfriamiento_semanas")]
        public int CooldownWeeks { get; set; }

        /// <summary>
        /// Si se indica, el evento no pasa al azar: es la escena que aparece
        /// cuando despierta una semilla de ese tipo.
        /// </summary>
        [JsonProperty("semilla")]
        public string? SeedType { get; set; }

        /// <summary>Efectos que se aplican apenas ocurre, antes de elegir.</summary>
        [JsonProperty("efectos")]
        public List<string> Effects { get; set; } = new List<string>();

        [JsonProperty("opciones")]
        public List<EventOptionDefinition> Options { get; set; } = new List<EventOptionDefinition>();

        /// <summary>
        /// Probabilidad de que ocurra en un año (0 a 1) mientras se cumplen sus condiciones,
        /// tomada de estadísticas reales (por ejemplo 0.04 para un robo). Si se usa, el
        /// evento no compite por peso con los demás: cada semana tiene su propia oportunidad.
        /// Opcional.
        /// </summary>
        [JsonProperty("probabilidad_anual")]
        public double? AnnualProbability { get; set; }

        /// <summary>Oportunidad semanal equivalente a la probabilidad anual.</summary>
        [JsonIgnore]
        public double WeeklyProbability =>
            AnnualProbability.HasValue ? 1.0 - System.Math.Pow(1.0 - AnnualProbability.Value, 1.0 / Time.GameDate.WeeksPerYear) : 0.0;

        /// <summary>
        /// Si es verdadero, al armar el contenido se agrega una opción por cada nivel
        /// de cada carrera cargada ("estudiar esto"), antes de las opciones propias.
        /// Así un país no necesita conocer las carreras que existen.
        /// </summary>
        [JsonProperty("opciones_carreras")]
        public bool CareerOptions { get; set; }

        /// <summary>
        /// Con quién pasa el momento: un lugar en la vida del jugador (madre, padre,
        /// pareja, hijo, amigo, colega, rival, ex, conocido, cualquiera...). Al ocurrir se elige a una persona
        /// de verdad, con nombre, y en el evento se le habla como "persona"
        /// (por ejemplo "persona.carino += 3"). Si no hay nadie así, el evento no pasa.
        /// En los textos, {persona} se cambia por su nombre. Opcional.
        /// En un evento de semilla puede ser <see cref="WitnessRole"/>: pasa con quien vio
        /// lo que hiciste (el testigo vivo más antiguo de la semilla).
        /// </summary>
        [JsonProperty("con")]
        public string? With { get; set; }

        /// <summary>El "con" de los eventos de semilla que pasan con su testigo.</summary>
        public const string WitnessRole = "testigo";

        /// <summary>
        /// El "con" de los momentos en que conoces a alguien nuevo (D4d1): al pasar, el juego crea
        /// a una persona con nombre que te podría gustar (ver Life.FamilyFactory.CreateStranger) y
        /// queda como conocido. Sus condiciones no pueden usar "persona": se revisan antes de que exista.
        /// </summary>
        public const string NewPersonRole = "alguien_nuevo";

        /// <summary>Dónde pasa el momento: la clave de un lugar con nombre ("lugar.plaza"). Opcional.</summary>
        [JsonProperty("lugar")]
        public string? Place { get; set; }

        /// <summary>
        /// Si se indica, el momento nace de tu horario: solo pasa si esa actividad está
        /// en tu semana, y es más probable mientras más veces la tengas.
        /// <see cref="AnnualProbability"/> es la probabilidad en un año haciéndola una vez por semana.
        /// </summary>
        [JsonProperty("actividad")]
        public string? Activity { get; set; }

        /// <summary>
        /// Si se indica, el momento tiene fecha: "24-12" (día-mes) pasa la semana de ese día,
        /// y "cumpleanos" pasa la semana del cumpleaños de la persona (pide "con").
        /// Pasa como mucho una vez al año. Si además tiene <see cref="AnnualProbability"/>,
        /// esa es la probabilidad de que ese año se viva como momento.
        /// </summary>
        [JsonProperty("fecha")]
        public string? Date { get; set; }

        /// <summary>Valor de "fecha" para los cumpleaños.</summary>
        public const string BirthdayDate = "cumpleanos";

        /// <summary>Si la fecha es el cumpleaños de la persona.</summary>
        [JsonIgnore]
        public bool OnBirthday => Date == BirthdayDate;

        /// <summary>El día del año (desde 0) de una fecha fija como "24-12"; null si no tiene o es un cumpleaños.</summary>
        [JsonIgnore]
        public int? FixedDayOfYear { get; internal set; }

        /// <summary>
        /// Si se indica, el momento lo pide el jugador con un botón (ver <see cref="RequestDefinition"/>)
        /// y nunca pasa solo. Opcional.
        /// </summary>
        [JsonProperty("a_pedido")]
        public RequestDefinition? OnRequest { get; set; }

        /// <summary>
        /// Si es un momento que nace del horario, del calendario o de lo que pide el jugador
        /// (no compite al azar con los demás eventos).
        /// </summary>
        [JsonIgnore]
        public bool IsScheduled => Activity != null || Date != null || OnRequest != null;

        /// <summary>
        /// Grupo para las reglas propias del jugador ("regla.prueba"): una regla hecha en
        /// un evento del grupo sirve para todos los del grupo. Su texto ("Cuando hay prueba")
        /// va en lang/. Si falta, la regla sirve solo para este evento.
        /// </summary>
        [JsonProperty("regla")]
        public string? RuleGroup { get; set; }

        /// <summary>A qué se aplica una regla propia hecha en este evento.</summary>
        [JsonIgnore]
        public string RuleKey => RuleGroup ?? Id;

        /// <summary>
        /// Si el jugador puede decir "si vuelve a pasar, haz lo mismo". Un día clave y lo que
        /// despierta una semilla (un error que vuelve) siempre se viven: no admiten regla.
        /// </summary>
        [JsonIgnore]
        public bool AllowsOwnRule => !KeyDay && SeedType == null && Options.Count > 1;

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>
        /// Si el evento viene del paquete de un país, ese país: solo le pasa a quien
        /// vive ahí. Null en los eventos del paquete base y de las carreras, que le
        /// pueden pasar a cualquiera.
        /// </summary>
        [JsonIgnore]
        public string? CountryId { get; internal set; }

        [JsonIgnore]
        public string ResolvedTextKey => string.IsNullOrEmpty(TextKey) ? Id : TextKey!;

        /// <summary>Todas las formas de contar la escena: la principal y sus variantes, en orden.</summary>
        [JsonIgnore]
        public IEnumerable<string> AllTextKeys
        {
            get
            {
                yield return ResolvedTextKey;
                foreach (string variant in Variants)
                {
                    yield return variant;
                }
            }
        }

        /// <summary>
        /// La clave del texto que toca mostrar, según cuántas veces le pasó antes
        /// al personaje (0 = es la primera vez).
        /// </summary>
        public string TextKeyFor(int timesBefore)
        {
            if (Variants.Count == 0 || timesBefore <= 0)
            {
                return ResolvedTextKey;
            }

            int index = timesBefore % (Variants.Count + 1);
            return index == 0 ? ResolvedTextKey : Variants[index - 1];
        }

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public List<Effect> CompiledEffects { get; } = new List<Effect>();
    }
}
