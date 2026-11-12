using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Una etapa de una causa: la notificación, la audiencia, la sentencia... Dura
    /// por lo menos sus semanas (el plazo real). Si "espera" es false, al cumplirse
    /// el plazo la causa pasa sola a la etapa siguiente (o termina, si es la última).
    /// Si es true, se queda ahí hasta que un evento la mueva (lo que decide el jugador
    /// o el juez).
    /// </summary>
    public sealed class CourtStageDefinition
    {
        /// <summary>Id corto de la etapa dentro de su causa, por ejemplo "notificacion".</summary>
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Semanas que dura como mínimo (el plazo de esa etapa).</summary>
        [JsonProperty("semanas")]
        public int Weeks { get; set; }

        /// <summary>Si la etapa espera a que un evento la mueva.</summary>
        [JsonProperty("espera")]
        public bool Waits { get; set; }

        /// <summary>Clave del nombre de la etapa en la carpeta del juzgado: el id de la causa + ".etapa." + el id de la etapa.</summary>
        public string TextKey(string caseId) => caseId + ".etapa." + Id;
    }
}
