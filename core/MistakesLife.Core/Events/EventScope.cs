using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Lo que ven las condiciones y efectos de los paquetes.
    ///
    /// Raíces: jugador, mundo.
    /// Funciones: semilla('id'), plantar('id') o plantar('id', persona), diario('clave'), estudiar('nivel'),
    /// si(condición, valor_si, valor_no), ley('id'), cambiar_ley('id', valor),
    /// precio(monto), cambiar_inflacion('regimen'), sueldo_minimo(), sueldo_tipico(), sueldo_titulo(), arriendo('tipo de vivienda').
    /// Ver docs/content-language.md para la lista completa.
    /// </summary>
    public sealed class EventScope : IScriptScope
    {
        public const string SeedFunction = "semilla";
        public const string PlantFunction = Effect.PlantFunction;
        public const string JournalFunction = "diario";
        public const string StudyFunction = "estudiar";
        public const string IfFunction = "si";
        public const string LawFunction = "ley";
        public const string ChangeLawFunction = "cambiar_ley";
        public const string PriceFunction = "precio";
        public const string ChangeInflationFunction = "cambiar_inflacion";
        public const string ChangeCycleFunction = "cambiar_ciclo";
        public const string MinimumSalaryFunction = "sueldo_minimo";
        public const string TypicalSalaryFunction = "sueldo_tipico";
        public const string DegreeSalaryFunction = "sueldo_titulo";
        public const string RentFunction = "arriendo";

        /// <summary>Funciones que leen los sueldos del país: todo país debe tener "economia.sueldos" si un evento las usa.</summary>
        public static readonly IReadOnlyCollection<string> SalaryNames = new[]
        {
            MinimumSalaryFunction + "()", TypicalSalaryFunction + "()", DegreeSalaryFunction + "()",
        };

        /// <summary>Todos los nombres que existen en el lenguaje.</summary>
        public static readonly IReadOnlyCollection<string> KnownNames = new SortedSet<string>(StringComparer.Ordinal)
        {
            "jugador", "mundo", SeedFunction + "()", PlantFunction + "()", JournalFunction + "()", StudyFunction + "()", IfFunction + "()", LawFunction + "()", ChangeLawFunction + "()",
            PriceFunction + "()", ChangeInflationFunction + "()", ChangeCycleFunction + "()",
            MinimumSalaryFunction + "()", TypicalSalaryFunction + "()", DegreeSalaryFunction + "()", RentFunction + "()",
        };

        /// <summary>Nombres que cambian el mundo: no se permiten en condiciones.</summary>
        public static readonly IReadOnlyCollection<string> EffectOnlyNames = new SortedSet<string>(StringComparer.Ordinal)
        {
            PlantFunction + "()", JournalFunction + "()", StudyFunction + "()", ChangeLawFunction + "()", ChangeInflationFunction + "()", ChangeCycleFunction + "()",
        };

        public EventScope(World world, ContentDatabase content, Character subject, Seed? currentSeed, bool readOnly)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            CurrentSeed = currentSeed;
            ReadOnly = readOnly;
        }

        public World World { get; }

        public ContentDatabase Content { get; }

        /// <summary>El personaje a quien le pasa el evento (por ahora, el jugador).</summary>
        public Character Subject { get; }

        /// <summary>La semilla que despertó y provocó este evento, si la hay.</summary>
        public Seed? CurrentSeed { get; }

        public bool ReadOnly { get; }

        public Value GetRoot(string name)
        {
            switch (name)
            {
                case "jugador":
                    return Value.FromObject(new CharacterObject(Subject));
                case "mundo":
                    return Value.FromObject(new WorldObject(World));
                default:
                    throw new ScriptException($"'{name}' no existe.");
            }
        }

        public Value CallFunction(string name, Value[] args)
        {
            switch (name)
            {
                case SeedFunction:
                    return Value.FromObject(new SeedHandle(this, SingleText(name, args)));
                case PlantFunction:
                    RequireWritable(name);
                    Character? witness = null;
                    if (args.Length == 2)
                    {
                        // plantar('semilla.x', persona): esa persona es testigo (por ejemplo, el amigo al que le prestaste).
                        witness = args[1].Kind == ValueKind.Null
                            ? throw new ScriptException("plantar(): el testigo es nada; revisa que exista antes (por ejemplo, jugador.amigo != nada).")
                            : (args[1].AsObject("plantar()") as CharacterObject)?.Character
                              ?? throw new ScriptException("plantar(): el testigo debe ser una persona, por ejemplo plantar('semilla.x', jugador.amigo).");
                        args = new[] { args[0] };
                    }

                    SeedTypeDefinition type = Content.GetSeedType(SingleText(name, args));
                    PlantOrStrengthen(type, witness);
                    return Value.Null;
                case StudyFunction:
                    RequireWritable(name);
                    CareerLevelDefinition level = Content.GetCareerLevel(SingleText(name, args));
                    Subject.Studies = new CareerStudies(level.CareerId, level.Id, level.Institution, level.Semesters);
                    return Value.Null;
                case IfFunction:
                    if (args.Length != 3)
                    {
                        throw new ScriptException("si() necesita tres valores: si(condición, valor_si, valor_no).");
                    }

                    return args[0].AsBool("si()") ? args[1] : args[2];
                case LawFunction:
                    return Law(SingleText(name, args));
                case ChangeLawFunction:
                    RequireWritable(name);
                    if (args.Length != 2)
                    {
                        throw new ScriptException("cambiar_ley() necesita dos valores: cambiar_ley('ley.x', valor).");
                    }

                    string lawId = args[0].AsText(name + "()");
                    Law(lawId);
                    World.ChangeLaw(SubjectCountry().Id, lawId, args[1]);
                    return Value.Null;
                case PriceFunction:
                    if (args.Length != 1)
                    {
                        throw new ScriptException("precio() necesita un monto: precio(15000).");
                    }

                    // El monto está en dinero del inicio del mundo; se lleva a precios de hoy.
                    double amount = args[0].AsNumber(name + "()");
                    double index = Subject.Residence == null ? 1 : World.EconomyOf(Subject.Residence.CountryId)?.PriceIndex ?? 1;
                    return Value.FromNumber(Math.Round(amount * index, MidpointRounding.AwayFromZero));
                case MinimumSalaryFunction:
                    NoArgs(name, args);
                    return Value.FromNumber((double)Salaries().Minimum);
                case TypicalSalaryFunction:
                    NoArgs(name, args);
                    return Value.FromNumber((double)Salaries().Typical);
                case DegreeSalaryFunction:
                    NoArgs(name, args);
                    return Value.FromNumber(DegreeSalary());
                case RentFunction:
                    return Value.FromNumber((double)Economy.HousingRules.MarketRent(Content, Subject, SingleText(name, args)));
                case ChangeInflationFunction:
                    RequireWritable(name);
                    ChangeInflation(SingleText(name, args));
                    return Value.Null;
                case ChangeCycleFunction:
                    RequireWritable(name);
                    ChangeCycle(SingleText(name, args));
                    return Value.Null;
                case JournalFunction:
                    RequireWritable(name);
                    World.WriteJournal(Subject.Id, SingleText(name, args));
                    return Value.Null;
                default:
                    throw new ScriptException($"La función '{name}' no existe.");
            }
        }

        /// <summary>
        /// Si el personaje ya tiene abierta una semilla del mismo tipo, repetir el acto
        /// la hace más fuerte en vez de plantar otra (copiar diez veces no trae diez
        /// profesores distintos, pero sí uno que se acuerda mejor).
        /// </summary>
        private void PlantOrStrengthen(SeedTypeDefinition type, Character? witness)
        {
            Seed? existing = World.Seeds.OpenFor(Subject.Id).FirstOrDefault(s => s.TypeId == type.Id);
            if (existing != null)
            {
                existing.Strengthen(type.Strength);
                if (witness != null)
                {
                    existing.AddWitness(witness.Id);
                }

                return;
            }

            World.Seeds.Plant(type.ToSeedDefinition(Content), Subject.Id, World.Date, Subject.AgeInYears(World.Date), witness == null ? null : new[] { witness.Id });
        }

        /// <summary>
        /// El valor de una ley en el país donde vive el personaje: el que tenga
        /// ahora si cambió durante la partida, o el de su paquete de país.
        /// </summary>
        private Value Law(string lawId)
        {
            CountryDefinition country = SubjectCountry();
            if (World.TryGetChangedLaw(country.Id, lawId, out Value changed))
            {
                return changed;
            }

            return country.TryGetLaw(lawId, out Value value) ? value : throw new ScriptException($"ley('{lawId}'): el país {country.Id} no define esa ley.");
        }

        private SalaryDefinition Salaries()
        {
            CountryDefinition country = SubjectCountry();
            return country.Economy?.Salaries ?? throw new ScriptException($"El país {country.Id} no define sus sueldos (economia.sueldos).");
        }

        /// <summary>
        /// Sueldo de mercado del personaje según su mejor título: el sueldo típico del
        /// país por el "sueldo_relativo" del nivel (o por "relativo_sin_titulo" si no
        /// tiene título). Nunca menos que el sueldo mínimo. En dinero del inicio del mundo.
        /// </summary>
        private double DegreeSalary() => DegreeSalaryOf(Subject);

        /// <summary>sueldo_titulo() de esa persona: el sueldo típico por lo que vale su mejor título.</summary>
        internal double DegreeSalaryOf(Character person)
        {
            SalaryDefinition salaries = Salaries();
            double relative = person.Degrees.Count == 0
                ? salaries.NoDegreeRelative
                : person.Degrees.Max(d => Content.GetCareerLevel(d).SalaryRelative);
            return Math.Max((double)salaries.Minimum, Math.Round((double)salaries.Typical * relative));
        }

        private static void NoArgs(string function, Value[] args)
        {
            if (args.Length != 0)
            {
                throw new ScriptException($"{function}() no recibe valores.");
            }
        }

        /// <summary>Cambia el régimen de inflación del país del personaje (por ejemplo, en una crisis).</summary>
        private void ChangeInflation(string regimeId)
        {
            CountryDefinition country = SubjectCountry();
            InflationRegimeDefinition regime = country.Economy?.Inflation.Regime(regimeId)
                ?? throw new ScriptException($"cambiar_inflacion('{regimeId}'): el país {country.Id} no tiene ese régimen de inflación.");
            EconomySystem.Initialize(World, Content);
            CountryEconomy economy = World.EconomyOf(country.Id)!;
            var random = World.Random.Fork("economia.cambio." + country.Id + "." + World.Date.TotalWeeks);
            EconomySystem.ChangeRegime(World, economy, regime, random);
        }

        private void ChangeCycle(string phaseId)
        {
            CountryDefinition country = SubjectCountry();
            EconomicPhaseDefinition phase = country.Economy?.Cycle?.Phase(phaseId)
                ?? throw new ScriptException($"cambiar_ciclo('{phaseId}'): el país {country.Id} no tiene esa fase del ciclo económico.");
            EconomySystem.Initialize(World, Content);
            CountryEconomy economy = World.EconomyOf(country.Id)!;
            var random = World.Random.Fork("economia.ciclo." + country.Id + "." + World.Date.TotalWeeks);
            EconomySystem.EnterPhase(World, country, economy, phase, random);
        }

        private CountryDefinition SubjectCountry() =>
            Subject.Residence == null
                ? throw new ScriptException($"{Subject} no vive en ningún país: no tiene leyes.")
                : Content.GetCountry(Subject.Residence.CountryId);

        internal void RequireWritable(string what)
        {
            if (ReadOnly)
            {
                throw new ScriptException($"'{what}' cambia el mundo y no puede usarse en una condición.");
            }
        }

        internal static string SingleText(string function, Value[] args)
        {
            if (args.Length != 1)
            {
                throw new ScriptException($"{function}() necesita exactamente un texto entre comillas.");
            }

            return args[0].AsText(function + "()");
        }
    }
}
