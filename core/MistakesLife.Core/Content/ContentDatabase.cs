using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Events;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Todo el contenido cargado de los paquetes: eventos y tipos de semilla.
    /// Se llena con Add* y se cierra con Build(), que lee las condiciones y
    /// revisa que todo esté bien antes de jugar.
    /// </summary>
    public sealed class ContentDatabase
    {
        private readonly SortedDictionary<string, EventDefinition> _events = new SortedDictionary<string, EventDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, SeedTypeDefinition> _seedTypes = new SortedDictionary<string, SeedTypeDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, CountryDefinition> _countries = new SortedDictionary<string, CountryDefinition>(StringComparer.Ordinal);
        private readonly List<ContentError> _errors = new List<ContentError>();
        private bool _built;

        public IEnumerable<EventDefinition> Events => _events.Values;

        public IEnumerable<SeedTypeDefinition> SeedTypes => _seedTypes.Values;

        public IEnumerable<CountryDefinition> Countries => _countries.Values;

        public CountryDefinition GetCountry(string id) =>
            _countries.TryGetValue(id, out CountryDefinition? definition) ? definition : throw new KeyNotFoundException($"No existe el país '{id}'.");

        public bool TryGetEvent(string id, out EventDefinition? definition) => _events.TryGetValue(id, out definition);

        public EventDefinition GetEvent(string id) =>
            _events.TryGetValue(id, out EventDefinition? definition) ? definition : throw new KeyNotFoundException($"No existe el evento '{id}'.");

        public SeedTypeDefinition GetSeedType(string id) =>
            _seedTypes.TryGetValue(id, out SeedTypeDefinition? definition) ? definition : throw new KeyNotFoundException($"No existe la semilla '{id}'.");

        public bool HasSeedType(string id) => _seedTypes.ContainsKey(id);

        /// <summary>Agrega los eventos de un archivo JSON (una lista de eventos).</summary>
        public void AddEventsJson(string packId, string sourceName, string json)
        {
            foreach (EventDefinition definition in Deserialize<EventDefinition>(sourceName, json))
            {
                definition.PackId = packId;
                if (_events.ContainsKey(definition.Id))
                {
                    _errors.Add(new ContentError($"{sourceName} [{definition.Id}]", "id de evento repetido."));
                    continue;
                }

                _events.Add(definition.Id, definition);
            }
        }

        /// <summary>Agrega un país desde su archivo country.json.</summary>
        public void AddCountryJson(string packId, string sourceName, string json)
        {
            CountryDefinition? country;
            try
            {
                country = JsonConvert.DeserializeObject<CountryDefinition>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            }
            catch (JsonException ex)
            {
                _errors.Add(new ContentError(sourceName, "JSON no válido: " + ex.Message));
                return;
            }

            if (country == null)
            {
                _errors.Add(new ContentError(sourceName, "archivo vacío."));
                return;
            }

            country.PackId = packId;
            if (_countries.ContainsKey(country.Id))
            {
                _errors.Add(new ContentError(sourceName, $"el país {country.Id} ya existe."));
                return;
            }

            _countries.Add(country.Id, country);
        }

        /// <summary>Agrega los tipos de semilla de un archivo JSON (una lista).</summary>
        public void AddSeedsJson(string packId, string sourceName, string json)
        {
            foreach (SeedTypeDefinition definition in Deserialize<SeedTypeDefinition>(sourceName, json))
            {
                definition.PackId = packId;
                if (_seedTypes.ContainsKey(definition.Id))
                {
                    _errors.Add(new ContentError($"{sourceName} [{definition.Id}]", "id de semilla repetido."));
                    continue;
                }

                _seedTypes.Add(definition.Id, definition);
            }
        }

        /// <summary>
        /// Lee todas las condiciones y efectos y revisa los ids. Si hay errores,
        /// lanza ContentLoadException con la lista completa.
        /// </summary>
        public ContentDatabase Build()
        {
            if (_built)
            {
                throw new InvalidOperationException("El contenido ya se armó.");
            }

            foreach (CountryDefinition country in _countries.Values)
            {
                ValidateCountry(country);
            }

            foreach (SeedTypeDefinition seed in _seedTypes.Values)
            {
                string where = $"semilla {seed.Id}";
                CheckId(where, seed.Id, "semilla.");
                try
                {
                    seed.ParsePolarity();
                }
                catch (FormatException ex)
                {
                    _errors.Add(new ContentError(where, ex.Message));
                }

                if (seed.Strength < 0 || seed.Strength > 100)
                {
                    _errors.Add(new ContentError(where, "la fuerza debe estar entre 0 y 100."));
                }

                if (seed.ExpiresAfterYears <= 0)
                {
                    _errors.Add(new ContentError(where, "vence_anios debe ser mayor que 0."));
                }

                seed.CompiledConditions = CompileCondition(where, seed.Conditions);
            }

            foreach (EventDefinition ev in _events.Values)
            {
                string where = $"evento {ev.Id}";
                CheckId(where, ev.Id, "evento.");
                if (ev.Weight <= 0)
                {
                    _errors.Add(new ContentError(where, "el peso debe ser mayor que 0."));
                }

                if (ev.SeedType != null)
                {
                    if (_seedTypes.TryGetValue(ev.SeedType, out SeedTypeDefinition? seedType))
                    {
                        seedType.PossibleEvents.Add(ev.Id);
                    }
                    else
                    {
                        _errors.Add(new ContentError(where, $"la semilla '{ev.SeedType}' no existe."));
                    }
                }

                ev.CompiledConditions = CompileCondition(where, ev.Conditions);
                CompileEffects(where, ev.Effects, ev.CompiledEffects);
                for (int i = 0; i < ev.Options.Count; i++)
                {
                    EventOptionDefinition option = ev.Options[i];
                    string optionWhere = $"{where}, opción {i + 1}";
                    option.CompiledConditions = CompileCondition(optionWhere, option.Conditions);
                    CompileEffects(optionWhere, option.Effects, option.CompiledEffects);
                }
            }

            foreach (SeedTypeDefinition seed in _seedTypes.Values.Where(s => s.PossibleEvents.Count == 0))
            {
                _errors.Add(new ContentError($"semilla {seed.Id}", "ningún evento la usa: al despertar no pasaría nada."));
            }

            if (_errors.Count > 0)
            {
                throw new ContentLoadException(_errors.ToList());
            }

            _built = true;
            return this;
        }

        private void ValidateCountry(CountryDefinition country)
        {
            string where = $"país {country.Id}";
            CheckId(where, country.Id, "pais.");
            if (country.BirthWeight <= 0)
            {
                _errors.Add(new ContentError(where, "peso_nacimiento debe ser mayor que 0."));
            }

            if (country.MotherAgeMin < 12 || country.MotherAgeMax < country.MotherAgeMin)
            {
                _errors.Add(new ContentError(where, "edad_madre_min y edad_madre_max no son válidas."));
            }

            if (country.Names.Female.Count == 0 || country.Names.Male.Count == 0 || country.Names.Surnames.Count == 0)
            {
                _errors.Add(new ContentError(where, "faltan nombres femeninos, masculinos o apellidos."));
            }

            if (country.Cities.Count == 0)
            {
                _errors.Add(new ContentError(where, "debe tener al menos una ciudad."));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CityDefinition city in country.Cities)
            {
                string cityWhere = $"{where}, {city.Id}";
                CheckId(cityWhere, city.Id, "ciudad.");
                CheckUnique(cityWhere, city.Id, seen);
                if (city.BirthWeight <= 0)
                {
                    _errors.Add(new ContentError(cityWhere, "peso_nacimiento debe ser mayor que 0."));
                }

                if (city.Communes.Count == 0)
                {
                    _errors.Add(new ContentError(cityWhere, "debe tener al menos una comuna."));
                }

                foreach (CommuneDefinition commune in city.Communes)
                {
                    string communeWhere = $"{cityWhere}, {commune.Id}";
                    CheckId(communeWhere, commune.Id, "comuna.");
                    CheckUnique(communeWhere, commune.Id, seen);
                    if (commune.BirthWeight <= 0)
                    {
                        _errors.Add(new ContentError(communeWhere, "peso_nacimiento debe ser mayor que 0."));
                    }

                    if (commune.HouseholdLevelMin < 0 || commune.HouseholdLevelMax > 100 || commune.HouseholdLevelMin > commune.HouseholdLevelMax)
                    {
                        _errors.Add(new ContentError(communeWhere, "nivel_hogar_min y nivel_hogar_max deben estar entre 0 y 100, con min <= max."));
                    }
                }
            }

            int previousEnd = -1;
            foreach (EducationStageDefinition stage in country.EducationStages)
            {
                string stageWhere = $"{where}, {stage.Id}";
                CheckId(stageWhere, stage.Id, "etapa.");
                CheckUnique(stageWhere, stage.Id, seen);
                if (stage.ToAge <= stage.FromAge)
                {
                    _errors.Add(new ContentError(stageWhere, "edad_hasta debe ser mayor que edad_desde."));
                }

                if (stage.FromAge < previousEnd)
                {
                    _errors.Add(new ContentError(stageWhere, "se cruza con la etapa anterior; deben ir en orden y sin solaparse."));
                }

                previousEnd = stage.ToAge;
            }
        }

        private void CheckUnique(string where, string id, HashSet<string> seen)
        {
            if (!seen.Add(id))
            {
                _errors.Add(new ContentError(where, "id repetido dentro del país."));
            }
        }

        private Expression? CompileCondition(string where, string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            try
            {
                Expression expression = Expression.Parse(source!);
                CheckNames(where, expression.Names(), isEffect: false);
                CheckReferences(where, expression.References());
                return expression;
            }
            catch (ScriptException ex)
            {
                _errors.Add(new ContentError(where, $"condición \"{source}\": {ex.Message}"));
                return null;
            }
        }

        private void CompileEffects(string where, List<string> sources, List<Effect> compiled)
        {
            foreach (string source in sources)
            {
                try
                {
                    Effect effect = Effect.Parse(source);
                    CheckNames(where, effect.Names(), isEffect: true);
                    CheckReferences(where, effect.References());
                    compiled.Add(effect);
                }
                catch (ScriptException ex)
                {
                    _errors.Add(new ContentError(where, $"efecto \"{source}\": {ex.Message}"));
                }
            }
        }

        private void CheckNames(string where, IEnumerable<string> names, bool isEffect)
        {
            foreach (string name in names.Distinct())
            {
                if (!EventScope.KnownNames.Contains(name))
                {
                    _errors.Add(new ContentError(where, $"'{name}' no existe en el lenguaje. Disponibles: {string.Join(", ", EventScope.KnownNames)}."));
                }
                else if (!isEffect && EventScope.EffectOnlyNames.Contains(name))
                {
                    _errors.Add(new ContentError(where, $"'{name}' cambia el mundo y solo puede usarse en efectos, no en condiciones."));
                }
            }
        }

        private void CheckReferences(string where, IEnumerable<ScriptReference> references)
        {
            foreach (ScriptReference reference in references)
            {
                if ((reference.Function == EventScope.SeedFunction || reference.Function == EventScope.PlantFunction) && !_seedTypes.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): esa semilla no existe."));
                }
            }
        }

        private void CheckId(string where, string id, string prefix)
        {
            if (!id.StartsWith(prefix, StringComparison.Ordinal) || id.Any(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
            {
                _errors.Add(new ContentError(where, $"el id debe empezar con '{prefix}' y estar en minúsculas sin espacios."));
            }
        }

        private IEnumerable<T> Deserialize<T>(string sourceName, string json)
        {
            List<T>? items;
            try
            {
                items = JsonConvert.DeserializeObject<List<T>>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            }
            catch (JsonException ex)
            {
                _errors.Add(new ContentError(sourceName, "JSON no válido: " + ex.Message));
                return Array.Empty<T>();
            }

            return items ?? new List<T>();
        }
    }
}
