using System.Collections.Generic;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Campo "economia.vacaciones" de country.json: las vacaciones que da la ley a quien
    /// trabaja con contrato (el feriado anual en Chile, las vacaciones de la Ley de Contrato
    /// de Trabajo en Argentina). Se ganan mes a mes y las toma el jugador cuando quiere.
    /// </summary>
    public sealed class VacationsDefinition
    {
        /// <summary>
        /// Días al año según los años con el mismo empleador, de menor a mayor antigüedad
        /// (Chile: un tramo de 15; Argentina: 14, 21, 28 y 35).
        /// </summary>
        [JsonProperty("tramos", Required = Required.Always)]
        public List<VacationTierDefinition> Tiers { get; set; } = new List<VacationTierDefinition>();

        /// <summary>"habiles" (Chile: de lunes a viernes) o "corridos" (Argentina).</summary>
        [JsonProperty("unidad", Required = Required.Always)]
        public string Unit { get; set; } = "habiles";

        /// <summary>
        /// Feriado progresivo (Chile, art. 68 del Código del Trabajo): con tantos años
        /// trabajados en total, un día más por cada tantos años nuevos. 0 si el país no lo tiene.
        /// </summary>
        [JsonProperty("progresivo_desde_anos")]
        public int ProgressiveFromYears { get; set; }

        /// <summary>Cada cuántos años nuevos se suma un día de feriado progresivo.</summary>
        [JsonProperty("progresivo_cada_anos")]
        public int ProgressiveEveryYears { get; set; }

        /// <summary>
        /// Cuántos de esos años pueden venir de empleadores anteriores (en Chile, hasta 10:
        /// el resto tiene que ser con el empleador de ahora).
        /// </summary>
        [JsonProperty("progresivo_maximo_anos_anteriores")]
        public int ProgressiveMaxPriorYears { get; set; }

        /// <summary>
        /// Cuántos años de vacaciones se pueden juntar sin tomarlas (Chile: 2 períodos, art. 70;
        /// Argentina: las del año, art. 162). Lo que pasa de ahí se pierde.
        /// </summary>
        [JsonProperty("acumula_maximo_anos")]
        public int MaxYearsSaved { get; set; } = 1;

        /// <summary>
        /// Si al terminar el contrato se pagan los días ganados y no tomados (el feriado
        /// proporcional de Chile, art. 73; la indemnización por vacaciones no gozadas, art. 156 LCT).
        /// </summary>
        [JsonProperty("se_pagan_al_salir")]
        public bool PaidOnLeaving { get; set; } = true;

        /// <summary>
        /// Por cuánto se divide el sueldo mensual para saber cuánto vale un día de vacaciones
        /// (Chile: 30, el sueldo diario; Argentina: 25, art. 155 LCT).
        /// </summary>
        [JsonProperty("divisor_dia")]
        public double DayDivisor { get; set; } = 30;

        /// <summary>
        /// Cuántos días se pagan por cada día ganado al salir (Chile: los días hábiles se pagan
        /// como corridos, con sábados, domingos y feriados: unos 1,4 por cada hábil; Argentina: 1).
        /// </summary>
        [JsonProperty("dias_pagados_por_dia")]
        public double PaidDaysPerDay { get; set; } = 1;
    }
}
