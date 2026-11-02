using System;
using System.IO;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Scripting;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class CountryTests
    {
        private const string CountryJson = @"{
          ""id"": ""pais.prueba"",
          ""moneda"": ""CLP"",
          ""edad_madre_min"": 20,
          ""edad_madre_max"": 30,
          ""nombres"": { ""femeninos"": [""Sofía"", ""Isidora""], ""masculinos"": [""Mateo"", ""Agustín""], ""apellidos"": [""González"", ""Muñoz"", ""Rojas""] },
          ""ciudades"": [
            { ""id"": ""ciudad.prueba"", ""comunas"": [
              { ""id"": ""comuna.grande"", ""peso_nacimiento"": 9, ""nivel_hogar_min"": 20, ""nivel_hogar_max"": 40 },
              { ""id"": ""comuna.chica"", ""peso_nacimiento"": 1, ""nivel_hogar_min"": 70, ""nivel_hogar_max"": 90 }
            ] }
          ],
          ""etapas_educacion"": [
            { ""id"": ""etapa.prueba.jardin"", ""edad_desde"": 2, ""edad_hasta"": 6 },
            { ""id"": ""etapa.prueba.basica"", ""edad_desde"": 6, ""edad_hasta"": 14, ""obligatoria"": true },
            { ""id"": ""etapa.prueba.media"", ""edad_desde"": 14, ""edad_hasta"": 18, ""obligatoria"": true }
          ]
        }";

        private static ContentDatabase Content(string countryJson = CountryJson)
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", countryJson);
            return content.Build();
        }

        [Fact]
        public void VidaNueva_NaceEnUnLugarDelPaqueteConMadreYPadre()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(5, content);
            Character player = world.Player;

            Assert.Equal("pais.prueba", player.Residence!.CountryId);
            Assert.Equal("ciudad.prueba", player.Residence.CityId);
            Assert.Contains(player.Residence.CommuneId, new[] { "comuna.grande", "comuna.chica" });
            Assert.Equal(0, player.AgeInWeeks(world.Date));
            Assert.Contains(player.FirstName, new[] { "Sofía", "Isidora", "Mateo", "Agustín" });

            Character mother = world.GetCharacter(player.MotherId!.Value);
            Character father = world.GetCharacter(player.FatherId!.Value);
            Assert.Equal(Sex.Female, mother.Sex);
            Assert.Equal(Sex.Male, father.Sex);
            Assert.InRange(mother.AgeInYears(world.Date), 20, 30);
            Assert.True(father.AgeInYears(world.Date) >= 20);
            Assert.Equal(player.Residence.CommuneId, mother.Residence!.CommuneId);
        }

        [Fact]
        public void Apellidos_PrimeroDelPadreLuegoDeLaMadre()
        {
            World world = WorldFactory.CreateNewLife(11, Content());
            Character player = world.Player;
            string fatherFirst = world.GetCharacter(player.FatherId!.Value).LastName.Split(' ')[0];
            string motherFirst = world.GetCharacter(player.MotherId!.Value).LastName.Split(' ')[0];
            Assert.Equal(fatherFirst + " " + motherFirst, player.LastName);
        }

        [Fact]
        public void NivelDelHogar_RespetaElRangoDeLaComuna()
        {
            ContentDatabase content = Content();
            for (ulong seed = 0; seed < 50; seed++)
            {
                Character player = WorldFactory.CreateNewLife(seed, content).Player;
                if (player.Residence!.CommuneId == "comuna.grande")
                {
                    Assert.InRange(player.HouseholdLevel, 20, 40);
                }
                else
                {
                    Assert.InRange(player.HouseholdLevel, 70, 90);
                }
            }
        }

        [Fact]
        public void LugarDeNacimiento_SigueLosPesos()
        {
            ContentDatabase content = Content();
            int chica = Enumerable.Range(0, 1000).Count(i => WorldFactory.CreateNewLife((ulong)i, content).Player.Residence!.CommuneId == "comuna.chica");
            Assert.InRange(chica, 60, 140); // se espera ~100 de 1000
        }

        [Fact]
        public void MismaSemilla_MismaFamilia()
        {
            ContentDatabase content = Content();
            World a = WorldFactory.CreateNewLife(99, content);
            World b = WorldFactory.CreateNewLife(99, content);
            Assert.Equal(a.Player.FirstName + a.Player.LastName, b.Player.FirstName + b.Player.LastName);
            Assert.Equal(a.GetCharacter(a.Player.MotherId!.Value).BirthDate, b.GetCharacter(b.Player.MotherId!.Value).BirthDate);
        }

        [Fact]
        public void EtapasEducativas_CambianConLaEdadYSeAnotan()
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(3, content);
            var cycle = new WeekCycle().Register(new EducationSystem(content));

            cycle.AdvanceWeeks(world, (2 * GameDate.WeeksPerYear) + 1);
            Assert.Equal("etapa.prueba.jardin", world.Player.EducationStageId);

            cycle.AdvanceWeeks(world, 4 * GameDate.WeeksPerYear);
            Assert.Equal("etapa.prueba.basica", world.Player.EducationStageId);

            cycle.AdvanceWeeks(world, 13 * GameDate.WeeksPerYear);
            Assert.Null(world.Player.EducationStageId);

            string[] diary = world.Journal.Select(e => e.TextKey).Where(k => k.StartsWith("diario.etapa.", StringComparison.Ordinal)).ToArray();
            Assert.Equal(new[] { "diario.etapa.prueba.jardin", "diario.etapa.prueba.basica", "diario.etapa.prueba.media" }, diary);
        }

        [Theory]
        [InlineData("jugador.pais == 'pais.prueba'")]
        [InlineData("jugador.ciudad == 'ciudad.prueba'")]
        [InlineData("jugador.madre != nada && jugador.madre.edad >= 20")]
        [InlineData("jugador.padre.sexo == 'masculino'")]
        [InlineData("jugador.nivel_hogar >= 20")]
        [InlineData("jugador.etapa == ''")]
        public void Lenguaje_VeElLugarYLaFamilia(string condition)
        {
            ContentDatabase content = Content();
            World world = WorldFactory.CreateNewLife(5, content);
            var scope = new EventScope(world, content, world.Player, null, readOnly: true);
            Assert.True(Expression.Parse(condition).IsTrue(scope));
        }

        [Fact]
        public void PaisMalArmado_ReportaSusErrores()
        {
            var content = new ContentDatabase();
            content.AddCountryJson("pack.prueba", "country.json", @"{
              ""id"": ""pais.malo"",
              ""nombres"": { ""femeninos"": [], ""masculinos"": [""A""], ""apellidos"": [""B""] },
              ""ciudades"": [ { ""id"": ""ciudad.x"", ""comunas"": [ { ""id"": ""comuna.x"", ""nivel_hogar_min"": 80, ""nivel_hogar_max"": 20 } ] } ],
              ""etapas_educacion"": [
                { ""id"": ""etapa.a"", ""edad_desde"": 6, ""edad_hasta"": 14 },
                { ""id"": ""etapa.b"", ""edad_desde"": 10, ""edad_hasta"": 18 }
              ]
            }");

            var ex = Assert.Throws<ContentLoadException>(() => content.Build());

            Assert.Contains(ex.Errors, e => e.Message.Contains("faltan nombres"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("nivel_hogar"));
            Assert.Contains(ex.Errors, e => e.Message.Contains("solaparse"));
        }

        [Fact]
        public void SinPaises_NoSePuedeCrearUnaVidaCompleta()
        {
            Assert.Throws<InvalidOperationException>(() => WorldFactory.CreateNewLife(1, new ContentDatabase().Build()));
        }

        [Fact]
        public void PackLoader_LeeElArchivoDelPais()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ml-pack-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                File.WriteAllText(Path.Combine(dir, "pack.json"), @"{ ""id"": ""pack.prueba"" }");
                File.WriteAllText(Path.Combine(dir, "country.json"), CountryJson);

                ContentDatabase content = PackLoader.LoadAll(dir);

                Assert.Equal("pack.prueba", content.GetCountry("pais.prueba").PackId);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
