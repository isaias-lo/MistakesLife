namespace MistakesLife.Core.Economy
{
    /// <summary>En qué va una búsqueda de trabajo.</summary>
    public enum JobSearchStage
    {
        /// <summary>Manda postulaciones y espera que lo llamen.</summary>
        Searching = 0,

        /// <summary>Lo llamaron a una entrevista: el jugador decide cómo ir.</summary>
        Interview = 1,

        /// <summary>Le ofrecieron el trabajo: lo acepta, lo negocia o lo rechaza.</summary>
        Offer = 2,
    }
}
