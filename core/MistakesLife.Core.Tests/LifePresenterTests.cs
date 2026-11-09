using System;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Scripting;
using Xunit;

namespace MistakesLife.Core.Tests
{
    public class LifePresenterTests
    {
        private static LifePresenter NewGame(ulong seed) => new LifePresenter(new LifeSession(RepoPacks.Content, seed));

        /// <summary>Avanza hasta que haya un evento esperando decisión.</summary>
        private static LifePresenter UntilEvent(ulong seed)
        {
            LifePresenter presenter = NewGame(seed);
            while (presenter.Phase == LifePhase.Living)
            {
                presenter.AdvanceUntilSomething();
            }

            return presenter;
        }

        [Fact]
        public void Vista_AlNacer_MuestraNombreLugarFamiliaYEdadEnSemanas()
        {
            LifeSnapshot snapshot = NewGame(7).Snapshot();

            Assert.Equal(LifePhase.Living, snapshot.Phase);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.PlayerName));
            Assert.Equal("Puente Alto, Santiago, Chile", snapshot.Place);
            Assert.Equal("0 semanas", snapshot.AgeLabel);
            Assert.Matches(@"^Año \d+, semana \d+$", snapshot.DateLabel);
            Assert.Contains(snapshot.Family, p => p.Relation == "Mamá");
            Assert.Null(snapshot.PendingEvent);
            Assert.Equal(7UL, snapshot.Seed);
            Assert.Equal(1, snapshot.Generation);
        }

        [Fact]
        public void Vista_ConEventoPendiente_MuestraTextoYOpcionesTraducidas()
        {
            LifeSnapshot snapshot = UntilEvent(7).Snapshot();

            Assert.Equal(LifePhase.Choosing, snapshot.Phase);
            EventView view = Assert.IsType<EventView>(snapshot.PendingEvent);
            Assert.NotEqual(view.EventId, view.Text);
            Assert.NotEmpty(view.Options);
            Assert.All(view.Options, o => Assert.False(o.StartsWith("opcion.", StringComparison.Ordinal)));
        }

        [Fact]
        public void Elegir_UnaOpcion_ResuelveElEvento()
        {
            LifePresenter presenter = UntilEvent(7);

            var pending = presenter.Session.Pending;

            presenter.Choose(0);

            Assert.NotSame(pending, presenter.Session.Pending);
        }

        [Fact]
        public void Elegir_PosicionFueraDeRango_DaError()
        {
            LifePresenter presenter = UntilEvent(7);
            int options = presenter.Snapshot().PendingEvent!.Options.Count;

            Assert.Throws<ArgumentOutOfRangeException>(() => presenter.Choose(options));
            Assert.Throws<ArgumentOutOfRangeException>(() => presenter.Choose(-1));
        }

        [Fact]
        public void Avanzar_ConEventoPendiente_NoMueveElTiempo()
        {
            LifePresenter presenter = UntilEvent(7);
            string date = presenter.Snapshot().DateLabel;

            Assert.Equal(0, presenter.AdvanceWeek());
            Assert.Equal(0, presenter.AdvanceUntilSomething());
            Assert.Equal(date, presenter.Snapshot().DateLabel);
        }

        [Fact]
        public void Avanzar_NuncaPasaMasDeCincoAnosSeguidos()
        {
            LifePresenter presenter = NewGame(7);

            int weeks = presenter.AdvanceUntilSomething();

            Assert.InRange(weeks, 1, LifePresenter.MaxYearsPerStep * 52);
        }

        [Fact]
        public void Diario_LineasNuevas_SeEntreganUnaSolaVez()
        {
            LifePresenter presenter = NewGame(7);
            presenter.AdvanceUntilSomething();

            var first = presenter.TakeNewJournal();
            var second = presenter.TakeNewJournal();

            Assert.Contains(first, l => l.Text == "Nací.");
            Assert.Empty(second);
        }

        [Fact]
        public void Vida_HastaLaMuerte_TerminaOEligeHeredero()
        {
            LifePresenter presenter = NewGame(7);
            for (int step = 0; step < 5000 && (presenter.Phase == LifePhase.Living || presenter.Phase == LifePhase.Choosing); step++)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceUntilSomething();
                }
            }

            LifeSnapshot end = presenter.Snapshot();
            Assert.True(end.Phase == LifePhase.Over || end.Phase == LifePhase.ChoosingHeir);
            if (end.Phase == LifePhase.ChoosingHeir)
            {
                Assert.NotEmpty(end.Heirs);
                presenter.ContinueWith(0);
                Assert.Equal(2, presenter.Snapshot().Generation);
                Assert.Equal(end.Heirs[0].Name, presenter.Snapshot().PlayerName);
            }
            else
            {
                Assert.Empty(end.Heirs);
            }
        }

        [Fact]
        public void Dinero_UsaElSeparadorDeMilesDelIdioma()
        {
            LifePresenter presenter = NewGame(7);
            presenter.Session.World.Player.Money = 1250000.4m;

            Assert.Equal("$1.250.000", presenter.Snapshot().MoneyLabel);
        }

        [Fact]
        public void Textos_DeLaPantalla_ExistenEnElIdiomaBase()
        {
            foreach (string key in LifePresenter.TextKeys)
            {
                Assert.NotEqual(key, RepoPacks.Content.Text(key));
            }
        }

        [Fact]
        public void Texto_ConValores_LosPoneEnSuLugar()
        {
            LifePresenter presenter = NewGame(7);

            Assert.Equal("Seguir con Ana (12 años)", presenter.Text("ui.boton.seguir_con", "Ana", 12));
            Assert.Equal("Seguir viviendo", presenter.Text("ui.boton.seguir_viviendo"));
        }

        [Theory]
        [InlineData(0, LifeStage.Baby)]
        [InlineData(1, LifeStage.Baby)]
        [InlineData(2, LifeStage.Child)]
        [InlineData(11, LifeStage.Child)]
        [InlineData(12, LifeStage.Teen)]
        [InlineData(17, LifeStage.Teen)]
        [InlineData(18, LifeStage.Adult)]
        [InlineData(64, LifeStage.Adult)]
        [InlineData(65, LifeStage.Elder)]
        public void Etapa_SegunLaEdad(int age, LifeStage expected)
        {
            Assert.Equal(expected, LifePresenter.StageOf(age));
        }

        [Fact]
        public void Casa_AlNacer_TieneAlJugadorPrimeroYASusPadres()
        {
            LifeSnapshot snapshot = NewGame(7).Snapshot();
            HomeView home = snapshot.Home;

            Assert.True(home.Members[0].IsPlayer);
            Assert.Equal(LifeStage.Baby, home.PlayerStage);
            Assert.Contains(home.Members, m => m.Relation == "Mamá");
            Assert.Equal("comuna.puente_alto", home.CommuneId);
            Assert.InRange(home.HouseholdLevel, 0, 100);
        }

        [Fact]
        public void Casa_DeAdultoMayorDeLaEdadDeIrse_NoIncluyeALosPadres()
        {
            LifePresenter presenter = NewGame(7);
            while (presenter.Snapshot().AgeInYears < 30 && presenter.Phase != LifePhase.Over && presenter.Phase != LifePhase.ChoosingHeir)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceUntilSomething();
                }
            }

            HomeView home = presenter.Snapshot().Home;
            Assert.DoesNotContain(home.Members, m => m.Relation == "Mamá" || m.Relation == "Papá");
            Assert.All(home.Members.Where(m => !m.IsPlayer && m.Relation != "Pareja"), m => Assert.True(m.AgeInYears < 18));
        }

        [Fact]
        public void Casa_JovenSinPareja_SigueConSusPadres()
        {
            LifePresenter presenter = NewGame(7);
            while (presenter.Snapshot().AgeInYears < 20 && presenter.Phase != LifePhase.Over && presenter.Phase != LifePhase.ChoosingHeir)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceWeek();
                }
            }

            var player = presenter.Session.World.Player;
            Assert.True(player.IsAlive);
            Assert.Null(player.PartnerId);
            HomeView home = presenter.Snapshot().Home;
            bool motherAlive = player.MotherId.HasValue && presenter.Session.World.GetCharacter(player.MotherId.Value).IsAlive;
            Assert.Equal(motherAlive, home.Members.Any(m => m.Relation == "Mamá"));
        }

        [Fact]
        public void Casa_FirmaCambiaSoloCuandoCambiaLaCasa()
        {
            LifePresenter presenter = NewGame(7);
            string atBirth = presenter.Snapshot().Home.Signature;

            presenter.AdvanceWeek();
            Assert.Equal(atBirth, presenter.Snapshot().Home.Signature);

            while (presenter.Snapshot().AgeInYears < 3 && presenter.Phase != LifePhase.Over)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceUntilSomething();
                }
            }

            Assert.NotEqual(atBirth, presenter.Snapshot().Home.Signature);
        }

        [Fact]
        public void Lugar_GuaguaSinJardin_EnCasa_YConColegio_AlColegio()
        {
            var baby = new LifePresenter(new LifeSession(RepoPacks.Content, 7)).Session.World.Player;
            baby.EducationStageId = null;
            Assert.Equal(DailyPlace.Home, LifePresenter.PlaceOf(baby));

            baby.EducationStageId = "etapa.chile.basica";
            Assert.Equal(DailyPlace.School, LifePresenter.PlaceOf(baby));
        }

        [Fact]
        public void Lugar_Guagua_DiceElNombreDeSuEtapaYNoColegio()
        {
            LifePresenter presenter = NewGame(7);
            presenter.Session.World.Player.EducationStageId = "etapa.chile.sala_cuna";

            Assert.Equal("Esta semana: Sala cuna", presenter.Snapshot().DailyPlaceLabel);
        }

        [Fact]
        public void Lugar_ConTrabajo_EsElTrabajoAunqueEstudie()
        {
            LifePresenter presenter = NewGame(7);
            for (int step = 0; step < 5000 && presenter.Session.World.Player.Job == null && presenter.Phase != LifePhase.Over && presenter.Phase != LifePhase.ChoosingHeir; step++)
            {
                if (presenter.Phase == LifePhase.Choosing)
                {
                    presenter.Choose(0);
                }
                else
                {
                    presenter.AdvanceUntilSomething();
                }
            }

            Assert.NotNull(presenter.Session.World.Player.Job);
            Assert.Equal(DailyPlace.Work, presenter.Snapshot().DailyPlace);
        }
        [Fact]
        public void Trabajo_SinTrabajo_NoHayLugarDeTrabajo()
        {
            Assert.Null(NewGame(3).Snapshot().Workplace);
        }

        [Fact]
        public void Trabajo_SinTitulo_EsUnLocalSinCarrera()
        {
            LifePresenter presenter = NewGame(3);
            presenter.Session.World.Player.Job = new Job(JobKind.Employee, 500000m, 1);

            WorkplaceView workplace = presenter.Snapshot().Workplace!;
            Assert.Null(workplace.CareerId);
            Assert.Null(workplace.Kind);
            Assert.True(workplace.Player.IsPlayer);
        }

        [Fact]
        public void Trabajo_ConTitulos_UsaLaCarreraDelTituloQueMejorPaga()
        {
            LifePresenter presenter = NewGame(3);
            Character player = presenter.Session.World.Player;
            player.Studies = new CareerStudies("carrera.software", "carrera.software.nivel.ingenieria_civil_computacion", "universidad", 11);
            player.Graduate();
            player.Studies = new CareerStudies("carrera.psicologia", PsychologyLevel(presenter), "universidad", 10);
            player.Graduate();
            player.Job = new Job(JobKind.Freelance, 500000m, 1);

            WorkplaceView workplace = presenter.Snapshot().Workplace!;
            CareerLevelDefinition software = presenter.Session.Content.GetCareerLevel("carrera.software.nivel.ingenieria_civil_computacion");
            CareerLevelDefinition psychology = presenter.Session.Content.GetCareerLevel(PsychologyLevel(presenter));
            string expected = software.SalaryRelative >= psychology.SalaryRelative ? "carrera.software" : "carrera.psicologia";
            Assert.Equal(expected, workplace.CareerId);
            Assert.Equal(presenter.Session.Content.GetCareer(expected).WorkplaceKind, workplace.Kind);
            Assert.True(workplace.Freelance);
        }

        [Fact]
        public void Trabajo_CadaCarreraDelRepo_TieneUnLugarConocido()
        {
            foreach (CareerDefinition career in RepoPacks.Content.Careers)
            {
                Assert.Contains(career.WorkplaceKind, CareerDefinition.WorkplaceKinds);
            }
        }

        [Fact]
        public void Trabajo_ConCargo_MuestraCargoAreaYDesempeno()
        {
            LifePresenter presenter = NewGame(3);
            Character player = presenter.Session.World.Player;
            player.Studies = new CareerStudies("carrera.software", "carrera.software.nivel.ingenieria_civil_computacion", "universidad", 11);
            player.Graduate();
            player.Job = new Job(JobKind.Employee, 500000m, 1, new CareerPosition("carrera.software.area.datos", "carrera.software.cargo.senior"));

            WorkplaceView workplace = presenter.Snapshot().Workplace!;
            Assert.Equal("Senior en Datos e IA · desempeño 50/100", workplace.Caption);
            Assert.Equal(2, workplace.Rank);
        }

        [Fact]
        public void Trabajo_RangoDelCargo_CuentaAscensosDesdeElPrimero()
        {
            CareerDefinition software = RepoPacks.Content.GetCareer("carrera.software");
            Assert.Equal(0, LifePresenter.RankOf(software, RepoPacks.Content.GetCareerPosition("carrera.software.cargo.junior")));
            Assert.Equal(5, LifePresenter.RankOf(software, RepoPacks.Content.GetCareerPosition("carrera.software.cargo.cto")));
        }

        [Fact]
        public void Trabajo_SinCargo_NoTieneFrase()
        {
            LifePresenter presenter = NewGame(3);
            presenter.Session.World.Player.Job = new Job(JobKind.Employee, 500000m, 1);

            Assert.Null(presenter.Snapshot().Workplace!.Caption);
        }

        [Fact]
        public void Trabajo_EnEmpresa_MuestraNombreYTamanoAntesDelCargo()
        {
            LifePresenter presenter = NewGame(3);
            Run(presenter, "estudiar('carrera.software.nivel.analista_programador')");
            Run(presenter, "jugador.titularse()");
            Run(presenter, "jugador.marcar('trabajando')");
            Run(presenter, "jugador.trabajar_en('carrera.software.area.desarrollo')");

            WorkplaceView workplace = presenter.Snapshot().Workplace!;
            Assert.StartsWith("empresa.tamano.", workplace.CompanySizeId);
            string size = presenter.Text(workplace.CompanySizeId!);
            Assert.Contains($", {size} · ", workplace.Caption);
            Assert.DoesNotContain("empresa.", workplace.Caption);
        }

        [Fact]
        public void Trabajo_SinEmpresa_NoTieneTamano()
        {
            LifePresenter presenter = NewGame(3);
            presenter.Session.World.Player.Job = new Job(JobKind.Employee, 500000m, 1);

            Assert.Null(presenter.Snapshot().Workplace!.CompanySizeId);
        }

        private static void Run(LifePresenter presenter, string effect)
        {
            var world = presenter.Session.World;
            Effect.Parse(effect).Apply(new EventScope(world, RepoPacks.Content, world.Player, null, readOnly: false));
        }

        [Fact]
        public void Evento_SelloEstoSeRecuerda_SoloSiAlgunaOpcionDejaSemilla()
        {
            bool sawRemembered = false;
            bool sawForgotten = false;
            for (ulong seed = 1; seed <= 6; seed++)
            {
                LifePresenter presenter = NewGame(seed);
                for (int step = 0; step < 15 && presenter.Phase != LifePhase.Over; step++)
                {
                    while (presenter.Phase == LifePhase.Living)
                    {
                        presenter.AdvanceUntilSomething();
                    }

                    if (presenter.Phase != LifePhase.Choosing)
                    {
                        break;
                    }

                    EventView view = presenter.Snapshot().PendingEvent!;
                    PendingEvent pending = presenter.Session.Pending!;
                    EventDefinition definition = RepoPacks.Content.GetEvent(pending.EventId);
                    bool plants = pending.AvailableOptions.Any(i => definition.Options[i].Effects.Any(e => e.Contains("plantar(")));

                    Assert.Equal(plants, view.Remembered);
                    Assert.Equal(view.Options.Count, view.NoticedBy.Count);
                    sawRemembered |= plants;
                    sawForgotten |= !plants;
                    presenter.Choose(0);
                }
            }

            Assert.True(sawRemembered);
            Assert.True(sawForgotten);
        }

        [Fact]
        public void Evento_TextosDeTestigos_ExistenEnEspanol()
        {
            LifePresenter presenter = NewGame(1);

            Assert.Equal("Lo va a notar Pedro", presenter.Text("ui.evento.lo_nota", "Pedro"));
            Assert.Equal("Te miran: Pedro y Ana", presenter.Text("ui.evento.miran", "Pedro" + presenter.Text("ui.lista.y") + "Ana"));
            Assert.Equal("Esto se recuerda", presenter.Text("ui.evento.se_recuerda"));
        }

        [Fact]
        public void Familia_Viva_DiceComoEstaLaRelacion()
        {
            LifeSnapshot snapshot = NewGame(7).Snapshot();

            PersonView mother = Assert.Single(snapshot.Family, p => p.Relation == "Mamá");
            Assert.Equal("muy unidos", mother.Feeling);
        }

        [Fact]
        public void Gente_AlConocerAmistadYRival_ApareceConSuRelacionYElRencor()
        {
            LifePresenter presenter = NewGame(1);
            Assert.Empty(presenter.Snapshot().People);

            Run(presenter, "jugador.conocer('amistad')");
            Run(presenter, "jugador.conocer('rival')");
            LifeSnapshot snapshot = presenter.Snapshot();

            Assert.Equal(2, snapshot.People.Count);
            Assert.Contains(snapshot.People, p => p.Relation == "Amistad" && p.Feeling.Length > 0);
            PersonView rival = Assert.Single(snapshot.People, p => p.Relation == "Rival");
            Assert.Equal("te guarda rencor", rival.Feeling);
            Assert.DoesNotContain(snapshot.Family, p => p.Relation == "Amistad" || p.Relation == "Rival");
        }

        [Fact]
        public void Gente_ConMuchasAmistades_MuestraSoloLasMasCercanasHastaElMaximo()
        {
            LifePresenter presenter = NewGame(1);
            for (int i = 0; i < LifePresenter.MaxPeople + 3; i++)
            {
                Run(presenter, "jugador.conocer('amistad')");
            }

            Assert.Equal(LifePresenter.MaxPeople, presenter.Snapshot().People.Count);
        }

        [Theory]
        [InlineData(90, 0, "ui.sentir.muy_unidos")]
        [InlineData(60, 10, "ui.sentir.buena")]
        [InlineData(40, 0, "ui.sentir.tibia")]
        [InlineData(10, 0, "ui.sentir.distanciados")]
        [InlineData(90, 60, "ui.sentir.rencor")]
        public void Sentir_ElRencorFuertePesaMasQueElCarino(int affection, int resentment, string expected)
        {
            var feelings = new Relationship(2, affection, 50, 50, resentment);

            Assert.Equal(expected, LifePresenter.FeelingKey(feelings));
        }

        [Theory]
        [InlineData(0, "ui.estado.salud.0")]
        [InlineData(19, "ui.estado.salud.0")]
        [InlineData(20, "ui.estado.salud.1")]
        [InlineData(59, "ui.estado.salud.2")]
        [InlineData(60, "ui.estado.salud.3")]
        [InlineData(100, "ui.estado.salud.4")]
        public void Estado_EnPalabras_UsaCincoTramosDeVeintePuntos(int value, string expected)
        {
            Assert.Equal(expected, LifePresenter.StateWordKey("salud", value));
        }

        [Fact]
        public void Estado_EnPalabras_TieneTextoParaCadaTramoYEstado()
        {
            LifePresenter presenter = NewGame(7);
            foreach (string state in LifePresenter.States)
            {
                for (int value = 0; value <= 100; value += 20)
                {
                    string word = presenter.StateWord(state, value);
                    Assert.False(word.StartsWith("ui.", StringComparison.Ordinal), $"Falta el texto de {LifePresenter.StateWordKey(state, value)}");
                }
            }
        }

        private static string PsychologyLevel(LifePresenter presenter) =>
            presenter.Session.Content.GetCareer("carrera.psicologia").Levels[0].Id;
    }
}
