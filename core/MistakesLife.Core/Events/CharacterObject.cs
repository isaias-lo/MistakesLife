using System;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Scripting;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Un personaje visto desde el lenguaje de condiciones ("jugador").
    /// Datos: edad, edad_semanas, salud, felicidad, inteligencia, vivo, sexo,
    /// apertura, responsabilidad, extraversion, amabilidad, neuroticismo.
    /// Métodos: tiene('marca'), marcar('marca'), desmarcar('marca').
    /// </summary>
    public sealed class CharacterObject : IScriptObject
    {
        private readonly Character _character;

        public CharacterObject(Character character)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
        }

        public Value Get(string member, IScriptScope scope)
        {
            var date = ((EventScope)scope).World.Date;
            switch (member)
            {
                case "edad":
                    return Value.FromNumber(_character.AgeInYears(date));
                case "edad_semanas":
                    return Value.FromNumber(_character.AgeInWeeks(date));
                case "salud":
                    return Value.FromNumber(_character.Stats.Health);
                case "felicidad":
                    return Value.FromNumber(_character.Stats.Happiness);
                case "inteligencia":
                    return Value.FromNumber(_character.Stats.Smarts);
                case "vivo":
                    return Value.FromBool(_character.IsAlive);
                case "sexo":
                    return Value.FromText(_character.Sex == Sex.Female ? "femenino" : "masculino");
                case "apertura":
                    return Value.FromNumber(_character.Personality.Openness);
                case "responsabilidad":
                    return Value.FromNumber(_character.Personality.Conscientiousness);
                case "extraversion":
                    return Value.FromNumber(_character.Personality.Extraversion);
                case "amabilidad":
                    return Value.FromNumber(_character.Personality.Agreeableness);
                case "neuroticismo":
                    return Value.FromNumber(_character.Personality.Neuroticism);
                default:
                    throw new ScriptException($"jugador.{member} no existe.");
            }
        }

        public void Set(string member, Value value, IScriptScope scope)
        {
            ((EventScope)scope).RequireWritable("jugador." + member);
            int number = (int)Math.Round(value.AsNumber("jugador." + member), MidpointRounding.AwayFromZero);
            switch (member)
            {
                case "salud":
                    _character.Stats.Health = number;
                    break;
                case "felicidad":
                    _character.Stats.Happiness = number;
                    break;
                case "inteligencia":
                    _character.Stats.Smarts = number;
                    break;
                default:
                    throw new ScriptException($"jugador.{member} no se puede cambiar.");
            }
        }

        public Value Call(string method, Value[] args, IScriptScope scope)
        {
            string flag = EventScope.SingleText("jugador." + method, args);
            switch (method)
            {
                case "tiene":
                    return Value.FromBool(_character.HasFlag(flag));
                case "marcar":
                    ((EventScope)scope).RequireWritable("jugador.marcar");
                    _character.SetFlag(flag);
                    return Value.Null;
                case "desmarcar":
                    ((EventScope)scope).RequireWritable("jugador.desmarcar");
                    _character.ClearFlag(flag);
                    return Value.Null;
                default:
                    throw new ScriptException($"jugador.{method}() no existe.");
            }
        }

        public override string ToString() => "jugador";
    }
}
