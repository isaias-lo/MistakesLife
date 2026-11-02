using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Events;
using MistakesLife.Core.Scripting;
using Newtonsoft.Json;

namespace MistakesLife.Core.Content
{
    /// <summary>
    /// Todo el contenido cargado de los paquetes: países, carreras, eventos, semillas y textos.
    /// Se llena con Add* y se cierra con Build(), que lee las condiciones y
    /// revisa que todo esté bien antes de jugar.
    /// </summary>
    public sealed class ContentDatabase
    {
        private readonly SortedDictionary<string, EventDefinition> _events = new SortedDictionary<string, EventDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, SeedTypeDefinition> _seedTypes = new SortedDictionary<string, SeedTypeDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, CountryDefinition> _countries = new SortedDictionary<string, CountryDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, CareerDefinition> _careers = new SortedDictionary<string, CareerDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CareerLevelDefinition> _careerLevels = new Dictionary<string, CareerLevelDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, Dictionary<string, string>> _texts = new SortedDictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private readonly List<ContentError> _errors = new List<ContentError>();
        private bool _built;

        public IEnumerable<EventDefinition> Events => _events.Values;

        public IEnumerable<SeedTypeDefinition> SeedTypes => _seedTypes.Values;

        public IEnumerable<CountryDefinition> Countries => _countries.Values;

        /// <summary>Las carreras, ordenadas por id.</summary>
        public IEnumerable<CareerDefinition> Careers => _careers.Values;

        public CareerLevelDefinition GetCareerLevel(string id) =>
            _careerLevels.TryGetValue(id, out CareerLevelDefinition? level) ? level : throw new KeyNotFoundException($"No existe el nivel de carrera '{id}'.");

        /// <summary>Idioma base: todo texto debe existir en este idioma.</summary>
        public const string BaseLanguage = "es";

        /// <summary>Claves de texto que usa el núcleo directamente.</summary>
        public static readonly IReadOnlyList<string> CoreTextKeys = new[] { "diario.nacimiento", "diario.cumpleanos", "diario.muerte", "diario.muerte_madre", "diario.muerte_padre", "diario.muerte_pareja", "diario.muerte_hija", "diario.muerte_hijo", "diario.nace_hija", "diario.nace_hijo" };

        /// <summary>Idiomas con textos cargados.</summary>
        public IEnumerable<string> Languages => _texts.Keys;

