using System.Linq;
using System.Text;
using MistakesLife.Core.Decisions;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Saving;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Time;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class SaveGameTests
    {
        /// <summary>
        /// Juega hasta que el jugador de turno cumpla esa edad. Las decisiones
        /// dependen solo del estado del mundo, así dos partidas iguales deciden
        /// igual. Si el jugador muere, sigue con su primer hijo vivo.
        /// </summary>
        private static void PlayUntilAge(LifeSession session, int age)
        {
            var decider = new AutoDecider(session.Content);
            while (!session.IsOver)
            {
                if (!session.World.Player.IsAlive)
                {
                    session.ContinueWith(session.Heirs[0].Id);
                    continue;
                }

                int target = age * GameDate.WeeksPerYear;
                int weeks = target - session.World.Player.AgeInWeeks(session.World.Date);
                PendingEvent? pending = session.Pending;
                if (pending != null)
                {
                    var chooser = new GameRandom(session.World.Seed).Fork("prueba." + session.World.Date.TotalWeeks + "." + session.World.EventHistory.Count);
                    session.Choose(decider.Choose(session.World, pending, chooser));
                    continue;
                }

                if (weeks <= 0)
                {
                    return;
                }

                session.Advance(weeks);
            }
        }

        [Theory]
        [InlineData(1UL)]
        [InlineData(7UL)]
        [InlineData(42UL)]
        public void Guardar_YCargar_DaExactamenteLaMismaPartida(ulong seed)
        {
            var session = new LifeSession(RepoPacks.Content, seed);
            PlayUntilAge(session, 45);

            string saved = session.SaveToText();
            LifeSession loaded = LifeSession.Load(RepoPacks.Content, saved);

            Assert.Equal(saved, loaded.SaveToText());
            Assert.Equal(session.World.Date, loaded.World.Date);
            Assert.Equal(session.World.PlayerId, loaded.World.PlayerId);
            Assert.Equal(session.World.Characters.Count, loaded.World.Characters.Count);
            Assert.Equal(session.World.Player.Money, loaded.World.Player.Money);
        }

        [Theory]
        [InlineData(3UL)]
        [InlineData(11UL)]
        public void PartidaCargada_SigueViviendoIgualQueSinGuardar(ulong seed)
        {
            var original = new LifeSession(RepoPacks.Content, seed);
            PlayUntilAge(original, 30);

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, original.SaveToText());

            // Las dos partidas siguen 40 años más con las mismas decisiones:
            // si el guardado olvidara algo, las vidas se separarían.
            PlayUntilAge(original, 70);
            PlayUntilAge(loaded, 70);

            Assert.Equal(original.SaveToText(), loaded.SaveToText());
        }

        [Fact]
        public void Guardar_DespuesDelLegado_ConservaElLinajeYLaGeneracion()
        {
            var session = new LifeSession(RepoPacks.Content, 5);
            PlayUntilAge(session, 30);
            session.World.Player.Die(session.World.Date);

            // Un hijo para poder seguir: se crea con la fábrica de familias del núcleo.
            if (session.Heirs.Count == 0)
            {
                Life.FamilyFactory.CreateChild(session.World, session.Content, session.World.Player);
            }

            session.ContinueWith(session.Heirs[0].Id);
            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.Equal(2, loaded.World.Generation);
            Assert.Equal(session.World.FormerPlayers, loaded.World.FormerPlayers);
            Assert.False(loaded.World.GetCharacter(session.World.FormerPlayers[0]).IsAlive);
        }

        [Fact]
        public void Guardar_ConUnEventoEsperando_LoCargaConLasMismasOpciones()
        {
            var session = new LifeSession(RepoPacks.Content, 2);
            while (session.Pending == null)
            {
                session.Advance(52);
            }

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.NotNull(loaded.Pending);
            Assert.Equal(session.Pending.EventId, loaded.Pending!.EventId);
            Assert.Equal(session.Pending.AvailableOptions, loaded.Pending.AvailableOptions);
        }

        [Fact]
        public void Guardar_UnaLeyQueCambio_LaCargaConSuValor()
        {
            var session = new LifeSession(RepoPacks.Content, 1);
            session.World.ChangeLaw("pais.chile", "ley.prueba_numero", Value.FromNumber(21));
            session.World.ChangeLaw("pais.chile", "ley.prueba_si_no", Value.True);
            session.World.ChangeLaw("pais.chile", "ley.prueba_texto", Value.FromText("mixto"));

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.True(loaded.World.TryGetChangedLaw("pais.chile", "ley.prueba_numero", out Value number));
            Assert.Equal(Value.FromNumber(21), number);
            Assert.True(loaded.World.TryGetChangedLaw("pais.chile", "ley.prueba_si_no", out Value yesNo));
            Assert.Equal(Value.True, yesNo);
            Assert.True(loaded.World.TryGetChangedLaw("pais.chile", "ley.prueba_texto", out Value text));
            Assert.Equal(Value.FromText("mixto"), text);
        }

        [Fact]
        public void Guardar_ElDiarioYaLeido_NoSeRepiteAlCargar()
        {
            var session = new LifeSession(RepoPacks.Content, 4);
            PlayUntilAge(session, 10);
            session.TakeNewJournalEntries();

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            Assert.Empty(loaded.TakeNewJournalEntries());
            Assert.Equal(session.World.Journal.Count, loaded.World.Journal.Count);
        }

        [Fact]
        public void GuardarEnBytes_SeCargaIgualYPesaMenosQueElTexto()
        {
            var session = new LifeSession(RepoPacks.Content, 9);
            PlayUntilAge(session, 40);

            byte[] bytes = session.SaveToBytes();
            LifeSession loaded = LifeSession.LoadFromBytes(RepoPacks.Content, bytes);

            string text = session.SaveToText();
            Assert.Equal(text, loaded.SaveToText());
            Assert.True(bytes.Length < Encoding.UTF8.GetByteCount(text) / 3, $"Comprimido pesa {bytes.Length} bytes y el texto {text.Length}.");
        }

        [Fact]
        public void Guardado_DiceLaVersionDelFormato()
        {
            string saved = new LifeSession(RepoPacks.Content, 1).SaveToText();

            Assert.Contains("\"version\":" + CoreInfo.SaveFormatVersion, saved);
            Assert.Contains("\"game\":\"MistakesLife\"", saved);
        }

        [Fact]
        public void Cargar_UnGuardadoDeUnaVersionMasNueva_AvisaQueHayQueActualizar()
        {
            string saved = new LifeSession(RepoPacks.Content, 1).SaveToText()
                .Replace("\"version\":" + CoreInfo.SaveFormatVersion, "\"version\":" + (CoreInfo.SaveFormatVersion + 1));

            var error = Assert.Throws<SaveFormatException>(() => LifeSession.Load(RepoPacks.Content, saved));
            Assert.Contains("actualizar", error.Message);
        }

        [Fact]
        public void Cargar_UnGuardadoDeLaVersion1_LoConvierteYSigue()
        {
            var session = new LifeSession(RepoPacks.Content, 6);
            PlayUntilAge(session, 30);
            // Se lee con los números como decimales, para no perder dígitos del dinero.
            using var reader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(session.SaveToText()))
            {
                FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal,
            };
            var json = Newtonsoft.Json.Linq.JObject.Load(reader);

            // Así era un guardado de la versión 1: sin los meses cotizados.
            json["version"] = 1;
            foreach (var character in json.SelectTokens("world.characters[*]"))
            {
                ((Newtonsoft.Json.Linq.JObject)character).Remove("contributedMonths");
            }

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, json.ToString());

            Assert.Equal(0, loaded.World.Player.ContributedMonths);
            Assert.Equal(session.World.Player.Money, loaded.World.Player.Money);
            Assert.Equal(session.World.Player.PensionFund, loaded.World.Player.PensionFund);
            Assert.Contains("\"version\":" + CoreInfo.SaveFormatVersion, loaded.SaveToText());
        }

        [Theory]
        [InlineData("esto no es un guardado")]
        [InlineData("{\"nombre\": \"otra cosa\"}")]
        [InlineData("{\"game\": \"MistakesLife\"}")]
        [InlineData("{\"game\": \"MistakesLife\", \"version\": 2, \"world\": {\"playerId\": 99}}")]
        public void Cargar_UnTextoQueNoEsUnaPartida_DaUnErrorClaro(string text)
        {
            Assert.Throws<SaveFormatException>(() => LifeSession.Load(RepoPacks.Content, text));
        }

        [Fact]
        public void Cargar_BytesQueNoSonUnaPartida_DaUnErrorClaro()
        {
            Assert.Throws<SaveFormatException>(() => LifeSession.LoadFromBytes(RepoPacks.Content, Encoding.UTF8.GetBytes("hola")));
        }

        /// <summary>
        /// Si alguien agrega un dato al personaje o al mundo, esta prueba falla para
        /// recordarle que también debe guardarlo (Saving/WorldWriter.cs y
        /// WorldReader.cs), subir CoreInfo.SaveFormatVersion y agregar su migración.
        /// Después de hacerlo, se suma el nombre del dato a esta lista.
        /// </summary>
        [Fact]
        public void DatosNuevosDelPersonajeODelMundo_ObliganARevisarElGuardado()
        {
            string[] character =
            {
                "Id", "FirstName", "LastName", "Sex", "Gender", "GenderIdentity", "Orientation", "BirthDate", "Personality", "Stats",
                "MotherId", "FatherId", "SecondParentId", "Adopted", "PartnerId", "Residence", "HouseholdLevel", "Money", "Job",
                "LastPayslip", "LastExpenses", "MonthsInRed", "PendingTaxReturn", "PensionFund", "Pension", "ReceivesUniversalPension",
                "LastPensionPayment", "ContributedMonths", "Debts", "LastDebtPayment", "Delinquent", "EducationStageId", "Flags", "Studies", "Degrees",
                "IsAlive", "DeathDate", "DeathCauseId",
            };
            string[] world =
            {
                "Seed", "Random", "Date", "PlayerId", "Player", "Characters", "Journal", "Seeds", "PendingEvents", "EventHistory",
                "FormerPlayers", "Economies", "Generation",
            };

            Assert.Equal(character.OrderBy(n => n), PublicProperties(typeof(Characters.Character)));
            Assert.Equal(world.OrderBy(n => n), PublicProperties(typeof(Worlds.World)));
        }

        private static string[] PublicProperties(System.Type type) =>
            type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Select(p => p.Name).OrderBy(n => n).ToArray();

        [Fact]
        public void Guardar_UnPersonajeConTodo_ConservaSusDatos()
        {
            var session = new LifeSession(RepoPacks.Content, 8);
            PlayUntilAge(session, 50);

            LifeSession loaded = LifeSession.Load(RepoPacks.Content, session.SaveToText());

            foreach (var before in session.World.Characters)
            {
                var after = loaded.World.GetCharacter(before.Id);
                Assert.Equal(before.ToString(), after.ToString());
                Assert.Equal(before.Flags, after.Flags);
                Assert.Equal(before.Degrees, after.Degrees);
                Assert.Equal(before.Debts.Select(d => d.Balance), after.Debts.Select(d => d.Balance));
                Assert.Equal(before.Job?.Salary, after.Job?.Salary);
                Assert.Equal(before.IsAlive, after.IsAlive);
                Assert.Equal(before.Personality.Neuroticism, after.Personality.Neuroticism);
            }

            Assert.Equal(session.World.Seeds.All.Count(), loaded.World.Seeds.All.Count());
            Assert.Equal(session.World.Economies.Single().PriceIndex, loaded.World.Economies.Single().PriceIndex);
        }
    }
}
