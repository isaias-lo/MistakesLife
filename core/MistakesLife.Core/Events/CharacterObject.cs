using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Scripting;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Un personaje visto desde el lenguaje de condiciones ("jugador").
    /// Datos: edad, edad_semanas, salud, felicidad, inteligencia, vivo, sexo,
    /// apertura, responsabilidad, extraversion, amabilidad, neuroticismo,
    /// pais, ciudad, comuna, nivel_hogar, etapa, madre, padre,
    /// estudia, nivel_estudio, institucion, semestres_aprobados, semestres_carrera, titulado.
    /// Métodos: tiene('marca'), marcar('marca'), desmarcar('marca'),
    /// titulado_en('carrera o nivel'), titularse(), desertar().
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
                case "pais":
                    return Value.FromText(_character.Residence?.CountryId ?? string.Empty);
                case "ciudad":
                    return Value.FromText(_character.Residence?.CityId ?? string.Empty);
                case "comuna":
                    return Value.FromText(_character.Residence?.CommuneId ?? string.Empty);
                case "nivel_hogar":
                    return Value.FromNumber(_character.HouseholdLevel);
                case "etapa":
                    return Value.FromText(_character.EducationStageId ?? string.Empty);
                case "estudia":
                    return TextOrNull(_character.Studies?.CareerId);
                case "nivel_estudio":
                    return TextOrNull(_character.Studies?.LevelId);
                case "institucion":
                    return TextOrNull(_character.Studies?.Institution);
                case "semestres_aprobados":
                    return Value.FromNumber(_character.Studies?.CompletedSemesters ?? 0);
                case "semestres_carrera":
                    return Value.FromNumber(_character.Studies?.RequiredSemesters ?? 0);
                case "titulado":
                    return Value.FromBool(_character.Degrees.Count > 0);
                case "madre":
                    return Relative(_character.MotherId, scope);
                case "padre":
                    return Relative(_character.FatherId, scope);
                default:
                    throw new ScriptException($"jugador.{member} no existe.");
            }
        }

        private static Value TextOrNull(string? text) => text == null ? Value.Null : Value.FromText(text);

        private static Value Relative(int? id, IScriptScope scope)
        {
            if (id.HasValue && ((EventScope)scope).World.TryGetCharacter(id.Value, out Character? relative))
            {
                return Value.FromObject(new CharacterObject(relative));
            }

            return Value.Null;
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
                case "semestres_aprobados":
                    if (_character.Studies == null)
                    {
                        throw new ScriptException("jugador.semestres_aprobados solo se puede cambiar mientras estudia.");
                    }

                    _character.Studies.CompletedSemesters = number;
                    break;
                default:
                    throw new ScriptException($"jugador.{member} no se puede cambiar.");
            }
        }

        public Value Call(string method, Value[] args, IScriptScope scope)
        {
            switch (method)
            {
                case "titularse":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.titularse");
                    RequireStudies(method);
                    _character.Graduate();
                    return Value.Null;
                case "desertar":
                    NoArguments(method, args);
                    ((EventScope)scope).RequireWritable("jugador.desertar");
                    RequireStudies(method);
                    _character.Studies = null;
                    return Value.Null;
                case "titulado_en":
                    string career = EventScope.SingleText("jugador." + method, args);
                    return Value.FromBool(_character.Degrees.Any(d => d == career || d.StartsWith(career + ".", StringComparison.Ordinal)));
            }

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

        private static void NoArguments(string method, Value[] args)
        {
            if (args.Length != 0)
            {
                throw new ScriptException($"jugador.{method}() no recibe valores.");
            }
        }

        private void RequireStudies(string method)
        {
            if (_character.Studies == null)
            {
                throw new ScriptException($"jugador.{method}(): no está estudiando.");
            }
        }

        public override string ToString() => _character.ToString();
    }
}