        /// <summary>
        /// El texto para el jugador. Si no existe en ese idioma, usa el idioma base;
        /// si tampoco, devuelve la clave (así se nota el texto faltante sin romper el juego).
        /// </summary>
        public string Text(string key, string language = BaseLanguage)
        {
            if (_texts.TryGetValue(language, out Dictionary<string, string>? table) && table.TryGetValue(key, out string? text))
            {
                return text;
            }

            if (language != BaseLanguage && _texts.TryGetValue(BaseLanguage, out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            return key;
        }

        /// <summary>
        /// Agrega textos de un idioma desde un archivo lang/&lt;idioma&gt;.json
        /// (un objeto clave → texto).
        /// </summary>
        public void AddTextsJson(string packId, string sourceName, string language, string json)
        {
            Dictionary<string, string>? entries;
            try
            {
                entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (JsonException ex)
            {
                _errors.Add(new ContentError(sourceName, "JSON no válido: " + ex.Message));
                return;
            }

            if (!_texts.TryGetValue(language, out Dictionary<string, string>? table))
            {
                table = new Dictionary<string, string>(StringComparer.Ordinal);
                _texts.Add(language, table);
            }

            foreach (KeyValuePair<string, string> entry in entries ?? new Dictionary<string, string>())
            {
                if (table.ContainsKey(entry.Key))
                {
                    _errors.Add(new ContentError(sourceName, $"el texto '{entry.Key}' ya existe en '{language}'."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    _errors.Add(new ContentError(sourceName, $"el texto '{entry.Key}' está vacío."));
                    continue;
                }

                table.Add(entry.Key, entry.Value);
            }
        }

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

        /// <summary>Agrega una carrera desde su archivo career.json.</summary>
        public void AddCareerJson(string packId, string sourceName, string json)
        {
            CareerDefinition? career;
            try
            {
                career = JsonConvert.DeserializeObject<CareerDefinition>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            }
            catch (JsonException ex)
            {
                _errors.Add(new ContentError(sourceName, "JSON no válido: " + ex.Message));
                return;
            }

            if (career == null)
            {
                _errors.Add(new ContentError(sourceName, "archivo vacío."));
                return;
            }

            career.PackId = packId;
            if (_careers.ContainsKey(career.Id))
            {
                _errors.Add(new ContentError(sourceName, $"la carrera {career.Id} ya existe."));
                return;
            }

            _careers.Add(career.Id, career);
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

            foreach (CareerDefinition career in _careers.Values)
            {
                ValidateCareer(career);
            }

            foreach (EventDefinition ev in _events.Values.Where(e => e.CareerOptions))
            {
                AddCareerOptions(ev);
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

                if (ev.AnnualProbability.HasValue && (ev.AnnualProbability <= 0 || ev.AnnualProbability > 1))
                {
                    _errors.Add(new ContentError(where, "probabilidad_anual debe ser mayor que 0 y como máximo 1."));
                }

                if (ev.AnnualProbability.HasValue && ev.SeedType != null)
                {
                    _errors.Add(new ContentError(where, "un evento de semilla no lleva probabilidad_anual: ocurre cuando la semilla despierta."));
                }

                if (ev.CooldownWeeks < 0)
                {
                    _errors.Add(new ContentError(where, "enfriamiento_semanas no puede ser negativo."));
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

            if (_texts.ContainsKey(BaseLanguage))
            {
                ValidateTexts();
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

            int previousAge = -1;
            foreach (MortalityRateDefinition row in country.Mortality)
            {
                string rowWhere = $"{where}, mortalidad edad {row.Age}";
                if (row.Age <= previousAge)
                {
                    _errors.Add(new ContentError(rowWhere, "las edades deben ir de menor a mayor, sin repetirse."));
                }

                if (row.Female < 0 || row.Female > 1 || row.Male < 0 || row.Male > 1)
                {
                    _errors.Add(new ContentError(rowWhere, "femenino y masculino son probabilidades: deben estar entre 0 y 1."));
                }

                previousAge = row.Age;
            }
        }

        private void ValidateCareer(CareerDefinition career)
        {
            string where = $"carrera {career.Id}";
            CheckId(where, career.Id, "carrera.");
            if (career.Levels.Count == 0)
            {
                _errors.Add(new ContentError(where, "debe tener al menos un nivel."));
            }

            foreach (CareerLevelDefinition level in career.Levels)
            {
                string levelWhere = $"{where}, {level.Id}";
                level.CareerId = career.Id;
                CheckId(levelWhere, level.Id, career.Id + ".nivel.");
                if (_careerLevels.ContainsKey(level.Id))
                {
                    _errors.Add(new ContentError(levelWhere, "id de nivel repetido."));
                    continue;
                }

                _careerLevels.Add(level.Id, level);
                if (!CareerLevelDefinition.Institutions.Contains(level.Institution))
                {
                    _errors.Add(new ContentError(levelWhere, $"institucion debe ser una de: {string.Join(", ", CareerLevelDefinition.Institutions)}."));
                }

                if (level.Semesters <= 0)
                {
                    _errors.Add(new ContentError(levelWhere, "semestres debe ser mayor que 0."));
                }
            }
        }

        /// <summary>
        /// Agrega a un evento con "opciones_carreras" una opción por cada nivel de cada
        /// carrera, antes de sus opciones propias. El orden es fijo (por id de carrera y
        /// orden de niveles) para que las partidas se puedan repetir.
        /// </summary>
        private void AddCareerOptions(EventDefinition ev)
        {
            var careerOptions = new List<EventOptionDefinition>();
            foreach (CareerDefinition career in _careers.Values)
            {
                foreach (CareerLevelDefinition level in career.Levels)
                {
                    careerOptions.Add(new EventOptionDefinition
                    {
                        TextKey = level.Id,
                        Conditions = level.Conditions,
                        Effects = new List<string> { $"{EventScope.StudyFunction}('{level.Id}')" },
                    });
                }
            }

            ev.Options.InsertRange(0, careerOptions);
        }

        /// <summary>Todas las claves de texto que el contenido usa, con dónde se usan.</summary>
        public IEnumerable<KeyValuePair<string, string>> UsedTextKeys()
        {
            foreach (string key in CoreTextKeys)
            {
                yield return new KeyValuePair<string, string>(key, "núcleo");
            }

            foreach (CountryDefinition country in _countries.Values)
            {
                foreach (EducationStageDefinition stage in country.EducationStages)
                {
                    yield return new KeyValuePair<string, string>("diario." + stage.Id, $"país {country.Id}");
                    yield return new KeyValuePair<string, string>(stage.Id, $"país {country.Id}");
                }

                foreach (CityDefinition city in country.Cities)
                {
                    yield return new KeyValuePair<string, string>(city.Id, $"país {country.Id}");
                    foreach (CommuneDefinition commune in city.Communes)
                    {
                        yield return new KeyValuePair<string, string>(commune.Id, $"país {country.Id}");
                    }
                }

                yield return new KeyValuePair<string, string>(country.Id, $"país {country.Id}");
            }

            foreach (CareerDefinition career in _careers.Values)
            {
                yield return new KeyValuePair<string, string>(career.Id, $"carrera {career.Id}");
                foreach (CareerLevelDefinition level in career.Levels)
                {
                    yield return new KeyValuePair<string, string>(level.Id, $"carrera {career.Id}");
                    yield return new KeyValuePair<string, string>(level.TitleTextKey, $"carrera {career.Id}");
                }
            }

            foreach (EventDefinition ev in _events.Values)
            {
                string where = $"evento {ev.Id}";
                yield return new KeyValuePair<string, string>(ev.ResolvedTextKey, where);
                foreach (EventOptionDefinition option in ev.Options)
                {
                    yield return new KeyValuePair<string, string>(option.TextKey, where);
                }

                IEnumerable<Effect> effects = ev.CompiledEffects.Concat(ev.Options.SelectMany(o => o.CompiledEffects));
                foreach (ScriptReference reference in effects.SelectMany(e => e.References()))
                {
                    if (reference.Function == EventScope.JournalFunction)
                    {
                        yield return new KeyValuePair<string, string>(reference.Argument, where);
                    }
                }
            }
        }

        private void ValidateTexts()
        {
            Dictionary<string, string> table = _texts[BaseLanguage];
            foreach (var used in UsedTextKeys().GroupBy(k => k.Key))
            {
                if (!table.ContainsKey(used.Key))
                {
                    _errors.Add(new ContentError(used.First().Value, $"falta el texto '{used.Key}' en el idioma '{BaseLanguage}'."));
                }
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

                if (reference.Function == EventScope.StudyFunction && !_careerLevels.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): ese nivel de carrera no existe."));
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
