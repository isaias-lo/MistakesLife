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
        private readonly SortedDictionary<string, NewsDefinition> _news = new SortedDictionary<string, NewsDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, SceneObjectDefinition> _sceneObjects = new SortedDictionary<string, SceneObjectDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, AppDefinition> _apps = new SortedDictionary<string, AppDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, MailDefinition> _mails = new SortedDictionary<string, MailDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<MailDefinition>> _mailsByJournal = new Dictionary<string, List<MailDefinition>>(StringComparer.Ordinal);
        private readonly List<MailDefinition> _periodicMails = new List<MailDefinition>();
        private readonly SortedDictionary<string, CountryDefinition> _countries = new SortedDictionary<string, CountryDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, CareerDefinition> _careers = new SortedDictionary<string, CareerDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CareerLevelDefinition> _careerLevels = new Dictionary<string, CareerLevelDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CareerAreaDefinition> _careerAreas = new Dictionary<string, CareerAreaDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CareerPositionDefinition> _careerPositions = new Dictionary<string, CareerPositionDefinition>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, Dictionary<string, string>> _texts = new SortedDictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private readonly List<ContentError> _errors = new List<ContentError>();
        private bool _built;

        /// <summary>
        /// Mientras se lee un evento del paquete de un país, ese país: sus condiciones
        /// y efectos solo se revisan contra él (los demás países no tienen por qué
        /// definir sus deudas o leyes). Null: se revisan contra todos.
        /// </summary>
        private CountryDefinition? _checkedCountry;

        public IEnumerable<EventDefinition> Events => _events.Values;

        public IEnumerable<SeedTypeDefinition> SeedTypes => _seedTypes.Values;

        /// <summary>Los objetos de la escena 3D que dependen de la vida del jugador, ordenados por id.</summary>
        public IEnumerable<SceneObjectDefinition> SceneObjects => _sceneObjects.Values;

        /// <summary>Las noticias del mundo, ordenadas por id.</summary>
        public IEnumerable<NewsDefinition> News => _news.Values;

        public bool TryGetNews(string id, out NewsDefinition? definition) => _news.TryGetValue(id, out definition);

        public IEnumerable<CountryDefinition> Countries => _countries.Values;

        /// <summary>Las apps del celular, ordenadas por id.</summary>
        public IEnumerable<AppDefinition> Apps => _apps.Values;

        /// <summary>Los correos que pueden llegar, ordenados por id.</summary>
        public IEnumerable<MailDefinition> Mails => _mails.Values;

        /// <summary>La app con ese id, o null.</summary>
        public AppDefinition? App(string id) => _apps.TryGetValue(id, out AppDefinition? app) ? app : null;

        /// <summary>El correo con ese id, o null.</summary>
        public MailDefinition? Mail(string id) => _mails.TryGetValue(id, out MailDefinition? mail) ? mail : null;

        /// <summary>Los correos que llegan cuando se anota esa línea en el diario (ordenados por id).</summary>
        /// <summary>Los correos periódicos (con "cada"), en orden de id.</summary>
        public IReadOnlyList<MailDefinition> PeriodicMails => _periodicMails;

        public IReadOnlyList<MailDefinition> MailsForJournal(string journalKey) =>
            _mailsByJournal.TryGetValue(journalKey, out List<MailDefinition>? mails) ? mails : (IReadOnlyList<MailDefinition>)Array.Empty<MailDefinition>();

        /// <summary>Las carreras, ordenadas por id.</summary>
        public IEnumerable<CareerDefinition> Careers => _careers.Values;

        public CareerDefinition GetCareer(string id) =>
            _careers.TryGetValue(id, out CareerDefinition? career) ? career : throw new KeyNotFoundException($"No existe la carrera '{id}'.");

        public CareerLevelDefinition GetCareerLevel(string id) =>
            _careerLevels.TryGetValue(id, out CareerLevelDefinition? level) ? level : throw new KeyNotFoundException($"No existe el nivel de carrera '{id}'.");

        public CareerAreaDefinition GetCareerArea(string id) =>
            _careerAreas.TryGetValue(id, out CareerAreaDefinition? area) ? area : throw new KeyNotFoundException($"No existe el área de carrera '{id}'.");

        public bool TryGetCareerArea(string id, out CareerAreaDefinition? area) => _careerAreas.TryGetValue(id, out area);

        public CareerPositionDefinition GetCareerPosition(string id) =>
            _careerPositions.TryGetValue(id, out CareerPositionDefinition? position) ? position : throw new KeyNotFoundException($"No existe el cargo '{id}'.");

        /// <summary>Idioma base: todo texto debe existir en este idioma.</summary>
        public const string BaseLanguage = "es";

        /// <summary>Claves de texto que usa el núcleo directamente.</summary>
        public static readonly IReadOnlyList<string> CoreTextKeys = new[] { "diario.nacimiento", "diario.cumpleanos", "diario.muerte", "diario.muerte_madre", "diario.muerte_padre", "diario.muerte_pareja", "diario.muerte_hija", "diario.muerte_hijo", "diario.nace_hija", "diario.nace_hijo", "diario.legado", "diario.nace_nieta", "diario.nace_nieto", "diario.adopcion_hija", "diario.adopcion_hijo", "diario.llega_nieta", "diario.llega_nieto", Game.LifeSession.JournalIntuition, Game.LifeSession.JournalIntuitionDecision }
            .Concat(Game.LifeStages.All).ToArray();

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

            // Una variante (es-CL) sin ese texto usa el idioma general (es).
            string general = GeneralLanguage(language);
            if (general != language && _texts.TryGetValue(general, out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            if (general != BaseLanguage && _texts.TryGetValue(BaseLanguage, out table) && table.TryGetValue(key, out text))
            {
                return text;
            }

            return key;
        }

        /// <summary>
        /// El idioma que se usa en un país: su variante si la tiene (para "es" en Chile,
        /// "es-CL"), o el mismo idioma si no.
        /// </summary>
        public string LanguageFor(string? countryId, string language = BaseLanguage)
        {
            if (countryId != null && _countries.TryGetValue(countryId, out CountryDefinition? country)
                && country.LanguageVariants.TryGetValue(language, out string? variant))
            {
                return variant;
            }

            return language;
        }

        /// <summary>El idioma general de una variante: "es-CL" → "es".</summary>
        public static string GeneralLanguage(string language)
        {
            int dash = language.IndexOf('-');
            return dash > 0 ? language.Substring(0, dash) : language;
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

        /// <summary>Agrega las noticias de un archivo JSON de news/ (una lista).</summary>
        public void AddNewsJson(string packId, string sourceName, string json)
        {
            foreach (NewsDefinition definition in Deserialize<NewsDefinition>(sourceName, json))
            {
                definition.PackId = packId;
                if (_news.ContainsKey(definition.Id))
                {
                    _errors.Add(new ContentError($"{sourceName} [{definition.Id}]", "id de noticia repetido."));
                    continue;
                }

                _news.Add(definition.Id, definition);
            }
        }

        /// <summary>Agrega los objetos de la escena de un archivo JSON de scene/ (una lista).</summary>
        public void AddSceneJson(string packId, string sourceName, string json)
        {
            foreach (SceneObjectDefinition definition in Deserialize<SceneObjectDefinition>(sourceName, json))
            {
                definition.PackId = packId;
                if (_sceneObjects.ContainsKey(definition.Id))
                {
                    _errors.Add(new ContentError($"{sourceName} [{definition.Id}]", "id de objeto de escena repetido."));
                    continue;
                }

                _sceneObjects.Add(definition.Id, definition);
            }
        }

        /// <summary>Agrega las apps del celular de un archivo JSON de apps/ (una lista).</summary>
        public void AddAppsJson(string packId, string sourceName, string json)
        {
            foreach (AppDefinition definition in Deserialize<AppDefinition>(sourceName, json))
            {
                definition.PackId = packId;
                if (_apps.ContainsKey(definition.Id))
                {
                    _errors.Add(new ContentError($"{sourceName} [{definition.Id}]", "id de app repetido."));
                    continue;
                }

                _apps.Add(definition.Id, definition);
            }
        }

        /// <summary>Agrega los correos de un archivo JSON de mail/ (una lista).</summary>
        public void AddMailJson(string packId, string sourceName, string json)
        {
            foreach (MailDefinition definition in Deserialize<MailDefinition>(sourceName, json))
            {
                definition.PackId = packId;
                if (_mails.ContainsKey(definition.Id))
                {
                    _errors.Add(new ContentError($"{sourceName} [{definition.Id}]", "id de correo repetido."));
                    continue;
                }

                _mails.Add(definition.Id, definition);
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

            // Los requisitos se leen después de registrar todas las áreas y cargos,
            // porque pueden nombrar los de cualquier carrera.
            foreach (CareerAreaDefinition area in _careerAreas.Values)
            {
                area.CompiledRequirements = CompileCondition($"carrera {area.CareerId}, {area.Id}", area.Requirements);
            }

            foreach (CareerPositionDefinition position in _careerPositions.Values)
            {
                position.CompiledRequirements = CompileCondition($"carrera {position.CareerId}, {position.Id}", position.Requirements);
            }

            foreach (EventDefinition ev in _events.Values.Where(e => e.CareerOptions))
            {
                AddCareerOptions(ev);
            }

            // Los eventos del paquete de un país solo le pasan a quien vive en ese país.
            Dictionary<string, string> countryOfPack = _countries.Values.Where(c => c.PackId.Length > 0).ToDictionary(c => c.PackId, c => c.Id, StringComparer.Ordinal);
            foreach (EventDefinition ev in _events.Values)
            {
                ev.CountryId = countryOfPack.TryGetValue(ev.PackId, out string? countryId) ? countryId : null;
            }

            foreach (NewsDefinition news in _news.Values)
            {
                news.CountryId = countryOfPack.TryGetValue(news.PackId, out string? countryId) ? countryId : null;
                ValidateNews(news);
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

            foreach (AppDefinition app in _apps.Values)
            {
                CheckId($"app {app.Id}", app.Id, "app.");
            }

            ValidateMails();

            foreach (SceneObjectDefinition scene in _sceneObjects.Values)
            {
                string where = $"objeto de escena {scene.Id}";
                if (Array.IndexOf(SceneObjectDefinition.Places, scene.Place) < 0)
                {
                    _errors.Add(new ContentError(where, $"lugar '{scene.Place}' no existe (usa {string.Join(", ", SceneObjectDefinition.Places)})."));
                }
                else
                {
                    CheckId(where, scene.Id, $"escena.{scene.Place}.");
                }

                scene.CompiledConditions = CompileCondition(where, scene.Conditions);
                if (scene.CompiledConditions == null && string.IsNullOrWhiteSpace(scene.Conditions))
                {
                    _errors.Add(new ContentError(where, "falta la condición."));
                }
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

                if (ev.MaxTimes.HasValue && ev.MaxTimes <= 0)
                {
                    _errors.Add(new ContentError(where, "veces_maximas debe ser mayor que 0."));
                }

                if (ev.Variants.Any(string.IsNullOrWhiteSpace) || ev.AllTextKeys.Distinct().Count() != ev.Variants.Count + 1)
                {
                    _errors.Add(new ContentError(where, "las variantes no pueden estar vacías ni repetir el texto principal u otra variante."));
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

                _checkedCountry = ev.CountryId == null ? null : _countries[ev.CountryId];
                ev.CompiledConditions = CompileCondition(where, ev.Conditions);
                CompileEffects(where, ev.Effects, ev.CompiledEffects);
                for (int i = 0; i < ev.Options.Count; i++)
                {
                    EventOptionDefinition option = ev.Options[i];
                    string optionWhere = $"{where}, opción {i + 1}";
                    option.CompiledConditions = CompileCondition(optionWhere, option.Conditions);
                    option.CompiledTendency = CompileCondition(optionWhere, option.Tendency);
                    CompileEffects(optionWhere, option.Effects, option.CompiledEffects);
                }
            }

            _checkedCountry = null;

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

        /// <summary>Lo que se usa en una noticia: solo el mundo, nunca el jugador.</summary>
        private static readonly string[] NewsEffectNames =
        {
            EventScope.ChangeCycleFunction + "()", EventScope.ChangeInflationFunction + "()", EventScope.ChangeLawFunction + "()", EventScope.IfFunction + "()", EventScope.LawFunction + "()", "mundo",
        };

        private void ValidateNews(NewsDefinition news)
        {
            string where = $"noticia {news.Id}";
            CheckId(where, news.Id, "noticia.");
            if (Array.IndexOf(NewsDefinition.Topics, news.Topic) < 0)
            {
                _errors.Add(new ContentError(where, $"tema '{news.Topic}' no existe (usa {string.Join(", ", NewsDefinition.Topics)})."));
            }

            if (news.AnnualProbability <= 0 || news.AnnualProbability > 1)
            {
                _errors.Add(new ContentError(where, "probabilidad_anual debe ser mayor que 0 y como máximo 1."));
            }

            if (news.CooldownWeeks < 0)
            {
                _errors.Add(new ContentError(where, "enfriamiento_semanas no puede ser negativo."));
            }

            if (news.Variants.Any(string.IsNullOrWhiteSpace) || news.AllTextKeys.Distinct().Count() != news.Variants.Count + 1)
            {
                _errors.Add(new ContentError(where, "las variantes no pueden estar vacías ni repetir el titular u otra variante."));
            }

            IEnumerable<CountryDefinition> countries = news.CountryId == null ? _countries.Values : new[] { _countries[news.CountryId] };
            foreach (string city in news.Cities.Where(c => !countries.Any(country => country.Cities.Any(k => k.Id == c))))
            {
                _errors.Add(new ContentError(where, $"la ciudad '{city}' no existe en {(news.CountryId ?? "ningún país")}."));
            }

            _checkedCountry = news.CountryId == null ? null : _countries[news.CountryId];
            news.CompiledConditions = CompileCondition(where, news.Conditions);
            if (news.CompiledConditions != null && news.CompiledConditions.Names().Contains("jugador"))
            {
                _errors.Add(new ContentError(where, "una noticia no depende del jugador: sus condiciones solo pueden leer el mundo (mundo.mes, mundo.ciclo, ley(...), noticia_reciente(...))."));
            }

            CompileEffects(where, news.Effects, news.CompiledEffects);
            foreach (string name in news.CompiledEffects.SelectMany(e => e.Names()).Distinct().Where(n => Array.IndexOf(NewsEffectNames, n) < 0))
            {
                _errors.Add(new ContentError(where, $"una noticia solo cambia el país (cambiar_ciclo, cambiar_inflacion, cambiar_ley), no '{name}': lo que le pasa al jugador va en un evento que lee noticia_reciente()."));
            }

            _checkedCountry = null;
        }

        private void ValidateCountry(CountryDefinition country)
        {
            string where = $"país {country.Id}";
            CheckId(where, country.Id, "pais.");
            if (country.BirthWeight < 0)
            {
                _errors.Add(new ContentError(where, "peso_nacimiento no puede ser negativo (0: solo se nace ahí si se elige)."));
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

                    if (commune.Rents.Values.Any(r => r < 0))
                    {
                        _errors.Add(new ContentError(communeWhere, "el arriendo no puede ser negativo."));
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

            foreach (PriorLifeDefinition prior in country.PriorLife)
            {
                if (prior.Age < 0 || prior.Flags.Count == 0 || prior.Flags.Any(string.IsNullOrWhiteSpace))
                {
                    _errors.Add(new ContentError($"{where}, vida_previa edad {prior.Age}", "la edad no puede ser negativa y debe tener al menos una marca no vacía."));
                }

                if (prior.Housing != null && country.Economy?.Housing?.Type(prior.Housing) == null)
                {
                    _errors.Add(new ContentError($"{where}, vida_previa edad {prior.Age}", $"la vivienda '{prior.Housing}' no está en economia.vivienda.tipos."));
                }

                if (prior.GroceryBasket != null && country.Economy?.Groceries?.Basket(prior.GroceryBasket) == null)
                {
                    _errors.Add(new ContentError($"{where}, vida_previa edad {prior.Age}", $"la compra '{prior.GroceryBasket}' no está en economia.supermercado.compras."));
                }
            }

            foreach (KeyValuePair<string, Newtonsoft.Json.Linq.JToken> law in country.LawsJson)
            {
                string lawWhere = $"{where}, {law.Key}";
                CheckId(lawWhere, law.Key, "ley.");
                if (CountryDefinition.LawValue(law.Value) == null)
                {
                    _errors.Add(new ContentError(lawWhere, "el valor de una ley debe ser sí/no (true/false), un número o un texto."));
                }
            }

            if (country.Diversity != null)
            {
                CheckWeights($"{where}, diversidad.orientaciones", country.Diversity.Orientations, DiversityDefinition.OrientationKeys);
                CheckWeights($"{where}, diversidad.identidades", country.Diversity.Identities, DiversityDefinition.IdentityKeys);
            }

            if (country.Economy != null)
            {
                ValidateInflation($"{where}, economia.inflacion", country.Economy.Inflation);
                if (country.Economy.Cycle != null)
                {
                    ValidateCycle($"{where}, economia.ciclo", country.Economy.Cycle, country.Economy.Inflation);
                }

                ValidatePayroll($"{where}, economia", country.Economy);
                ValidateHousing($"{where}, economia.vivienda", country);
                ValidateGroceries($"{where}, economia.supermercado", country.Economy.Groceries);
                ValidateServices($"{where}, economia.servicios", country.Economy.Services);
                if (country.Economy.Companies != null)
                {
                    ValidateCompanies($"{where}, economia.empresas", country.Economy.Companies);
                }

                if (country.Economy.JobSearch != null)
                {
                    ValidateJobSearch($"{where}, economia.busqueda_trabajo", country.Economy.JobSearch);
                }
            }

            FamilyRatesDefinition? family = country.NonPlayedFamily;
            if (family != null)
            {
                bool badRate = new[] { family.PartnerPerYear, family.ChildPerYear, family.SeparationPerYear, family.AdoptionPerYear }.Any(r => r < 0 || r > 1);
                if (badRate || family.PartnerMinAge < 12 || family.FertileMaxAge < family.PartnerMinAge || family.MaxChildren < 0)
                {
                    _errors.Add(new ContentError($"{where}, familia_no_jugada", "las probabilidades van de 0 a 1, edad_pareja_min desde 12, edad_fertil_max mayor que edad_pareja_min y max_hijos no negativo."));
                }
            }
        }

        private void ValidateServices(string where, List<ServiceDefinition> services)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ServiceDefinition service in services)
            {
                string serviceWhere = $"{where}, {service.Id}";
                CheckId(serviceWhere, service.Id, "servicio.");
                if (!seen.Add(service.Id))
                {
                    _errors.Add(new ContentError(serviceWhere, "servicio repetido."));
                }

                service.CompiledConditions = CompileCondition(serviceWhere, service.Conditions);
                if (service.Options.Count == 0)
                {
                    _errors.Add(new ContentError(serviceWhere, "un servicio necesita al menos una opción."));
                }

                foreach (ServiceOptionDefinition option in service.Options)
                {
                    if (!option.Id.StartsWith(service.Id + ".", StringComparison.Ordinal))
                    {
                        _errors.Add(new ContentError($"{serviceWhere}, {option.Id}", $"el id de la opción debe empezar con '{service.Id}.'."));
                    }

                    if (!seen.Add(option.Id))
                    {
                        _errors.Add(new ContentError($"{serviceWhere}, {option.Id}", "opción repetida."));
                    }

                    if (option.Price < 0)
                    {
                        _errors.Add(new ContentError($"{serviceWhere}, {option.Id}", "el precio no puede ser negativo."));
                    }
                }
            }
        }

        private void ValidateGroceries(string where, GroceriesDefinition? groceries)
        {
            if (groceries == null)
            {
                return;
            }

            if (groceries.ChildFactor < 0 || groceries.ChildFactor > 2)
            {
                _errors.Add(new ContentError(where, "factor_hijo va de 0 a 2 (1 = un hijo come lo mismo que un adulto)."));
            }

            var seen = new HashSet<string>();
            foreach (GroceryBasketDefinition basket in groceries.Baskets)
            {
                CheckId($"{where}, {basket.Id}", basket.Id, "compra.");
                if (!seen.Add(basket.Id))
                {
                    _errors.Add(new ContentError($"{where}, {basket.Id}", "compra repetida."));
                }

                if (basket.PerPerson < 0)
                {
                    _errors.Add(new ContentError($"{where}, {basket.Id}", "costo_persona no puede ser negativo."));
                }
            }
        }

        private void ValidateHousing(string where, CountryDefinition country)
        {
            HousingDefinition? housing = country.Economy?.Housing;
            List<CommuneDefinition> communes = country.Cities.SelectMany(c => c.Communes).ToList();
            if (housing == null)
            {
                if (communes.Any(c => c.Rents.Count > 0))
                {
                    _errors.Add(new ContentError(where, "las comunas tienen arriendos pero el país no define economia.vivienda."));
                }

                return;
            }

            if (housing.TermMonths < 1 || housing.DepositMonths < 0 || housing.AdjustEveryMonths < 1)
            {
                _errors.Add(new ContentError(where, "plazo_meses y reajuste_meses desde 1; garantia_meses no negativo."));
            }

            var seenTypes = new HashSet<string>();
            foreach (HousingTypeDefinition type in housing.Types)
            {
                CheckId($"{where}, {type.Id}", type.Id, "vivienda.");
                if (!seenTypes.Add(type.Id))
                {
                    _errors.Add(new ContentError($"{where}, {type.Id}", "tipo de vivienda repetido."));
                }
            }

            foreach (CommuneDefinition commune in communes)
            {
                foreach (HousingTypeDefinition type in housing.Types.Where(t => commune.RentOf(t.Id) == null))
                {
                    _errors.Add(new ContentError($"{where}, {commune.Id}", $"la comuna no tiene el arriendo de {type.Id}. Toda comuna debe tener el precio de cada tipo de vivienda."));
                }

                foreach (string unknown in commune.Rents.Keys.Where(k => housing.Type(k) == null))
                {
                    _errors.Add(new ContentError($"{where}, {commune.Id}", $"el arriendo de '{unknown}' no es un tipo de economia.vivienda.tipos."));
                }
            }
        }

        private void ValidatePayroll(string where, EconomyDefinition economy)
        {
            SalaryDefinition? salaries = economy.Salaries;
            if (salaries != null && (salaries.Minimum < 0 || salaries.Typical < salaries.Minimum || salaries.NoDegreeRelative <= 0 || salaries.AdjustmentMonths < 1))
            {
                _errors.Add(new ContentError($"{where}.sueldos", "minimo no negativo, tipico no menor que minimo, relativo_sin_titulo mayor que 0 y reajuste_meses desde 1."));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (DeductionDefinition deduction in economy.Payroll.Employee.Concat(economy.Payroll.Freelance))
            {
                string deductionWhere = $"{where}.liquidacion, {deduction.Id}";
                CheckId(deductionWhere, deduction.Id, "descuento.");
                if (deduction.Rate < 0 || deduction.Rate > 1 || deduction.Cap < 0)
                {
                    _errors.Add(new ContentError(deductionWhere, "la tasa va de 0 a 1 y el tope no puede ser negativo."));
                }
            }

            foreach (List<DeductionDefinition> list in new[] { economy.Payroll.Employee, economy.Payroll.Freelance })
            {
                seen.Clear();
                foreach (DeductionDefinition deduction in list.Where(d => !seen.Add(d.Id)))
                {
                    _errors.Add(new ContentError($"{where}.liquidacion, {deduction.Id}", "descuento repetido."));
                }
            }

            CheckBrackets($"{where}.liquidacion.impuesto_mensual", economy.Payroll.MonthlyTax);

            ExpensesDefinition? expenses = economy.Expenses;
            if (expenses != null)
            {
                if (expenses.FromAge < 0 || expenses.HappinessInRed < 0)
                {
                    _errors.Add(new ContentError($"{where}.gastos", "edad_desde y felicidad_en_rojo no pueden ser negativos."));
                }

                var seenExpenses = new HashSet<string>(StringComparer.Ordinal);
                foreach (ExpenseDefinition expense in expenses.Categories)
                {
                    string expenseWhere = $"{where}.gastos, {expense.Id}";
                    CheckId(expenseWhere, expense.Id, "gasto.");
                    if (!seenExpenses.Add(expense.Id))
                    {
                        _errors.Add(new ContentError(expenseWhere, "gasto repetido."));
                    }

                    expense.CompiledAmount = CompileCondition(expenseWhere + " (monto)", expense.Amount);
                    expense.CompiledConditions = CompileCondition(expenseWhere, expense.Conditions);
                    if (expense.CompiledAmount == null && string.IsNullOrWhiteSpace(expense.Amount))
                    {
                        _errors.Add(new ContentError(expenseWhere, "falta el monto."));
                    }
                }

                if (expenses.DeficitLimit < 0 || (expenses.DeficitLimit.HasValue && expenses.DeficitDebt == null))
                {
                    _errors.Add(new ContentError($"{where}.gastos.cupo_deficit", "el cupo no puede ser negativo y necesita deuda_por_deficit."));
                }

                if (expenses.DeficitDebt != null && economy.Debts?.Type(expenses.DeficitDebt) == null)
                {
                    _errors.Add(new ContentError($"{where}.gastos.deuda_por_deficit", $"la deuda '{expenses.DeficitDebt}' no está en economia.deudas."));
                }
            }

            PensionsDefinition? pensions = economy.Pensions;
            PensionAccountDefinition? account = pensions?.IndividualAccount;
            if (account != null)
            {
                if (!economy.Payroll.Employee.Any(d => d.Id == account.Deduction))
                {
                    _errors.Add(new ContentError($"{where}.jubilacion.cuenta_individual", $"el descuento '{account.Deduction}' no está en liquidacion.dependiente."));
                }

                if (account.ShareToAccount < 0 || account.ShareToAccount > 1 || account.FreelanceRate < 0 || account.FreelanceRate > 1 || account.RealAnnualReturn <= -1 || account.PensionYears < 1)
                {
                    _errors.Add(new ContentError($"{where}.jubilacion.cuenta_individual", "parte_a_la_cuenta y tasa_honorarios de 0 a 1, rentabilidad_real_anual mayor que -1 y anos_de_pension desde 1."));
                }
            }

            PayAsYouGoPensionDefinition? payAsYouGo = pensions?.PayAsYouGo;
            if (payAsYouGo != null)
            {
                if (account != null)
                {
                    _errors.Add(new ContentError($"{where}.jubilacion", "un país tiene cuenta_individual o reparto, no los dos."));
                }

                if (!economy.Payroll.Employee.Any(d => d.Id == payAsYouGo.Deduction))
                {
                    _errors.Add(new ContentError($"{where}.jubilacion.reparto", $"el descuento '{payAsYouGo.Deduction}' no está en liquidacion.dependiente."));
                }

                if (payAsYouGo.MinimumYears < 0 || payAsYouGo.SharePerYear < 0 || payAsYouGo.AverageYears < 1 || payAsYouGo.UniversalBase < 0 || payAsYouGo.Minimum < 0)
                {
                    _errors.Add(new ContentError($"{where}.jubilacion.reparto", "anos_minimos, porcentaje_por_ano, base_universal y minima no negativos; anos_promedio desde 1."));
                }
            }

            UniversalPensionDefinition? universal = pensions?.UniversalPension;
            if (universal != null && (universal.Amount < 0 || universal.FullUpTo < 0 || (universal.FullUpTo.HasValue != universal.NoneFrom.HasValue) || universal.NoneFrom <= universal.FullUpTo))
            {
                _errors.Add(new ContentError($"{where}.jubilacion.pension_universal", "monto no negativo; completa_hasta y sin_pension_desde van juntos y el segundo es mayor."));
            }

            DebtsDefinition? debts = economy.Debts;
            if (debts != null)
            {
                if (debts.MonthsToDelinquent < 1 || debts.MonthsToPrescription < debts.MonthsToDelinquent)
                {
                    _errors.Add(new ContentError($"{where}.deudas", "meses_para_moroso desde 1 y meses_prescripcion no menor que meses_para_moroso."));
                }

                var seenDebts = new HashSet<string>(StringComparer.Ordinal);
                foreach (DebtTypeDefinition type in debts.Types)
                {
                    string debtWhere = $"{where}.deudas, {type.Id}";
                    CheckId(debtWhere, type.Id, "deuda.");
                    if (!seenDebts.Add(type.Id))
                    {
                        _errors.Add(new ContentError(debtWhere, "deuda repetida."));
                    }

                    if (type.AnnualRate < 0 || type.Installments < 1 || type.MinimumPaymentRate <= 0 || type.MinimumPaymentRate > 1
                        || type.MinimumPaymentAmount < 0 || type.RescheduleInstallments < 1)
                    {
                        _errors.Add(new ContentError(debtWhere, "tasa_anual no negativa, cuotas y cuotas_repactacion desde 1, pago_minimo mayor que 0 y hasta 1, pago_minimo_monto no negativo."));
                    }

                    type.CompiledPaymentCondition = CompileCondition(debtWhere + " (condicion_pago)", type.PaymentCondition);
                }
            }

            if (economy.Inheritance != null && economy.Inheritance.PartnerShares < 0)
            {
                _errors.Add(new ContentError($"{where}.herencia", "porciones_pareja no puede ser negativo."));
            }

            AnnualTaxDefinition? annual = economy.AnnualTax;
            if (annual != null)
            {
                CheckBrackets($"{where}.declaracion_anual.tramos", annual.Brackets);
                bool badRate = new[] { annual.FreelancePresumedExpenses, annual.FreelanceContributionBase, annual.FreelanceContributionRate, annual.LatePenalty }.Any(r => r < 0 || r > 1);
                if (annual.Month < 1 || annual.Month > 12 || badRate || annual.FreelancePresumedExpensesCap < 0)
                {
                    _errors.Add(new ContentError($"{where}.declaracion_anual", "mes de 1 a 12; gastos presuntos, base y tasa de cotización y multa entre 0 y 1; tope no negativo."));
                }
            }
        }

        private void CheckBrackets(string where, IReadOnlyList<TaxBracketDefinition> brackets)
        {
            for (int i = 0; i < brackets.Count; i++)
            {
                if (brackets[i].Rate < 0 || brackets[i].Rate > 1 || brackets[i].From < 0 || (i > 0 && brackets[i].From <= brackets[i - 1].From))
                {
                    _errors.Add(new ContentError(where, "los tramos van de menor a mayor (desde), sin repetirse, con tasa de 0 a 1."));
                    return;
                }
            }
        }

        private void ValidateCycle(string where, EconomicCycleDefinition cycle, InflationDefinition inflation)
        {
            if (cycle.Phase(cycle.InitialPhase) == null)
            {
                _errors.Add(new ContentError(where, $"fase_inicial '{cycle.InitialPhase}' no es una de sus fases."));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (EconomicPhaseDefinition phase in cycle.Phases)
            {
                string phaseWhere = $"{where}, {phase.Id}";
                CheckId(phaseWhere, phase.Id, "economia.ciclo.");
                if (!seen.Add(phase.Id))
                {
                    _errors.Add(new ContentError(phaseWhere, "fase repetida."));
                }

                if (phase.InflationRegime != null && inflation.Regime(phase.InflationRegime) == null)
                {
                    _errors.Add(new ContentError(phaseWhere, $"inflacion: '{phase.InflationRegime}' no es un régimen de inflación de este país."));
                }

                if (phase.PensionFundShock <= -1)
                {
                    _errors.Add(new ContentError(phaseWhere, "golpe_fondos debe ser mayor que -1."));
                }

                foreach (KeyValuePair<string, double> change in phase.AnnualSwitch)
                {
                    if (cycle.Phase(change.Key) == null || change.Key == phase.Id || change.Value < 0 || change.Value > 1)
                    {
                        _errors.Add(new ContentError(phaseWhere, $"cambio_anual: '{change.Key}' debe ser otra fase de este país, con probabilidad entre 0 y 1."));
                    }
                }

                if (phase.AnnualSwitch.Values.Sum() > 1)
                {
                    _errors.Add(new ContentError(phaseWhere, "cambio_anual: las probabilidades suman más de 1."));
                }
            }
        }

        private void ValidateInflation(string where, InflationDefinition inflation)
        {
            if (inflation.Regimes.Count == 0)
            {
                _errors.Add(new ContentError(where, "debe tener al menos un régimen."));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (InflationRegimeDefinition regime in inflation.Regimes)
            {
                string regimeWhere = $"{where}, {regime.Id}";
                CheckId(regimeWhere, regime.Id, "economia.inflacion.");
                if (!seen.Add(regime.Id))
                {
                    _errors.Add(new ContentError(regimeWhere, "régimen repetido."));
                }

                if (regime.AnnualMin <= -1 || regime.AnnualMax < regime.AnnualMin || regime.MonthlyVariation < 0)
                {
                    _errors.Add(new ContentError(regimeWhere, "anual_min debe ser mayor que -1, anual_max no menor que anual_min y variacion_mensual no negativa."));
                }

                foreach (KeyValuePair<string, double> change in regime.AnnualSwitch)
                {
                    if (inflation.Regime(change.Key) == null || change.Key == regime.Id)
                    {
                        _errors.Add(new ContentError(regimeWhere, $"cambio_anual: '{change.Key}' no es otro régimen de este país."));
                    }

                    if (change.Value < 0 || change.Value > 1)
                    {
                        _errors.Add(new ContentError(regimeWhere, $"cambio_anual: la probabilidad de '{change.Key}' debe estar entre 0 y 1."));
                    }
                }

                if (regime.AnnualSwitch.Values.Sum() > 1)
                {
                    _errors.Add(new ContentError(regimeWhere, "cambio_anual: las probabilidades suman más que 1."));
                }
            }

            if (inflation.Regime(inflation.InitialRegime) == null)
            {
                _errors.Add(new ContentError(where, $"regimen_inicial '{inflation.InitialRegime}' no es uno de sus regímenes."));
            }
        }

        private void ValidateJobSearch(string where, JobSearchDefinition search)
        {
            if (search.Methods.Count == 0)
            {
                _errors.Add(new ContentError(where, "debe tener al menos una forma de buscar."));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JobSearchMethodDefinition method in search.Methods)
            {
                string methodWhere = $"{where}, {method.Id}";
                CheckId(methodWhere, method.Id, "busqueda.forma.");
                if (!seen.Add(method.Id))
                {
                    _errors.Add(new ContentError(methodWhere, "forma repetida."));
                }

                if (method.WeeklyInterview < 0 || method.WeeklyInterview > 1 || method.ApplicationsPerWeek < 0)
                {
                    _errors.Add(new ContentError(methodWhere, "entrevista_semanal entre 0 y 1, y postulaciones_semana desde 0."));
                }
            }

            JobNegotiationDefinition n = search.Negotiation;
            bool badChance = new[] { search.InterviewSuccess, n.RaiseChance, n.WithdrawChance }.Any(p => p < 0 || p > 1) || n.RaiseChance + n.WithdrawChance > 1;
            bool badFactor = new[] { search.DegreeFactor, search.AgeFactor, search.LongSearchFactor, search.ExperienceCap }.Any(f => f < 0) || search.PerYearOfExperience < 0 || n.Raise < 0;
            if (badChance || badFactor || search.ContactAffection < 0 || search.ContactAffection > 100 || search.MonthsForTypicalSalary < 0)
            {
                _errors.Add(new ContentError(where, "probabilidades entre 0 y 1 (prob_mejora + prob_retiro hasta 1), factores no negativos y carino_contacto entre 0 y 100."));
            }
        }

        private void ValidateCompanies(string where, CompaniesDefinition companies)
        {
            if (companies.Sizes.Count == 0 || companies.Sizes.Sum(s => s.Weight) <= 0 || companies.SameCompany < 0 || companies.SameCompany > 1)
            {
                _errors.Add(new ContentError(where, "debe tener al menos un tamaño con peso, y misma_empresa entre 0 y 1."));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CompanySizeDefinition size in companies.Sizes)
            {
                string sizeWhere = $"{where}, {size.Id}";
                CheckId(sizeWhere, size.Id, "empresa.tamano.");
                if (!seen.Add(size.Id))
                {
                    _errors.Add(new ContentError(sizeWhere, "tamaño repetido."));
                }

                bool badChance = new[] { size.AnnualBankruptcy, size.AnnualGrowth, size.AnnualShrink }.Any(p => p < 0 || p > 1);
                if (size.Weight < 0 || size.SalaryRelative <= 0 || size.MaxPositionDepth < 0 || badChance)
                {
                    _errors.Add(new ContentError(sizeWhere, "peso no negativo, sueldo_relativo mayor que 0, cargo_maximo desde 0 y probabilidades entre 0 y 1."));
                }
            }
        }

        private void ValidateCareer(CareerDefinition career)
        {
            string where = $"carrera {career.Id}";
            CheckId(where, career.Id, "carrera.");
            foreach (string name in career.CompanyNames)
            {
                CheckId(where + ", nombres_empresa", name, "empresa.");
            }

            if (career.WorkplaceKind != null && !CareerDefinition.WorkplaceKinds.Contains(career.WorkplaceKind))
            {
                _errors.Add(new ContentError(where, $"lugar_de_trabajo debe ser uno de: {string.Join(", ", CareerDefinition.WorkplaceKinds)}."));
            }

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

                if (level.SalaryRelative <= 0)
                {
                    _errors.Add(new ContentError(levelWhere, "sueldo_relativo debe ser mayor que 0."));
                }
            }

            ValidateAreas(where, career);
        }

        /// <summary>Mínimo de áreas por carrera (regla 3 del diseño de carreras).</summary>
        public const int MinimumAreas = 3;

        private void ValidateAreas(string where, CareerDefinition career)
        {
            if (career.Areas.Count == 0)
            {
                if (career.Positions.Count > 0)
                {
                    _errors.Add(new ContentError(where, "tiene cargos pero ninguna área: los cargos se usan dentro de un área."));
                }

                return;
            }

            if (career.Areas.Count < MinimumAreas)
            {
                _errors.Add(new ContentError(where, $"debe tener al menos {MinimumAreas} áreas (tiene {career.Areas.Count})."));
            }

            var positionIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<CareerPositionDefinition>? shared = career.Positions.Count > 0 ? ValidateLadder(where, career, career.Positions, positionIds) : null;
            foreach (CareerAreaDefinition area in career.Areas)
            {
                string areaWhere = $"{where}, {area.Id}";
                area.CareerId = career.Id;
                CheckId(areaWhere, area.Id, career.Id + ".area.");
                if (_careerAreas.ContainsKey(area.Id))
                {
                    _errors.Add(new ContentError(areaWhere, "id de área repetido."));
                    continue;
                }

                _careerAreas.Add(area.Id, area);
                if (area.SalaryRelative <= 0)
                {
                    _errors.Add(new ContentError(areaWhere, "sueldo_relativo debe ser mayor que 0."));
                }

                IReadOnlyList<CareerPositionDefinition>? ladder = area.Positions != null && area.Positions.Count > 0
                    ? ValidateLadder(areaWhere, career, area.Positions, positionIds)
                    : shared;
                if (ladder == null || ladder.Count == 0)
                {
                    _errors.Add(new ContentError(areaWhere, "no tiene cargos: define \"cargos\" en la carrera o en el área."));
                    area.Ladder = new List<CareerPositionDefinition>();
                    continue;
                }

                area.Ladder = ladder;
            }
        }

        /// <summary>
        /// Revisa una escalera de cargos y enlaza cada cargo con sus siguientes y su anterior.
        /// Todo cargo debe poder alcanzarse desde el primero, y por un solo camino.
        /// </summary>
        private IReadOnlyList<CareerPositionDefinition> ValidateLadder(string where, CareerDefinition career, List<CareerPositionDefinition> positions, HashSet<string> careerPositionIds)
        {
            var byId = new Dictionary<string, CareerPositionDefinition>(StringComparer.Ordinal);
            foreach (CareerPositionDefinition position in positions)
            {
                string positionWhere = $"{where}, {position.Id}";
                position.CareerId = career.Id;
                CheckId(positionWhere, position.Id, career.Id + ".cargo.");
                if (!careerPositionIds.Add(position.Id) || _careerPositions.ContainsKey(position.Id))
                {
                    _errors.Add(new ContentError(positionWhere, "id de cargo repetido (cada escalera usa cargos propios)."));
                    continue;
                }

                byId.Add(position.Id, position);
                _careerPositions.Add(position.Id, position);
                if (position.SalaryRelative <= 0)
                {
                    _errors.Add(new ContentError(positionWhere, "sueldo_relativo debe ser mayor que 0."));
                }

                if (position.MinMonths < 0)
                {
                    _errors.Add(new ContentError(positionWhere, "meses_minimos no puede ser negativo."));
                }

                if (position.MinPerformance < 0 || position.MinPerformance > 100)
                {
                    _errors.Add(new ContentError(positionWhere, "desempeno_minimo debe estar entre 0 y 100."));
                }
            }

            for (int i = 0; i < positions.Count; i++)
            {
                CareerPositionDefinition position = positions[i];
                IEnumerable<string> nextIds = position.Next ?? (i + 1 < positions.Count ? new List<string> { positions[i + 1].Id } : new List<string>());
                var next = new List<CareerPositionDefinition>();
                foreach (string nextId in nextIds)
                {
                    if (!byId.TryGetValue(nextId, out CareerPositionDefinition? target))
                    {
                        _errors.Add(new ContentError($"{where}, {position.Id}", $"siguientes: el cargo '{nextId}' no está en esta escalera."));
                        continue;
                    }

                    if (target == positions[0] || (target.Previous != null && target.Previous != position))
                    {
                        _errors.Add(new ContentError($"{where}, {position.Id}", $"siguientes: al cargo '{nextId}' ya se llega desde otro; cada cargo tiene un solo anterior."));
                        continue;
                    }

                    target.Previous = position;
                    next.Add(target);
                }

                position.NextPositions = next;
            }

            // Profundidad desde el inicial; un cargo sin profundidad no se alcanza nunca.
            var reached = new HashSet<CareerPositionDefinition>();
            var pending = new Queue<CareerPositionDefinition>();
            if (positions.Count > 0)
            {
                positions[0].Depth = 0;
                reached.Add(positions[0]);
                pending.Enqueue(positions[0]);
            }

            while (pending.Count > 0)
            {
                CareerPositionDefinition current = pending.Dequeue();
                foreach (CareerPositionDefinition next in current.NextPositions.Where(n => reached.Add(n)))
                {
                    next.Depth = current.Depth + 1;
                    pending.Enqueue(next);
                }
            }

            foreach (CareerPositionDefinition position in positions.Where(p => !reached.Contains(p)))
            {
                _errors.Add(new ContentError($"{where}, {position.Id}", "no se llega a este cargo desde el primero de la escalera."));
            }

            return positions;
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
                        Tendency = level.Tendency,
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

            foreach (AppDefinition app in _apps.Values)
            {
                yield return new KeyValuePair<string, string>(app.Id, $"app {app.Id}");
            }

            foreach (MailDefinition mail in _mails.Values)
            {
                yield return new KeyValuePair<string, string>(mail.Id, $"correo {mail.Id}");
                yield return new KeyValuePair<string, string>(mail.SubjectKey, $"correo {mail.Id}");
                yield return new KeyValuePair<string, string>(mail.Sender, $"correo {mail.Id}");
                foreach (string key in mail.OnJournal)
                {
                    yield return new KeyValuePair<string, string>(key, $"correo {mail.Id}, al_anotar");
                }
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
                foreach (InflationRegimeDefinition regime in country.Economy?.Inflation.Regimes ?? new List<InflationRegimeDefinition>())
                {
                    yield return new KeyValuePair<string, string>("diario." + regime.Id, $"país {country.Id}");
                }

                foreach (EconomicPhaseDefinition phase in country.Economy?.Cycle?.Phases ?? new List<EconomicPhaseDefinition>())
                {
                    yield return new KeyValuePair<string, string>("diario." + phase.Id, $"país {country.Id}");
                }

                PayrollDefinition? payroll = country.Economy?.Payroll;
                if (payroll != null)
                {
                    foreach (DeductionDefinition deduction in payroll.Employee.Concat(payroll.Freelance))
                    {
                        yield return new KeyValuePair<string, string>(deduction.Id, $"país {country.Id}");
                    }

                    if (payroll.MonthlyTax.Count > 0)
                    {
                        yield return new KeyValuePair<string, string>(Economy.Payslip.IncomeTaxId, $"país {country.Id}");
                    }
                }

                foreach (ExpenseDefinition expense in country.Economy?.Expenses?.Categories ?? new List<ExpenseDefinition>())
                {
                    yield return new KeyValuePair<string, string>(expense.Id, $"país {country.Id}");
                }

                foreach (HousingTypeDefinition housingType in country.Economy?.Housing?.Types ?? new List<HousingTypeDefinition>())
                {
                    yield return new KeyValuePair<string, string>(housingType.Id, $"país {country.Id}");
                }

                foreach (ServiceOptionDefinition option in (country.Economy?.Services ?? new List<ServiceDefinition>()).SelectMany(s => s.Options))
                {
                    yield return new KeyValuePair<string, string>(option.Id, $"país {country.Id}");
                }

                foreach (GroceryBasketDefinition basket in country.Economy?.Groceries?.Baskets ?? new List<GroceryBasketDefinition>())
                {
                    yield return new KeyValuePair<string, string>(basket.Id, $"país {country.Id}");
                }

                foreach (DebtTypeDefinition debt in country.Economy?.Debts?.Types ?? new List<DebtTypeDefinition>())
                {
                    yield return new KeyValuePair<string, string>(debt.Id, $"país {country.Id}");
                }

                if (country.Economy?.Expenses?.DeficitDebt != null)
                {
                    yield return new KeyValuePair<string, string>(Economy.ExpenseSystem.DeficitJournal, $"país {country.Id}");
                }

                if (country.Economy?.Debts != null)
                {
                    foreach (string key in new[] { Economy.DebtSystem.PaidOffJournal, Economy.DebtSystem.DelinquentJournal, Economy.DebtSystem.ClearedJournal, Economy.DebtSystem.PrescribedJournal })
                    {
                        yield return new KeyValuePair<string, string>(key, $"país {country.Id}");
                    }
                }

                foreach (CompanySizeDefinition size in country.Economy?.Companies?.Sizes ?? new List<CompanySizeDefinition>())
                {
                    yield return new KeyValuePair<string, string>(size.Id, $"país {country.Id}");
                }

                if (country.Economy?.Companies != null)
                {
                    foreach (string key in new[] { Economy.CompanySystem.BankruptcyJournal, Economy.CompanySystem.GrowthJournal, Economy.CompanySystem.ShrinkJournal })
                    {
                        yield return new KeyValuePair<string, string>(key, $"país {country.Id}");
                    }
                }

                if (country.Economy?.JobSearch != null)
                {
                    foreach (JobSearchMethodDefinition method in country.Economy.JobSearch.Methods)
                    {
                        yield return new KeyValuePair<string, string>(method.Id, $"país {country.Id}");
                    }

                    foreach (string key in new[] { Economy.JobSearchRules.RejectionJournal, Economy.JobSearchRules.WithdrawnJournal, Economy.JobSearchRules.HiredJournal, Economy.JobSearchRules.InterviewJournal })
                    {
                        yield return new KeyValuePair<string, string>(key, $"país {country.Id}");
                    }
                }

                if (country.Economy?.Inheritance != null)
                {
                    yield return new KeyValuePair<string, string>(Economy.InheritanceSettlement.ReceivedJournal, $"país {country.Id}");
                    yield return new KeyValuePair<string, string>(Economy.InheritanceSettlement.DebtsJournal, $"país {country.Id}");
                }

                if (country.Economy?.AnnualTax != null)
                {
                    foreach (string key in new[] { Economy.TaxSystem.RefundJournal, Economy.TaxSystem.PaymentJournal, Economy.TaxSystem.PenaltyJournal, Economy.TaxSystem.LostRefundJournal })
                    {
                        yield return new KeyValuePair<string, string>(key, $"país {country.Id}");
                    }
                }
            }

            foreach (CareerDefinition career in _careers.Values)
            {
                yield return new KeyValuePair<string, string>(career.Id, $"carrera {career.Id}");
                foreach (CareerLevelDefinition level in career.Levels)
                {
                    yield return new KeyValuePair<string, string>(level.Id, $"carrera {career.Id}");
                    yield return new KeyValuePair<string, string>(level.TitleTextKey, $"carrera {career.Id}");
                }

                foreach (CareerAreaDefinition area in career.Areas)
                {
                    yield return new KeyValuePair<string, string>(area.Id, $"carrera {career.Id}");
                }

                foreach (string name in career.CompanyNames)
                {
                    yield return new KeyValuePair<string, string>(name, $"carrera {career.Id}");
                }

                foreach (CareerPositionDefinition position in career.Positions.Concat(career.Areas.SelectMany(a => a.Positions ?? new List<CareerPositionDefinition>())))
                {
                    yield return new KeyValuePair<string, string>(position.Id, $"carrera {career.Id}");
                }
            }

            foreach (NewsDefinition news in _news.Values)
            {
                foreach (string key in news.AllTextKeys)
                {
                    yield return new KeyValuePair<string, string>(key, $"noticia {news.Id}");
                }
            }

            foreach (EventDefinition ev in _events.Values)
            {
                string where = $"evento {ev.Id}";
                foreach (string key in ev.AllTextKeys)
                {
                    yield return new KeyValuePair<string, string>(key, where);
                }
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

            // Una variante solo cambia la forma de decir un texto que ya existe en su idioma general.
            foreach (KeyValuePair<string, Dictionary<string, string>> variant in _texts.Where(t => GeneralLanguage(t.Key) != t.Key))
            {
                string general = GeneralLanguage(variant.Key);
                _texts.TryGetValue(general, out Dictionary<string, string>? generalTable);
                foreach (string key in variant.Value.Keys.Where(k => generalTable == null || !generalTable.ContainsKey(k)))
                {
                    _errors.Add(new ContentError($"idioma {variant.Key}", $"el texto '{key}' no existe en '{general}': una variante solo cambia textos que ya existen."));
                }
            }

            foreach (CountryDefinition country in _countries.Values)
            {
                foreach (KeyValuePair<string, string> variant in country.LanguageVariants)
                {
                    if (GeneralLanguage(variant.Value) != variant.Key || variant.Value == variant.Key)
                    {
                        _errors.Add(new ContentError($"país {country.Id}", $"variantes_idioma: '{variant.Value}' no es una variante de '{variant.Key}' (por ejemplo, \"es\": \"es-CL\")."));
                    }
                }
            }
        }

        /// <summary>Los países contra los que se revisa lo que se está leyendo (ver <see cref="_checkedCountry"/>).</summary>
        private IEnumerable<CountryDefinition> CheckedCountries() =>
            _checkedCountry == null ? _countries.Values : new[] { _checkedCountry };

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
                if (EventScope.SalaryNames.Contains(name))
                {
                    foreach (CountryDefinition country in CheckedCountries().Where(c => c.Economy?.Salaries == null))
                    {
                        _errors.Add(new ContentError(where, $"{name}: el país {country.Id} no define sus sueldos (economia.sueldos). Todo país debe tenerlos si un evento los usa."));
                    }
                }

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

                if (reference.Function == EventScope.LawFunction || reference.Function == EventScope.ChangeLawFunction)
                {
                    foreach (CountryDefinition country in CheckedCountries().Where(c => !c.LawsJson.ContainsKey(reference.Argument)))
                    {
                        _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): el país {country.Id} no define esa ley. Todo país debe definir las leyes que usan los eventos."));
                    }
                }

                if (reference.Function == EventScope.ChangeCycleFunction)
                {
                    foreach (CountryDefinition country in CheckedCountries().Where(c => c.Economy?.Cycle?.Phase(reference.Argument) == null))
                    {
                        _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): el país {country.Id} no tiene esa fase del ciclo económico. Todo país debe definir las fases que usan los eventos."));
                    }
                }

                if (reference.Function == EventScope.ChangeInflationFunction)
                {
                    foreach (CountryDefinition country in CheckedCountries().Where(c => c.Economy?.Inflation.Regime(reference.Argument) == null))
                    {
                        _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): el país {country.Id} no tiene ese régimen de inflación. Todo país debe definir los regímenes que usan los eventos."));
                    }
                }

                if ((reference.Function == "." + CharacterObject.MeetMethod || reference.Function == "." + CharacterObject.BondMethod)
                    && !Life.RelationshipRules.BondKeys.Contains(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): ese lazo no existe; usa 'amistad', 'colega' o 'rival'."));
                }

                if (reference.Function == "." + CharacterObject.RentMethod || reference.Function == EventScope.RentFunction)
                {
                    string name = reference.Function.TrimStart('.');
                    foreach (CountryDefinition country in CheckedCountries().Where(c => c.Economy?.Housing?.Type(reference.Argument) == null))
                    {
                        _errors.Add(new ContentError(where, $"{name}('{reference.Argument}'): el país {country.Id} no tiene ese tipo de vivienda. Todo país debe definir en economia.vivienda los tipos que usan los eventos."));
                    }
                }

                if (reference.Function == "." + CharacterObject.ContractMethod || reference.Function == EventScope.FeeFunction)
                {
                    string name = reference.Function.TrimStart('.');
                    foreach (CountryDefinition country in CheckedCountries().Where(c => (c.Economy?.Services ?? new List<ServiceDefinition>()).All(s => s.Option(reference.Argument) == null)))
                    {
                        _errors.Add(new ContentError(where, $"{name}('{reference.Argument}'): el país {country.Id} no tiene esa opción en economia.servicios. Todo país debe definir los servicios que usan los eventos."));
                    }
                }

                if ((reference.Function == "." + CharacterObject.ServiceMethod || reference.Function == "." + CharacterObject.CancelServiceMethod)
                    && CheckedCountries().Any(c => (c.Economy?.Services ?? new List<ServiceDefinition>()).All(s => s.Id != reference.Argument)))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.TrimStart('.')}('{reference.Argument}'): ese servicio no está en economia.servicios de todos los países."));
                }

                if (reference.Function == "." + CharacterObject.ChooseGroceriesMethod || reference.Function == EventScope.GroceriesFunction)
                {
                    string name = reference.Function.TrimStart('.');
                    foreach (CountryDefinition country in CheckedCountries().Where(c => c.Economy?.Groceries?.Basket(reference.Argument) == null))
                    {
                        _errors.Add(new ContentError(where, $"{name}('{reference.Argument}'): el país {country.Id} no tiene esa compra. Todo país debe definir en economia.supermercado las compras que usan los eventos."));
                    }
                }

                if (reference.Function == "." + CharacterObject.BorrowMethod || reference.Function == "." + CharacterObject.DebtOfMethod)
                {
                    foreach (CountryDefinition country in CheckedCountries().Where(c => c.Economy?.Debts?.Type(reference.Argument) == null))
                    {
                        _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): el país {country.Id} no tiene esa deuda. Todo país debe definir las deudas que usan los eventos."));
                    }
                }

                if ((reference.Function == "." + CharacterObject.WorkInAreaMethod || reference.Function == "." + CharacterObject.CanWorkInAreaMethod)
                    && !_careerAreas.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): esa área de carrera no existe."));
                }

                if (reference.Function == "." + CharacterObject.PostulateMethod
                    && reference.Argument != Economy.JobSearch.AnyJob && !_careerAreas.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): esa área de carrera no existe (o usa '{Economy.JobSearch.AnyJob}')."));
                }

                if ((reference.Function == "." + CharacterObject.SearchJobMethod || reference.Function == "." + CharacterObject.CanSearchWithMethod)
                    && !_countries.Values.Any(c => c.Economy?.JobSearch?.Method(reference.Argument) != null))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): ningún país tiene esa forma de buscar trabajo (economia.busqueda_trabajo.formas)."));
                }

                if ((reference.Function == "." + CharacterObject.PromoteMethod || reference.Function == "." + CharacterObject.CanPromoteToMethod)
                    && !_careerPositions.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): ese cargo no existe."));
                }

                if ((reference.Function == "." + CharacterObject.InstallAppMethod || reference.Function == "." + CharacterObject.UninstallAppMethod
                        || reference.Function == "." + CharacterObject.HasAppMethod)
                    && !_apps.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): esa app no existe (packs/*/apps/)."));
                }

                if (reference.Function == "." + CharacterObject.ReceiveMailMethod && !_mails.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function.Substring(1)}('{reference.Argument}'): ese correo no existe (packs/*/mail/)."));
                }

                if (reference.Function == EventScope.RecentNewsFunction && !_news.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): esa noticia no existe."));
                }

                if (reference.Function == EventScope.StudyFunction && !_careerLevels.ContainsKey(reference.Argument))
                {
                    _errors.Add(new ContentError(where, $"{reference.Function}('{reference.Argument}'): ese nivel de carrera no existe."));
                }
            }
        }

        /// <summary>Revisa los correos y arma la lista de qué correo llega con cada línea del diario.</summary>
        private void ValidateMails()
        {
            foreach (MailDefinition mail in _mails.Values)
            {
                string where = $"correo {mail.Id}";
                CheckId(where, mail.Id, "correo.");
                CheckId(where, mail.Sender, "remitente.");
                if (Array.IndexOf(MailDefinition.Topics, mail.Topic) < 0)
                {
                    _errors.Add(new ContentError(where, $"tema '{mail.Topic}' no existe (usa {string.Join(", ", MailDefinition.Topics)})."));
                }

                mail.CompiledConditions = string.IsNullOrWhiteSpace(mail.Conditions) ? null : CompileCondition(where, mail.Conditions);
                if (mail.Every != null)
                {
                    if (Array.IndexOf(MailDefinition.Periods, mail.Every) < 0)
                    {
                        _errors.Add(new ContentError(where, $"cada '{mail.Every}' no existe (usa {string.Join(", ", MailDefinition.Periods)})."));
                    }

                    if (string.IsNullOrWhiteSpace(mail.Conditions))
                    {
                        _errors.Add(new ContentError(where, "un correo con 'cada' necesita condiciones: tiene que venir de algo que el personaje tiene (nada se inventa)."));
                    }

                    if (mail.OnJournal.Count > 0)
                    {
                        _errors.Add(new ContentError(where, "usa 'cada' o 'al_anotar', no los dos."));
                    }

                    _periodicMails.Add(mail);
                }

                if (mail.InMonth.HasValue && (mail.Every != "anio" || mail.InMonth < 1 || mail.InMonth > 12))
                {
                    _errors.Add(new ContentError(where, "en_mes va de 1 a 12 y solo sirve con \"cada\": \"anio\"."));
                }

                if (mail.OnJournal.Count != mail.OnJournal.Distinct().Count())
                {
                    _errors.Add(new ContentError(where, "al_anotar repite una línea del diario."));
                }

                foreach (string key in mail.OnJournal)
                {
                    if (!key.StartsWith("diario.", StringComparison.Ordinal))
                    {
                        _errors.Add(new ContentError(where, $"al_anotar: '{key}' no es una línea del diario (empiezan con 'diario.')."));
                        continue;
                    }

                    if (!_mailsByJournal.TryGetValue(key, out List<MailDefinition>? list))
                    {
                        list = new List<MailDefinition>();
                        _mailsByJournal.Add(key, list);
                    }

                    list.Add(mail);
                }
            }
        }

        private void CheckWeights(string where, Dictionary<string, int> weights, IReadOnlyList<string> allowed)
        {
            foreach (string key in weights.Keys.Where(k => !allowed.Contains(k)))
            {
                _errors.Add(new ContentError(where, $"'{key}' no existe. Usa: {string.Join(", ", allowed)}."));
            }

            if (weights.Values.Any(w => w < 0) || (weights.Count > 0 && weights.Values.Sum() <= 0))
            {
                _errors.Add(new ContentError(where, "los pesos no pueden ser negativos y deben sumar más que 0."));
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
