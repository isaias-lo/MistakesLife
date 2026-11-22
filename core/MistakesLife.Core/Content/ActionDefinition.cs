using System.Collections.Generic;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Algo que el jugador hace con el tiempo parado y que da resultado al tiro (archivos de
    /// acciones/ de un paquete, D3 en docs/roadmap.md): estudiar para la prueba, juntarse con un
    /// amigo, hacer deporte, descansar. Gasta tardes libres del mes (<see cref="Life.FreeTimeRules"/>).
    /// Su id es la clave de su nombre en lang/; <c>id.por_que</c> es la línea que explica por qué
    /// tiene sentido ahora y <c>id.resultado</c> lo que pasó (los dos pueden usar <c>{persona}</c>).
    /// </summary>
    public sealed class ActionDefinition
    {
        /// <summary>Los grupos de la pantalla principal donde puede ir una acción.</summary>
        public static readonly string[] Groups = { "gente", "estudios", "trabajo", "salud", "dinero" };

        /// <summary>
        /// Lo que puede cambiar una acción: las barras del jugador, el cariño de la persona con
        /// quien la hace (<c>carino</c>) y lo preparada que está la prueba anunciada (<c>preparar_prueba</c>).
        /// </summary>
        public static readonly string[] Changes = { "felicidad", "salud", "inteligencia", "apariencia", "energia", "carino", "preparar_prueba" };

        /// <summary>
        /// Con quién más se puede hacer una acción, además de quienes escriben por Mensajito
        /// (<see cref="ChatDefinition.Senders"/>): <c>conocido</c> es alguien con quien tiene
        /// relación sin ser familia, pareja, amistad, rival ni ex (tu curso, tu pega, gente que
        /// conociste); <c>cualquiera</c> es cualquier persona viva con quien tiene relación;
        /// <c>interesante</c> es la gente interesante de tu horario que todavía no conoces
        /// (D4b2, ver <see cref="Life.InterestingPeopleRules"/>): si dice que sí, se conocen; si
        /// dice que no, cuenta para que se cierre la puerta.
        /// Las acciones con ellos van solo en la ficha de cada persona.
        /// </summary>
        public static readonly string[] ExtraRoles = { Acquaintance, Anyone, Interesting };

        public const string Acquaintance = "conocido";

        public const string Anyone = "cualquiera";

        public const string Interesting = "interesante";

        /// <summary>Lo máximo que una acción mueve una barra o el cariño, para arriba o para abajo.</summary>
        public const int MaxChange = 8;

        /// <summary>Id estable: <c>accion.&lt;nombre&gt;</c>.</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>En qué grupo aparece (ver <see cref="Groups"/>).</summary>
        [JsonProperty("grupo", Required = Required.Always)]
        public string Group { get; set; } = string.Empty;

        [JsonProperty("edad_minima")]
        public int MinAge { get; set; }

        [JsonProperty("edad_maxima")]
        public int MaxAge { get; set; } = 150;

        /// <summary>Condición extra (lenguaje de condiciones), por ejemplo tener una prueba anunciada.</summary>
        [JsonProperty("condiciones")]
        public string Conditions { get; set; } = string.Empty;

        /// <summary>
        /// Con quién se hace (amigo, pareja, companero, hijo...): la acción aparece una vez por
        /// cada persona que ocupa ese lugar en la vida del jugador.
        /// </summary>
        [JsonProperty("con")]
        public string? With { get; set; }

        /// <summary>Cuántas tardes libres gasta.</summary>
        [JsonProperty("tardes")]
        public int Afternoons { get; set; } = 1;

        /// <summary>Cuántas veces se puede hacer al mes (con cada persona, si dice <see cref="With"/>).</summary>
        [JsonProperty("veces_al_mes")]
        public int PerMonth { get; set; } = 1;

        /// <summary>Lo que cambia al hacerla (ver <see cref="Changes"/>): es también lo que se muestra antes de elegirla.</summary>
        [JsonProperty("cambia")]
        public Dictionary<string, int> Effects { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Si va solo en la ficha de cada persona y no en la lista del grupo (conversar o invitar
        /// a alguien: con todos tus conocidos la lista sería eterna).
        /// </summary>
        [JsonProperty("solo_en_ficha")]
        public bool CardOnly { get; set; }

        /// <summary>Condición sobre la persona (con <c>persona</c>): por ejemplo, que trabaje en una pega del juego.</summary>
        [JsonProperty("condiciones_persona")]
        public string PersonConditions { get; set; } = string.Empty;

        /// <summary>
        /// Si la persona dice que sí (con <c>persona</c>), por ejemplo <c>persona.carino &gt;= 50</c>.
        /// Si dice que no, la tarde se gasta igual, cambia lo de <see cref="IfDeclined"/>, no
        /// pasa lo de <see cref="Scripts"/> y se cuenta con <c>id.resultado.no</c>. Vacío: siempre dice que sí.
        /// </summary>
        [JsonProperty("acepta")]
        public string Accepts { get; set; } = string.Empty;

        /// <summary>Lo que cambia si la persona dice que no (ver <see cref="Accepts"/>).</summary>
        [JsonProperty("si_no_acepta")]
        public Dictionary<string, int> IfDeclined { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Lo demás que pasa si se hace (y la persona dice que sí), en el lenguaje de los eventos:
        /// nace una amistad (<c>persona.vincular('amistad')</c>), te prestan plata y queda la
        /// semilla de devolverla, el café cuesta. No se muestra antes: lo cuenta el resultado.
        /// </summary>
        [JsonProperty("efectos")]
        public List<string> Scripts { get; set; } = new List<string>();

        /// <summary>
        /// Lo que la persona te puede aportar con esta acción, en una frase (una clave de idioma,
        /// "aporta.consejo"): la ficha de la persona lo junta en "Lo que te puede aportar" (D4b2).
        /// Vacío si la acción no aporta nada en especial (conversar, invitar).
        /// </summary>
        [JsonProperty("aporta")]
        public string? Offers { get; set; }

        /// <summary>
        /// El lugar del mapa de tu barrio donde se hace (<see cref="MapPlaceDefinition"/>, D5c2): el
        /// cine en el mall, el café. Las acciones con lugar no van en la lista del grupo ni en la
        /// ficha: se eligen tocando el lugar, y ahí se elige con quién.
        /// </summary>
        [JsonProperty("lugar")]
        public string? Place { get; set; }

        /// <summary>Si dice con quién (<see cref="With"/>), si también se puede ir solo (ver una película, entrenar).</summary>
        [JsonProperty("solo")]
        public bool Alone { get; set; }

        /// <summary>
        /// Lo que cuesta tu parte, por país (id del país → monto en dinero de los paquetes; se lleva a
        /// precios de hoy como <c>precio()</c>). Lo pagas desde los 18; antes lo paga tu casa.
        /// Se cobra solo si se hace (si la persona dice que no, no fueron).
        /// </summary>
        [JsonProperty("precio")]
        public Dictionary<string, int> Price { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// A quién le gusta especialmente (condición sobre la persona, con <c>persona</c>): sale como
        /// motivo al sugerir con quién ir (<c>id.le_gusta</c>, "Le encanta el cine").
        /// </summary>
        [JsonProperty("le_gusta")]
        public string Likes { get; set; } = string.Empty;

        [JsonIgnore]
        public Expression? CompiledLikes { get; set; }

        /// <summary>El país del paquete de la acción; null si es de base (sale en todos).</summary>
        [JsonIgnore]
        public string? CountryId { get; set; }

        /// <summary>La clave de lo que pasó si fue sin nadie (ver <see cref="Alone"/>).</summary>
        [JsonIgnore]
        public string AloneResultKey => Id + ".resultado.solo";

        /// <summary>La clave del motivo "le gusta" (ver <see cref="Likes"/>).</summary>
        [JsonIgnore]
        public string LikesKey => Id + ".le_gusta";

        /// <summary>Desde qué edad paga su parte el jugador (antes la paga su casa).</summary>
        public const int PaysFromAge = 18;

        [JsonIgnore]
        public Expression? CompiledConditions { get; set; }

        [JsonIgnore]
        public Expression? CompiledPersonConditions { get; set; }

        [JsonIgnore]
        public Expression? CompiledAccepts { get; set; }

        [JsonIgnore]
        public List<Effect> CompiledScripts { get; } = new List<Effect>();

        [JsonIgnore]
        public string PackId { get; set; } = string.Empty;

        /// <summary>La clave de la línea que explica por qué tiene sentido ahora.</summary>
        [JsonIgnore]
        public string WhyKey => Id + ".por_que";

        /// <summary>La clave de lo que pasó al hacerla.</summary>
        [JsonIgnore]
        public string ResultKey => Id + ".resultado";

        /// <summary>La clave de lo que pasó si la persona dijo que no (ver <see cref="Accepts"/>).</summary>
        [JsonIgnore]
        public string DeclinedKey => Id + ".resultado.no";

        /// <summary>Lo que cambia en <paramref name="key"/> (0 si no lo dice).</summary>
        public int Change(string key) => Effects.TryGetValue(key, out int value) ? value : 0;
    }
}
