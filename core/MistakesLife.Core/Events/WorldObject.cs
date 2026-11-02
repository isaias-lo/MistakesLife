using System;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Events
{
    /// <summary>El mundo visto desde el lenguaje ("mundo"): anio, semana, semanas_totales.</summary>
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
                default:
                    throw new ScriptException($"mundo.{member} no existe.");
            }
        }

        public void Set(string member, Value value, IScriptScope scope) => throw new ScriptException($"mundo.{member} no se puede cambiar.");

        public Value Call(string method, Value[] args, IScriptScope scope) => throw new ScriptException($"mundo.{method}() no existe.");

        public override string ToString() => "mundo";
    }
}
