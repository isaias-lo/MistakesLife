using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Education;
using MistakesLife.Core.Game;
using MistakesLife.Core.Life;
using MistakesLife.Core.Presentation;
using MistakesLife.Core.Randomness;
using Xunit;

namespace MistakesLife.Core.Tests
{
    /// <summary>Estudiar de verdad (D5a en docs/roadmap.md): dominio por ramo, tardes con preguntas reales, compañera y profe.</summary>
    public class StudyTests
    {
        private static ContentDatabase Content => RepoPacks.Content;

        private static CountryDefinition Chile => Content.GetCountry("pais.chile");

        private const string Math5 = "ramo.chile.matematica";

        /// <summary>Una vida entera jugada hasta tener clases a esa edad, con el horario libre y descansada.</summary>
        private static LifeSession InClass(int age, ulong seed = 3)
        {
            var session = new LifeSession(Content, seed, "pais.chile");
            session.TimeMachine.Enabled = false;
            Character player = session.World.Player;
            while (player.IsAlive && (player.AgeInYears(session.World.Date) < age || !SchoolRules.InClasses(session.World, Content, player)
                || StudyRules.Sets(session.World, Content, player).Count == 0))
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(1);
            }

            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            player.Routine = new Routine("libre");
            player.Stats.Energy = 80;
            return session;
        }

        private static Character Player(LifeSession session) => session.World.Player;

        private static int Mastery(LifeSession session, string subject) => StudyRules.Mastery(Player(session), subject)!.Value;

        /// <summary>Responde bien (o mal) la pregunta que va.</summary>
        private static StudyAnswer Answer(LifeSession session, bool right)
        {
            StudyQuestion question = session.Study!.Current!;
            int position = right ? question.CorrectPosition : (question.CorrectPosition + 1) % question.Order.Count;
            return session.AnswerStudy(position);
        }

        [Fact]
        public void Preguntas_DeChile_CubrenLosCuatroRamosDeBasica_ConCuarentaPorCurso()
        {
            for (int level = 1; level <= 8; level++)
            {
                string language = level <= 6 ? "ramo.chile.lenguaje" : "ramo.chile.lengua_literatura";
                foreach (string subject in new[] { language, "ramo.chile.matematica", "ramo.chile.historia", "ramo.chile.ciencias_naturales" })
                {
                    QuestionSetDefinition? set = Content.QuestionSet("etapa.chile.basica", level, subject);
                    Assert.True(set != null, $"Faltan las preguntas de {subject} en {level}º básico.");
                    Assert.Equal(4, set!.Units.Count);
                    Assert.True(set.Units.Sum(u => u.Questions.Count) >= 40, $"{subject} {level}º: menos de 40 preguntas.");
                }
            }
        }

        [Fact]
        public void Preguntas_DeChile_CubrenTodosLosRamosConPruebaEscrita_DeBasicaYMedia()
        {
            CountryDefinition chile = Content.GetCountry("pais.chile");
            int sets = 0;
            foreach (string stageId in new[] { "etapa.chile.basica", "etapa.chile.media" })
            {
                EducationStageDefinition stage = chile.EducationStages.Single(e => e.Id == stageId);
                int levels = stageId == "etapa.chile.basica" ? 8 : 4;
                for (int level = 1; level <= levels; level++)
                {
                    foreach (SubjectDefinition subject in stage.Subjects.Where(s => s.WrittenTests && s.InLevel(level)))
                    {
                        QuestionSetDefinition? set = Content.QuestionSet(stageId, level, subject.Id);
                        Assert.True(set != null, $"Faltan las preguntas de {subject.Id} en {stageId} {level}º.");
                        Assert.True(set!.Units.Sum(u => u.Questions.Count) >= 20, $"{subject.Id} {level}º: menos de 20 preguntas.");
                        sets++;
                    }
                }
            }
            // 8 años de básica (5 o 6 ramos) y 4 de media (6 a 8 ramos).
            Assert.Equal(72, sets);
        }

