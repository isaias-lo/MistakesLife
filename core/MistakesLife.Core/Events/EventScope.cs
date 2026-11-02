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
    /// Funciones: semilla('id'), plantar('id'), diario('clave'), estudiar('nivel'),
    /// si(condición, valor_si, valor_no), ley('id'), cambiar_ley('id', valor),
    /// precio(monto), cambiar_inflacion('regimen').
    /// Ver docs/content-language.md para la lista completa.
    /// </summary>
    public sealed class EventScope : IScriptScope
    {
        public const string SeedFunction = "semilla";
        public const string PlantFunction = "plantar";
        public const string JournalFunction = "diario";
        public const string StudyFunction = "estudiar";
        public const string IfFunction = "si";
        public const string LawFunction = "ley";
        public const string ChangeLawFunction = "cambiar_ley";
        public const string PriceFunction = "precio";
        public const string ChangeInflationFunction = "cambiar_inflacion";

        /// <summary>Todos los nombres que existen en el lenguaje.</summary>
        public static readonly IReadOnlyCollection<string> KnownNames = new SortedSet<string>(StringComparer.Ordinal)
        {
            "jugador", "mundo", SeedFunction + "()", PlantFunction + "()", JournalFunction + "()", StudyFunction + "()", IfFunction + "()", LawFunction + "()", ChangeLawFunction + "()",
            PriceFunction + "()", ChangeInflationFunction + "()",
        };

        /// <summary>Nombres que cambian el mundo: no se permiten en condiciones.</summary>
        public static readonly IReadOnlyCollection<string> EffectOnlyNames = new SortedSet<string>(StringComparer.Ordinal)
        {
            PlantFunction + "()", JournalFunction + "()", StudyFunction + "()", ChangeLawFunction + "()", ChangeInflationFunction + "()",
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
                    SeedTypeDefinition type = Content.GetSeedType(SingleText(name, args));
                    PlantOrStrengthen(type);
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
                case ChangeInflationFunction:
                    RequireWritable(name);
                    ChangeInflation(SingleText(name, args));
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
        private void PlantOrStrengthen(SeedTypeDefinition type)
        {
            Seed? existing = World.Seeds.OpenFor(Subject.Id).FirstOrDefault(s => s.TypeId == type.Id);
            if (existing != null)
            {
                existing.Strengthen(type.Strength);
                return;
            }

            World.Seeds.Plant(type.ToSeedDefinition(Content), Subject.Id, World.Date, Subject.AgeInYears(World.Date));
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
