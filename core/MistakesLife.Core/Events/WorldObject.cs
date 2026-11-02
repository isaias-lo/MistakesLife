using System;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// El mundo visto desde el lenguaje ("mundo"): anio, mes, semana, semanas_totales,
    /// y la economía del país donde vive el personaje: inflacion, regimen_inflacion, indice_precios.
    /// </summary>
    public sealed class WorldObject : IScriptObject
    {
        private readonly World _world;

        public WorldObject(World world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public Value Get(string member, IScriptScope scope)
        {
            switch (member)
            {
                case "anio":
                    return Value.FromNumber(_world.Date.Year);
                case "semana":
                    return Value.FromNumber(_world.Date.WeekOfYear);
                case "semanas_totales":
                    return Value.FromNumber(_world.Date.TotalWeeks);
                case "mes":
                    return Value.FromNumber(_world.Date.Month);
                case "inflacion":
                    // En porcentaje, con un decimal: 3.4 es 3,4 % en los últimos 12 meses.
                    return Value.FromNumber(Math.Round((Economy(scope)?.InflationLast12Months ?? 0) * 100, 1));
                case "regimen_inflacion":
                    return Value.FromText(Economy(scope)?.RegimeId ?? string.Empty);
                case "indice_precios":
                    return Value.FromNumber(Economy(scope)?.PriceIndex ?? 1);
                default:
                    throw new ScriptException($"mundo.{member} no existe.");
            }
        }

        private Economy.CountryEconomy? Economy(IScriptScope scope)
        {
            string? countryId = ((EventScope)scope).Subject.Residence?.CountryId;
            return countryId == null ? null : _world.EconomyOf(countryId);
        }

        public void Set(string member, Value value, IScriptScope scope) => throw new ScriptException($"mundo.{member} no se puede cambiar.");

        public Value Call(string method, Value[] args, IScriptScope scope) => throw new ScriptException($"mundo.{method}() no existe.");

        public override string ToString() => "mundo";
    }
}