        [Fact]
        public void Preguntas_LaCorrectaNoEstaSiempreEnLaMismaLetra()
        {
            var letters = Content.QuestionSets.SelectMany(s => s.Units).SelectMany(u => u.Questions).GroupBy(q => q.Correct).ToDictionary(g => g.Key, g => g.Count());
            Assert.True(letters.Count >= 3);
            Assert.True(letters.Values.Min() > letters.Values.Sum() / 5);
        }

        [Fact]
        public void Preguntas_ConLaCorrectaFueraDeSusAlternativas_NoCarga()
        {
            PackBundle real = PackBundle.FromDirectory(RepoPacks.Directory);
            var files = real.Files.ToDictionary(f => f.Key, f => f.Value);
            string key = files.Keys.Single(k => k.EndsWith("preguntas/basica/matematica_5.json", StringComparison.Ordinal));
            files[key] = files[key].Replace("\"correcta\": \"a\"", "\"correcta\": \"d\"");
            var error = Assert.Throws<ContentLoadException>(() => PackLoader.LoadBundle(new PackBundle(files)));
            Assert.Contains(error.Errors, e => e.Message.Contains("correcta"));
        }

        [Fact]
        public void Dominio_EmpiezaElAnoBajoLaBase_YLasClasesLoAcercan()
        {
            LifeSession session = InClass(10);
            int classLevel = StudyRules.ClassLevel(session.World, Content, Player(session));
            SchoolRecord record = Player(session).SchoolRecord!;
            record.Mastery[Math5] = classLevel - 20;
            for (int i = 0; i < 4; i++)
            {
                StudyRules.Week(session.World, Content, Player(session));
            }

            Assert.InRange(Mastery(session, Math5), classLevel - 16, classLevel - 1);
            Assert.Equal(39, StudyRules.Start(null, 50));
            Assert.True(StudyRules.Start(90, 50) > StudyRules.Start(20, 50), "Quien dominaba más el año pasado parte más arriba.");
        }

        [Fact]
        public void SinEstudiar_SeOlvidaDeAPoco_HastaLaBase()
        {
            LifeSession session = InClass(10);
            int classLevel = StudyRules.ClassLevel(session.World, Content, Player(session));
            SchoolRecord record = Player(session).SchoolRecord!;
            record.Mastery[Math5] = classLevel + 10;
            record.LastStudiedWeek[Math5] = session.World.Date.TotalWeeks - 10;
            StudyRules.Week(session.World, Content, Player(session));
            Assert.Equal(classLevel + 9, Mastery(session, Math5));

            // Un punto cada dos semanas: la semana impar no se olvida.
            record.LastStudiedWeek[Math5] = session.World.Date.TotalWeeks - 11;
            StudyRules.Week(session.World, Content, Player(session));
            Assert.Equal(classLevel + 9, Mastery(session, Math5));

            // Recién estudiado, no se olvida.
            record.LastStudiedWeek[Math5] = session.World.Date.TotalWeeks;
            StudyRules.Week(session.World, Content, Player(session));
            Assert.Equal(classLevel + 9, Mastery(session, Math5));
        }

        [Fact]
        public void TardeDeEstudio_GastaUnaTarde_YTraeTresPreguntasDeLaUnidadQueVa()
        {
            LifeSession session = InClass(10);
            int before = FreeTimeRules.Left(session.World, Content, Player(session));
            StudySession study = session.StartStudy(Math5, null, false);

            Assert.Equal(before - 1, FreeTimeRules.Left(session.World, Content, Player(session)));
            Assert.Equal(3, study.Questions.Count);
            Assert.Equal(3, study.Questions.Select(q => q.Definition.Id).Distinct().Count());
            QuestionSetDefinition set = StudyRules.Set(session.World, Content, Player(session), Math5)!;
            Assert.Same(StudyRules.CurrentUnit(Chile, session.World.Date, set), study.Unit);
            Assert.All(study.Questions, q => Assert.Contains(q.Definition, study.Unit.Questions));
        }

