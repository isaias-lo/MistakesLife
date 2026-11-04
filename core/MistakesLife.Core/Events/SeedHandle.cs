using System.Linq;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Resultado de semilla('id'): la semilla abierta de ese tipo del personaje
    /// (la que despertó, si es ese tipo; si no, la más reciente).
    /// Datos: activa, despierta, fuerza (se puede cambiar), testigos, testigo (el primero vivo, o nada).
    /// Acción: cerrar.
    /// </summary>
    public sealed class SeedHandle : IScriptObject
    {
        private readonly EventScope _scope;
        private readonly string _typeId;

        public SeedHandle(EventScope scope, string typeId)
        {
            _scope = scope;
            _typeId = typeId;
        }

        private Seed? Target
        {
            get
            {
                Seed? current = _scope.CurrentSeed;
                if (current != null && current.TypeId == _typeId && current.IsOpen)
                {
                    return current;
                }

                return _scope.World.Seeds.OpenFor(_scope.Subject.Id).Where(s => s.TypeId == _typeId).OrderByDescending(s => s.Id).FirstOrDefault();
            }
        }

        public Value Get(string member, IScriptScope scope)
        {
            Seed? seed = Target;
            switch (member)
            {
                case "activa":
                    return Value.FromBool(seed != null);
                case "despierta":
                    return Value.FromBool(seed != null && seed.State == SeedState.Awake);
                case "fuerza":
                    return Value.FromNumber(seed?.Strength ?? 0);
                case "testigos":
                    return Value.FromNumber(seed?.Witnesses.Count ?? 0);
                case "testigo":
                    // El testigo vivo más antiguo (el primero que vio lo que pasó), visto desde el jugador.
                    foreach (int witnessId in seed?.Witnesses.OrderBy(w => w) ?? Enumerable.Empty<int>())
                    {
                        if (_scope.World.TryGetCharacter(witnessId, out Characters.Character? witness) && witness.IsAlive)
                        {
                            return Value.FromObject(new CharacterObject(witness, _scope.Subject));
                        }
                    }

                    return Value.Null;
                case "cerrar":
                    _scope.RequireWritable("semilla.cerrar");
                    if (seed != null)
                    {
                        _scope.World.Seeds.Resolve(seed, _scope.World.Date);
                    }

                    return Value.Null;
                default:
                    throw new ScriptException($"semilla('{_typeId}').{member} no existe.");
            }
        }

        public void Set(string member, Value value, IScriptScope scope)
        {
            _scope.RequireWritable($"semilla('{_typeId}').{member}");
            if (member != "fuerza")
            {
                throw new ScriptException($"semilla('{_typeId}').{member} no se puede cambiar.");
            }

            Seed? seed = Target;
            if (seed == null)
            {
                return;
            }

            int delta = (int)System.Math.Round(value.AsNumber("fuerza")) - seed.Strength;
            if (delta >= 0)
            {
                seed.Strengthen(delta);
            }
            else
            {
                seed.Weaken(-delta);
            }
        }

        public Value Call(string method, Value[] args, IScriptScope scope) =>
            throw new ScriptException($"semilla('{_typeId}').{method}() no existe.");

        public override string ToString() => $"semilla('{_typeId}')";
    }
}
