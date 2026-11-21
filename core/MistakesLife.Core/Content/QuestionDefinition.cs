using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una pregunta de alternativas de una unidad ("pregunta.chile.matematica.5.fracciones.1").
    /// Su id es la clave del enunciado en lang/; <c>id.a</c>, <c>id.b</c>, <c>id.c</c> (y
    /// <c>id.d</c> si tiene cuatro) las de sus alternativas, y <c>id.por_que</c> la explicación que
    /// se ve al responder. Al mostrarla, el juego baraja las alternativas.
    /// </summary>
    public sealed class QuestionDefinition
    {
        /// <summary>Las letras de las alternativas, en orden.</summary>
        public static readonly string[] Letters = { "a", "b", "c", "d" };

        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Cuántas alternativas tiene (3 si falta; como mucho 4).</summary>
        [JsonProperty("alternativas")]
        public int Options { get; set; } = 3;

        /// <summary>La letra de la alternativa correcta ("a", "b", "c" o "d").</summary>
        [JsonProperty("correcta", Required = Required.Always)]
        public string Correct { get; set; } = string.Empty;

        /// <summary>La posición de la correcta (0 es "a").</summary>
        [JsonIgnore]
        public int CorrectIndex => System.Array.IndexOf(Letters, Correct);

        /// <summary>La clave de una alternativa (posición desde 0).</summary>
        public string OptionKey(int index) => Id + "." + Letters[index];

        /// <summary>La clave de la explicación.</summary>
        [JsonIgnore]
        public string WhyKey => Id + ".por_que";
    }
}