        [Fact]
        public void PreguntasVistas_NoSeRepitenEnElAno()
        {
            LifeSession session = InClass(10);
            var seen = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                Player(session).FreeTime = null;
                StudySession study = session.StartStudy(Math5, null, false);
                seen.AddRange(study.Questions.Select(q => q.Definition.Id));
                session.FinishStudy();
            }

            Assert.Equal(seen.Count, seen.Distinct().Count());
        }

        [Fact]
        public void Acierto_SubeElDominio_YUnErrorNoLoBaja()
        {
            LifeSession session = InClass(10);
            session.StartStudy(Math5, null, false);
            int start = Mastery(session, Math5);

            StudyAnswer right = Answer(session, true);
            Assert.True(right.Correct);
            Assert.Equal(StudyRules.Gain(start), right.MasteryGain);

            StudyAnswer wrong = Answer(session, false);
            Assert.False(wrong.Correct);
            Assert.Equal(0, wrong.MasteryGain);
            Assert.Equal(start + right.MasteryGain, Mastery(session, Math5));
        }

        [Fact]
        public void ConLaProfe_UnErrorIgualSubeUnPoco_YSoloUnaVezAlMes()
        {
            LifeSession session = InClass(10);
            Assert.NotNull(StudyRules.Teacher(session.World, Player(session)));
            session.StartStudy(Math5, null, true);
            Assert.Equal(StudyRules.GainExplained, Answer(session, false).MasteryGain);
            session.FinishStudy();

            Assert.Null(StudyRules.Teacher(session.World, Player(session)));
            Assert.Throws<InvalidOperationException>(() => session.StartStudy(Math5, null, true));
        }

        [Fact]
        public void ConUnaCompanera_SeQuierenMasLosDos_YElLaRespondeConLoQueSabe()
        {
            LifeSession session = InClass(10);
            RelationshipRules.Edit(session.World, SchoolRules.Classmates(session.World, Player(session)).First(), Player(session)).Affection = 60;
            Character mate = StudyRules.Companions(session.World, Player(session)).First();

            int before = RelationshipRules.Feelings(session.World, mate, Player(session)).Affection;
            StudySession study = session.StartStudy(Math5, mate.Id, false);
            Assert.All(study.Questions, q => Assert.True(q.CompanionCorrect.HasValue));
            StudyOutcome outcome = session.FinishStudy();

            Assert.Equal(before + StudyRules.CompanionAffection, RelationshipRules.Feelings(session.World, mate, Player(session)).Affection);
            Assert.Contains(outcome.Changes, c => c.Key == "carino");
            Assert.DoesNotContain(StudyRules.Companions(session.World, Player(session)), c => c.Id == mate.Id);
        }

        [Fact]
        public void EstudiarElRamoDeLaPrueba_LaDejaMasPreparada()
        {
            LifeSession session = InClass(10);
            SchoolRecord record = Player(session).SchoolRecord!;
            for (int week = 0; week < 80 && !(record.TestPending && StudyRules.Set(session.World, Content, Player(session), record.TestSubjectId!) != null && SchoolRules.InClasses(session.World, Content, Player(session))); week++)
            {
                if (session.Pending != null)
                {
                    session.Choose(session.Pending.AvailableOptions[0]);
                    continue;
                }

                session.Advance(1);
            }

            while (session.Pending != null)
            {
                session.Choose(session.Pending.AvailableOptions[0]);
            }

            Assert.True(record.TestPending, "En un año se anuncia alguna prueba.");
            int before = record.TestPreparation;
            Player(session).Routine = new Routine("libre");
            Player(session).FreeTime = null;
            session.StartStudy(record.TestSubjectId!, null, false);
            StudyOutcome outcome = session.FinishStudy();
            Assert.Equal(Math.Min(GradeRules.MaxPreparation, before + 1), record.TestPreparation);
        }

        [Fact]
        public void MasDominio_MejorNota_YCansadoRindeMenos()
        {
            LifeSession session = InClass(10);
            GradeScaleDefinition scale = Chile.GradeScale!;
            SchoolRecord record = Player(session).SchoolRecord!;
            record.Mastery[Math5] = 30;
            double low = GradeRules.Expected(session.World, Content, Player(session), scale, Math5, 0);
            record.Mastery[Math5] = 90;
            double high = GradeRules.Expected(session.World, Content, Player(session), scale, Math5, 0);
            Assert.True(high - low > 1.5, $"Con 90 de dominio ({high}) va mucho mejor que con 30 ({low}).");

            Player(session).Stats.Energy = 5;
            Assert.True(GradeRules.Expected(session.World, Content, Player(session), scale, Math5, 0) < high);
        }

        [Fact]
        public void Recuperativa_QuienFaltaEnfermoTieneMasProbabilidad()
        {
            LifeSession session = InClass(10);
            Player(session).Stats.Health = 90;
            double healthy = GradeRules.AbsentChance(Player(session));
            Player(session).Stats.Health = 20;
            Assert.True(GradeRules.AbsentChance(Player(session)) > healthy * 3);
        }

        [Fact]
        public void LasAccionesViejasDeEstudiar_NoSalenEnUnCursoConPreguntas()
        {
            LifeSession session = InClass(10);
            var ids = ActionRules.Available(session.World, Content, Player(session), "estudios").Select(o => o.Definition.Id).ToList();
            Assert.DoesNotContain("accion.repasar", ids);
            Assert.DoesNotContain("accion.estudiar_con", ids);
        }

        [Fact]
        public void Guardar_ConservaElDominioYLasPreguntasVistas()
        {
            LifeSession session = InClass(10);
            session.StartStudy(Math5, null, false);
            Answer(session, true);
            session.FinishStudy();

            LifeSession loaded = LifeSession.Load(Content, session.SaveToText());
            SchoolRecord before = Player(session).SchoolRecord!;
            SchoolRecord after = loaded.World.Player.SchoolRecord!;
            Assert.Equal(before.Mastery, after.Mastery);
            Assert.Equal(before.SeenQuestions, after.SeenQuestions);
            Assert.Equal(before.LastStudiedWeek, after.LastStudiedWeek);
            Assert.Equal(before.MasteryYear, after.MasteryYear);
            Assert.Equal(before.StudySessions, after.StudySessions);
        }

        [Fact]
        public void Presentador_DeLaListaAlResultado()
        {
            LifeSession session = InClass(10);
            var presenter = new LifePresenter(session);
            IReadOnlyList<StudySubjectView> subjects = presenter.StudySubjects();
            Assert.Equal(5, subjects.Count); // Lenguaje, Matemática, Historia, Ciencias y Tecnología (4º básico)
            Assert.All(subjects, s => Assert.StartsWith("Ahora: ", s.Topic));
            Assert.All(subjects, s => Assert.True(s.CanStudy));

            StudyIntroView intro = presenter.StudyIntro(Math5);
            Assert.False(string.IsNullOrWhiteSpace(intro.Reading));
            Assert.Contains("Mineduc", intro.Note);
            Assert.Null(intro.Partners[0].PersonId);

            StudyQuestionView question = presenter.StartStudy(Math5, null, false);
            Assert.Equal(1, question.Number);
            Assert.Equal(3, question.Options.Count);
            Assert.DoesNotContain(question.Options, o => o.StartsWith("pregunta.", StringComparison.Ordinal));

            StudyAnswerView answer = presenter.AnswerStudy(session.Study!.Current!.CorrectPosition);
            Assert.True(answer.Correct);
            Assert.Equal("¡Bien!", answer.Verdict);
            Assert.True(answer.HasNext);

            StudyResultView result = presenter.FinishStudy();
            Assert.Equal("1 de 3 correctas", result.Score);
            Assert.StartsWith("Estudiaste ", result.Title);
            Assert.Contains(result.Changes, c => !c.IsGood);
        }
    }
}
