using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using MistakesLife.Core.Content;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Seeds;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MistakesLife.Core.Saving
{
    /// <summary>
    /// Convierte una partida en texto (JSON) o bytes comprimidos, y de vuelta.
    /// No toca archivos: quien guarda (Unity, la consola) decide dónde escribir.
    /// Se usa a través de <see cref="LifeSession.SaveToText"/> y <see cref="LifeSession.Load(ContentDatabase, string)"/>.
    /// </summary>
    internal static class SaveGame
    {
        private static JObject ParseExact(string text)
        {
            using var reader = new JsonTextReader(new StringReader(text)) { FloatParseHandling = FloatParseHandling.Decimal };
            return JObject.Load(reader);
        }

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
        };

        public static string ToText(LifeSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var file = new SaveFileData
            {
                Version = CoreInfo.SaveFormatVersion,
                Game = CoreInfo.GameName,
                JournalRead = session.JournalRead,
                World = WorldWriter.Write(session.World),
            };
            return JsonConvert.SerializeObject(file, Settings);
        }

        public static byte[] ToBytes(LifeSession session)
        {
            byte[] text = Encoding.UTF8.GetBytes(ToText(session));
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                gzip.Write(text, 0, text.Length);
            }

            return output.ToArray();
        }

        public static LifeSession FromBytes(ContentDatabase content, byte[] data, SeedRules seedRules, EventRules eventRules)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string text;
            try
            {
                using var input = new MemoryStream(data);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Encoding.UTF8);
                text = reader.ReadToEnd();
            }
            catch (InvalidDataException e)
            {
                throw new SaveFormatException("Los datos no son una partida guardada (no están comprimidos como se esperaba).", e);
            }

            return FromText(content, text, seedRules, eventRules);
        }

        public static LifeSession FromText(ContentDatabase content, string text, SeedRules seedRules, EventRules eventRules)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            JObject json;
            try
            {
                json = JObject.Parse(text);
            }
            catch (JsonException e)
            {
                throw new SaveFormatException("El texto no es una partida guardada (no es JSON válido).", e);
            }

            if ((string?)json["game"] != CoreInfo.GameName)
            {
                throw new SaveFormatException("El texto no es una partida guardada de " + CoreInfo.GameName + ".");
            }

            int version = (int?)json["version"] ?? 0;
            if (version < 1)
            {
                throw new SaveFormatException("La partida guardada no dice su versión.");
            }

            if (version > CoreInfo.SaveFormatVersion)
            {
                throw new SaveFormatException(
                    $"La partida se guardó con una versión más nueva del juego (formato {version}); esta versión entiende hasta el {CoreInfo.SaveFormatVersion}. Hay que actualizar el juego.");
            }

            // Un guardado viejo se convierte primero al formato actual. Para eso se
            // vuelve a leer con los números como decimales, que no pierden dígitos.
            string current = text;
            if (version < CoreInfo.SaveFormatVersion)
            {
                JObject exact = ParseExact(text);
                SaveMigrations.MigrateToCurrent(exact, version);
                current = exact.ToString(Formatting.None);
            }

            // Se lee el texto directo a los datos: así cada número se lee con su
            // tipo (dinero en decimal, índices en double) sin perder precisión.
            SaveFileData file;
            try
            {
                file = JsonConvert.DeserializeObject<SaveFileData>(current, Settings)
                    ?? throw new SaveFormatException("La partida guardada está vacía.");
            }
            catch (JsonException e)
            {
                throw new SaveFormatException("La partida guardada está dañada: " + e.Message, e);
            }

            var world = WorldReader.Read(file.World, content);
            return new LifeSession(content, world, seedRules, eventRules, file.JournalRead);
        }
    }
}
