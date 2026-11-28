using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Devices;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Education;
using MistakesLife.Core.Events;
using MistakesLife.Core.Life;
using MistakesLife.Core.Saving;
using MistakesLife.Core.Seeds;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Game
{
    /// <summary>
    /// Una partida lista para jugar: arma el mundo y el ciclo semanal con todos
    /// los sistemas, en el orden correcto. La usan la consola, el simulador de
    /// vidas y, más adelante, Unity. Ninguno de ellos debe armar el ciclo a mano.
    /// </summary>
    public sealed class LifeSession
    {
        private readonly WeekCycle _cycle;

        private readonly RoutineSystem _routine;
        private int _journalRead;

        public LifeSession(ContentDatabase content, ulong seed)
            : this(content, seed, new SeedRules(), new EventRules())
        {
        }

        public LifeSession(ContentDatabase content, ulong seed, SeedRules seedRules, EventRules eventRules)
            : this(content, seed, null, seedRules, eventRules)
        {
        }

        /// <summary>
        /// Partida nueva que nace en un país elegido (por ejemplo "pais.argentina").
        /// Con null, el país sale al azar según los paquetes.
        /// </summary>
        public LifeSession(ContentDatabase content, ulong seed, string? countryId)
            : this(content, seed, countryId, new SeedRules(), new EventRules())
        {
        }

        public LifeSession(ContentDatabase content, ulong seed, string? countryId, SeedRules seedRules, EventRules eventRules)
            : this(content ?? throw new ArgumentNullException(nameof(content)), NewWorld(content, seed, countryId), seedRules, eventRules, 0)
        {
            TimeMachine.SetCharges(TimeMachine.StartingCharges);
        }

        /// <summary>
        /// Arma la partida sobre un mundo que ya existe (por ejemplo, uno cargado
        /// desde un guardado). El ciclo semanal se arma igual que en una partida nueva.
        /// </summary>
        internal LifeSession(ContentDatabase content, World world, SeedRules seedRules, EventRules eventRules, int journalRead)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            World = world ?? throw new ArgumentNullException(nameof(world));
            _journalRead = journalRead;
            TimeMachine = new TimeMachine(0);
            Events = new EventSystem(content, eventRules);
            _routine = new RoutineSystem(content);
            _cycle = new WeekCycle()
                .Register(new Education.SchoolSystem(content))
                .Register(_routine)
                .Register(new EconomySystem(content))
                .Register(new NewsSystem(content))
                .Register(new Courts.CourtSystem(content))
                .Register(new Work.OccupationSystem(content))
                .Register(new PayrollSystem(content))
                .Register(new Work.VacationSystem(content))
                .Register(new Work.VentureSystem(content))
                .Register(new PensionSystem(content))
                .Register(new UnemploymentInsuranceSystem(content))
                .Register(new TaxSystem(content))
                .Register(new ExpenseSystem(content))
                .Register(new DebtSystem(content))
                .Register(new EducationSystem(content))
                .Register(new Education.StudySystem(content))
                .Register(new Education.GradeSystem(content))
                .Register(new Education.StudentCouncilSystem(content))
                .Register(new CareerSystem())
                .Register(new Education.UniversitySystem(content))
                .Register(new CompanySystem(content))
                .Register(new JobSearchSystem(content))
                .Register(new Work.WorkSystem(content))
                .Register(new Work.WorkTaskSystem(content))
                .Register(new AgingSystem())
                .Register(new EnergySystem(content))
                .Register(new LooksSystem(content))
                .Register(new MortalitySystem(content))
                .Register(new GriefSystem())
                .Register(new RelativesFamilySystem(content))
                .Register(new PeopleWorkSystem(content))
                .Register(new InterestingPeopleSystem(content))
                .Register(new RelationshipSystem())
                .Register(new SeedSystem(seedRules))
                .Register(Events)
                .Register(new BirthdaySystem())
                .Register(new MailSystem(content))
                .Register(new ChatSystem(content))
                .Register(new WishSystem(content));
        }

        private static World NewWorld(ContentDatabase content, ulong seed, string? countryId)
        {
            World world = WorldFactory.CreateNewLife(seed, content, countryId);
            EconomySystem.Initialize(world, content);
            return world;
        }

        /// <summary>
        /// Carga una partida guardada con <see cref="SaveToText"/>. El contenido
        /// (paquetes) debe ser el mismo con que se jugó, o una versión más nueva.
        /// </summary>
        /// <exception cref="SaveFormatException">Si el texto no es un guardado válido.</exception>
        public static LifeSession Load(ContentDatabase content, string text) =>
            SaveGame.FromText(content, text, new SeedRules(), new EventRules());

        /// <summary>Igual que <see cref="Load(ContentDatabase, string)"/>, con reglas de semillas y eventos propias.</summary>
        public static LifeSession Load(ContentDatabase content, string text, SeedRules seedRules, EventRules eventRules) =>
            SaveGame.FromText(content, text, seedRules, eventRules);

        /// <summary>Carga una partida guardada con <see cref="SaveToBytes"/>.</summary>
        /// <exception cref="SaveFormatException">Si los bytes no son un guardado válido.</exception>
        public static LifeSession LoadFromBytes(ContentDatabase content, byte[] data) =>
            SaveGame.FromBytes(content, data, new SeedRules(), new EventRules());

        /// <summary>
        /// Guarda la partida completa como texto (JSON). No escribe archivos: quien
        /// la usa (Unity, la consola) decide dónde dejarla.
        /// </summary>
        public string SaveToText() => SaveGame.ToText(this);

        /// <summary>Guarda la partida completa comprimida, más liviana para el celular.</summary>
        public byte[] SaveToBytes() => SaveGame.ToBytes(this);

        /// <summary>Cuántas líneas del diario ya se entregaron con <see cref="TakeNewJournalEntries"/>.</summary>
        internal int JournalRead => _journalRead;

        public ContentDatabase Content { get; }

        /// <summary>
        /// El mundo de la partida. Cambia por otro al volver al pasado
        /// (<see cref="GoBack(int)"/>): no conviene guardarlo aparte, siempre
        /// pedirlo aquí.
        /// </summary>
        public World World { get; private set; }

        /// <summary>Volver al pasado: fotos del mundo, cargas y etapas ya vividas de nuevo.</summary>
        public TimeMachine TimeMachine { get; }

        public EventSystem Events { get; }

        /// <summary>El evento que espera la decisión del jugador, si lo hay.</summary>
        public PendingEvent? Pending => World.PendingEvents.FirstOrDefault(p => p.CharacterId == World.PlayerId);

        public int PlayerAgeInYears => World.Player.AgeInYears(World.Date);

        /// <summary>
        /// Avanza hasta <paramref name="maxWeeks"/> semanas; se detiene antes si
        /// llega un evento o un día clave. Devuelve las semanas que pasaron.
        /// </summary>
        public int Advance(int maxWeeks)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Primero hay que responder el evento pendiente.");
            }

            // Una tarde de estudio a medias termina con lo que alcanzó a responder.
            if (Study != null && !Study.Finished)
            {
                FinishStudy();
            }

            Study = null;

            // Con la máquina del tiempo encendida, el tiempo se para un momento
            // al empezar cada etapa de la vida, para sacar su foto, y sigue.
            // La primera foto (la del nacimiento, o la de una partida vieja) se saca
            // al empezar a avanzar.
            TakeSnapshots();
            int total = 0;
            while (total < maxWeeks && World.Player.IsAlive)
            {
                int step = maxWeeks - total;
                int? toNextStage = TimeMachine.Enabled ? LifeStages.WeeksToNextStage(World.Player.AgeInWeeks(World.Date)) : null;
                if (toNextStage.HasValue && toNextStage.Value < step)
                {
                    step = toNextStage.Value;
                }

                total += _cycle.AdvanceWeeks(World, step, out bool stopped);
                TakeSnapshots();
                if (stopped)
                {
                    break;
                }
            }

            return total;
        }

        /// <summary>
        /// Vuelve al pasado: la partida sigue desde esa foto del mundo, como si lo
        /// que vino después no hubiera pasado. Gasta una carga y la etapa de la
        /// foto ya no se puede volver a vivir. El jugador conserva una intuición
        /// en su diario y, si la foto es de antes de una decisión, la opción que
        /// eligió la otra vez queda en <see cref="TimeMachine.PreviousChoice"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si no quedan cargas, la etapa ya se volvió a vivir o la foto no existe.</exception>
        public void GoBack(int snapshotId)
        {
            WorldSnapshot snapshot = TimeMachine.Snapshots.FirstOrDefault(s => s.Id == snapshotId)
                ?? throw new InvalidOperationException($"No hay una foto del mundo número {snapshotId}.");

            // Lo que eligió esa vez, antes de que ese futuro se borre.
            int? previous = null;
            if (snapshot.Kind == SnapshotKind.BeforeDecision)
            {
                previous = World.EventHistory
                    .FirstOrDefault(r => r.CharacterId == snapshot.PlayerId && r.EventId == snapshot.EventId && r.Date >= snapshot.Date)
                    ?.ChosenOption;
            }

            TimeMachine.Spend(snapshot);
            World = SaveGame.ReadSnapshot(Content, snapshot.Data, out int journalRead);
            _journalRead = journalRead;
            PendingEvent? again = Pending;
            if (previous.HasValue && again != null && again.EventId == snapshot.EventId)
            {
                TimeMachine.RememberChoice(again.EventId, again.CharacterId, again.Date, previous.Value);
            }

            World.WriteJournal(World.PlayerId, previous.HasValue ? JournalIntuitionDecision : JournalIntuition);
        }

        /// <summary>Diario al volver al pasado.</summary>
        public const string JournalIntuition = "diario.intuicion";

        /// <summary>Diario al volver a una decisión que ya se había tomado.</summary>
        public const string JournalIntuitionDecision = "diario.intuicion_decision";

        /// <summary>
        /// Saca las fotos que faltan: al empezar la etapa de la vida en que va el
        /// jugador y antes de una decisión de día clave que espera respuesta.
        /// </summary>
        private void TakeSnapshots()
        {
            Character player = World.Player;
            if (!TimeMachine.Enabled || !player.IsAlive)
            {
                return;
            }

            int age = player.AgeInYears(World.Date);
            string stage = LifeStages.StageAt(age);
            if (!TimeMachine.HasStageSnapshot(player.Id, stage))
            {
                TimeMachine.Add(NewSnapshot(SnapshotKind.StageStart, age, stage, null));
            }

            PendingEvent? pending = Pending;
            if (pending != null && Content.GetEvent(pending.EventId).KeyDay && !TimeMachine.HasDecisionSnapshot(pending, World.Date))
            {
                TimeMachine.Add(NewSnapshot(SnapshotKind.BeforeDecision, age, stage, pending.EventId));
            }
        }

        private WorldSnapshot NewSnapshot(SnapshotKind kind, int age, string stage, string? eventId) =>
            new WorldSnapshot(TimeMachine.NextSnapshotId, kind, World.Date, World.PlayerId, age, stage, eventId, SaveGame.SnapshotBytes(World, _journalRead));

        /// <summary>
        /// Los hijos vivos con quienes se puede seguir jugando cuando el jugador
        /// muere (del mayor al menor). Vacío mientras el jugador vive.
        /// </summary>
        public IReadOnlyList<Character> Heirs =>
            World.Player.IsAlive
                ? Array.Empty<Character>()
                : World.ChildrenOf(World.PlayerId).Where(c => c.IsAlive).ToList();

        /// <summary>La partida terminó: el jugador murió y no tiene hijos vivos.</summary>
        public bool IsOver => !World.Player.IsAlive && Heirs.Count == 0;

        /// <summary>
        /// Legado: el jugador murió y se sigue jugando con uno de sus hijos, en el
        /// mismo mundo. Las semillas heredables pasan al hijo; las demás siguen vivas
        /// a través de sus testigos.
        /// </summary>
        public void ContinueWith(int heirId)
        {
            Character deceased = World.Player;
            if (deceased.IsAlive)
            {
                throw new InvalidOperationException("El jugador sigue vivo: todavía no hay legado.");
            }

            Character heir = Heirs.FirstOrDefault(c => c.Id == heirId)
                ?? throw new InvalidOperationException($"El personaje {heirId} no es un hijo vivo del jugador.");

            foreach (PendingEvent pending in World.PendingEvents.Where(p => p.CharacterId == deceased.Id).ToList())
            {
                World.RemovePendingEvent(pending);
            }

            World.Seeds.PassToHeir(deceased.Id, heir.Id);
            GivePriorLife(heir);
            World.SetPlayer(heir.Id);
            World.WriteJournal(heir.Id, deceased.Gender == Gender.Female ? "diario.muerte_madre" : "diario.muerte_padre");
            World.WriteJournal(heir.Id, "diario.legado");
            TimeMachine.StartNewGeneration();
            TakeSnapshots();
        }

        /// <summary>Da por vivido lo que su país dice para su edad (campo vida_previa).</summary>
        private void GivePriorLife(Character heir)
        {
            if (heir.Residence == null)
            {
                return;
            }

            int age = heir.AgeInYears(World.Date);
            foreach (PriorLifeDefinition prior in Content.GetCountry(heir.Residence.CountryId).PriorLife.Where(p => p.Age <= age))
            {
                foreach (string flag in prior.Flags)
                {
                    heir.SetFlag(flag);
                }

                if (prior.Housing != null && heir.Lease == null)
                {
                    Economy.HousingRules.AssumeLease(World, Content, heir, prior.Housing);
                }

                if (prior.GroceryBasket != null && heir.GroceryBasketId == null)
                {
                    heir.GroceryBasketId = prior.GroceryBasket;
                }
            }
        }

        /// <summary>Responde el evento pendiente con una de sus opciones disponibles.</summary>
        public void Choose(int optionIndex)
        {
            PendingEvent pending = Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            Events.Choose(World, pending, optionIndex);
            TakeSnapshots();
        }

        /// <summary>
        /// Responde el evento pendiente y deja una regla propia: si vuelve a pasar (este
        /// evento o uno de su grupo, como "cuando hay prueba"), se elige lo mismo sin preguntar.
        /// Reemplaza la regla que hubiera para lo mismo.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si este evento no admite regla (un día clave o un error que vuelve).</exception>
        public void ChooseAlways(int optionIndex)
        {
            PendingEvent pending = Pending ?? throw new InvalidOperationException("No hay un evento pendiente.");
            EventDefinition definition = Content.GetEvent(pending.EventId);
            if (!definition.AllowsOwnRule)
            {
                throw new InvalidOperationException("Este momento se vive cada vez: no admite regla.");
            }

            if (!pending.AvailableOptions.Contains(optionIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex), "Esa opción no está disponible.");
            }

            List<OwnRule> rules = World.Player.OwnRules;
            rules.RemoveAll(r => r.Key == definition.RuleKey);
            rules.Add(new OwnRule(definition.RuleKey, definition.Options[optionIndex].RuleKey));
            Choose(optionIndex);
        }

        /// <summary>Borra una regla propia: la próxima vez se vuelve a preguntar.</summary>
        public void RemoveRule(string key) => World.Player.OwnRules.RemoveAll(r => r.Key == key);

        /// <summary>Marca (o desmarca) con destacador algo que quiere: lo persigue.</summary>
        public void HighlightWish(int index, bool highlighted)
        {
            List<Wish> wishes = World.Player.Wishes;
            if (index < 0 || index >= wishes.Count || wishes[index].State != WishState.Open)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Eso ya no es algo que quiera ahora.");
            }

            wishes[index].Highlighted = highlighted;
        }

        /// <summary>Los momentos que el jugador puede pedir ahora (pedir un aumento, tomar vacaciones...).</summary>
        public IReadOnlyList<Content.EventDefinition> Requestable() => Events.Requestable(World);

        /// <summary>El jugador pide un momento con su botón: si se puede, queda pendiente ahora. Devuelve si pasó.</summary>
        public bool Request(string eventId) => Events.Request(World, eventId);

        /// <summary>Los momentos que el jugador puede pedir ahora con esa persona (recomendarla, arrendar juntos...).</summary>
        public IReadOnlyList<Content.EventDefinition> RequestableWith(int personId) => Events.RequestableWith(World, personId);

        /// <summary>El jugador pide un momento con esa persona: si se puede, queda pendiente ahora. Devuelve si pasó.</summary>
        public bool Request(string eventId, int personId) => Events.Request(World, eventId, personId);

        /// <summary>
        /// El jugador pide abrir su cuenta en ese banco desde la pantalla del banco (D6-1c): si
        /// cumple, se abre al tiro; si no, el banco dice que no y queda en el diario. Devuelve el
        /// motivo del rechazo (o <see cref="Economy.BankRules.Refusal.None"/> si se abrió).
        /// </summary>
        public Economy.BankRules.Refusal OpenAccount(string bankId)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Hay un evento pendiente: primero se elige su opción.");
            }

            Content.BanksDefinition banks = Economy.BankRules.BanksFor(Content, World.Player)
                ?? throw new InvalidOperationException("En el país donde vive no hay bancos.");
            Content.BankDefinition bank = banks.Bank(bankId) ?? throw new ArgumentException($"El banco {bankId} no existe en su país.", nameof(bankId));
            Economy.BankRules.Refusal refusal = Economy.BankRules.Check(World, banks, bank, World.Player);
            Economy.BankRules.Open(World, Content, World.Player, bankId);
            return refusal;
        }

        /// <summary>
        /// El jugador abre un correo de su bandeja (la posición en
        /// <see cref="MailAccount.Messages"/>): queda leído. Leer no cambia la vida;
        /// lo que importa del correo ya pasó o lo pregunta un evento.
        /// </summary>
        public void ReadMail(int index)
        {
            MailAccount mail = World.Player.Mail ?? throw new InvalidOperationException("El jugador no tiene correo.");
            mail.MarkRead(index);
        }

        /// <summary>
        /// El jugador hace algo con el tiempo parado (una acción de acciones/, con esa persona si la
        /// acción es con alguien): gasta tardes libres del mes y cambia lo que dice, al tiro.
        /// Devuelve lo que cambió de verdad.
        /// </summary>
        public Life.ActionOutcome DoAction(string actionId, int? personId)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Hay un evento pendiente: primero se elige su opción.");
            }

            return Life.ActionRules.Do(World, Content, World.Player, actionId, personId);
        }

        /// <summary>La tarde de estudio que va (D5a); null si no está estudiando.</summary>
        public Education.StudySession? Study { get; private set; }

        /// <summary>
        /// El jugador se pone a estudiar un ramo con el tiempo parado (D5a): solo, con alguien de
        /// su curso o con su profe jefe. Gasta una tarde libre y le toca leer el texto breve y
        /// responder tres preguntas (<see cref="AnswerStudy"/>), hasta <see cref="FinishStudy"/>.
        /// </summary>
        public Education.StudySession StartStudy(string subjectId, int? companionId, bool withTeacher)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Hay un evento pendiente: primero se elige su opción.");
            }

            if (Study != null && !Study.Finished)
            {
                FinishStudy();
            }

            Study = Education.StudyRules.Start(World, Content, World.Player, subjectId, companionId, withTeacher);
            return Study;
        }

        /// <summary>Responde la pregunta que va de la tarde de estudio (la posición entre las alternativas que ve).</summary>
        public Education.StudyAnswer AnswerStudy(int position)
        {
            Education.StudySession study = Study ?? throw new InvalidOperationException("No está estudiando.");
            return Education.StudyRules.Answer(World.Player, study, position);
        }

        /// <summary>Termina la tarde de estudio y cuenta cómo le fue y lo demás que cambió.</summary>
        public Education.StudyOutcome FinishStudy()
        {
            Education.StudySession study = Study ?? throw new InvalidOperationException("No está estudiando.");
            Education.StudyOutcome outcome = Education.StudyRules.Finish(World, World.Player, study);
            return outcome;
        }

        /// <summary>Las preguntas de la tarea de la pega que está resolviendo con lo que sabe (D5b2); null si no.</summary>
        public Work.WorkQuiz? TaskQuiz { get; private set; }

        /// <summary>
        /// Elige resolver con lo que sabe la tarea del momento pendiente (la opción
        /// <see cref="Work.WorkTaskRules.KnowOptionKey"/>): el tiempo sigue parado y le tocan tres
        /// preguntas (<see cref="AnswerTaskQuiz"/>) hasta <see cref="FinishTaskQuiz"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Si el momento pendiente no ofrece resolverla con lo que sabe.</exception>
        public Work.WorkQuiz StartTaskQuiz()
        {
            if (KnowOption() == null)
            {
                throw new InvalidOperationException("El momento pendiente no se resuelve con preguntas.");
            }

            TaskQuiz = Work.WorkTaskRules.StartQuiz(World, Content, World.Player);
            return TaskQuiz;
        }

        /// <summary>Responde la pregunta que va de la tarea (la posición entre las alternativas que ve).</summary>
        public Education.StudyAnswer AnswerTaskQuiz(int position)
        {
            Work.WorkQuiz quiz = TaskQuiz ?? throw new InvalidOperationException("No está resolviendo una tarea.");
            return Work.WorkTaskRules.Answer(quiz, position);
        }

        /// <summary>
        /// Termina las preguntas (las que no respondió cuentan como malas) y responde el momento
        /// con esa opción: la tarea sale según los aciertos. Devuelve cuánto cambió cómo lo ve su jefa.
        /// </summary>
        public int FinishTaskQuiz()
        {
            Work.WorkQuiz quiz = TaskQuiz ?? throw new InvalidOperationException("No está resolviendo una tarea.");
            int option = KnowOption() ?? throw new InvalidOperationException("El momento pendiente ya no se resuelve con preguntas.");
            Work.WorkTaskRules.FinishQuiz(World.Player, quiz, World.Date.TotalWeeks);
            TaskQuiz = null;
            int before = World.Player.Job?.Performance ?? 0;
            Choose(option);
            return (World.Player.Job?.Performance ?? 0) - before;
        }

        /// <summary>La opción del momento pendiente que resuelve la tarea con preguntas; null si no hay.</summary>
        private int? KnowOption()
        {
            PendingEvent? pending = Pending;
            if (pending == null)
            {
                return null;
            }

            EventDefinition definition = Content.GetEvent(pending.EventId);
            foreach (int index in pending.AvailableOptions)
            {
                if (definition.Options[index].TextKey == Work.WorkTaskRules.KnowOptionKey)
                {
                    return index;
                }
            }

            return null;
        }

        /// <summary>El jugador abre su conversación de Mensajito con esa persona: ve todos sus mensajes.</summary>
        public void ReadChat(int otherId)
        {
            ChatInbox chats = World.Player.Chats ?? throw new InvalidOperationException("El jugador no tiene Mensajito.");
            chats.MarkRead(otherId, World.Date);
        }

        /// <summary>
        /// El jugador contesta en Mensajito el mensaje que esa persona espera que le
        /// conteste, con una de sus respuestas (ver <see cref="Presentation.ChatView.Replies"/>).
        /// Lo que contesta cambia lo que la otra persona siente por él.
        /// </summary>
        public void ReplyChat(int otherId, int replyIndex)
        {
            if (Pending != null)
            {
                // Lo que contesta puede cambiar lo que siente esa persona, y con eso las opciones
                // del evento que ya está en pantalla: primero se decide el evento.
                throw new InvalidOperationException("Hay un evento pendiente: primero se elige su opción.");
            }

            ChatSystem.Reply(World, Content, World.Player, otherId, replyIndex);
        }

        /// <summary>
        /// El jugador le escribe primero a esa persona por Mensajito (un id de packs/*/escribir/
        /// que se le pueda escribir ahora). Contesta al tiro, según lo que sienta, o lo deja en visto.
        /// </summary>
        public void WriteChat(int otherId, string openerId)
        {
            if (Pending != null)
            {
                throw new InvalidOperationException("Hay un evento pendiente: primero se elige su opción.");
            }

            ChatSystem.Write(World, Content, World.Player, otherId, openerId);
        }

        /// <summary>
        /// Si el tiempo se detiene para que el jugador arme su horario al empezar cada
        /// etapa. Lo encienden las pantallas (consola, Unity); apagado, la vida sigue
        /// con el horario sugerido (simulador y pruebas).
        /// </summary>
        public bool AsksForRoutine
        {
            get => _routine.StopsForNewRoutine;
            set => _routine.StopsForNewRoutine = value;
        }

        /// <summary>
        /// El jugador pone una actividad (por su id) en un bloque de su horario, o lo deja
        /// libre con null. Solo vale lo que <see cref="RoutineRules.Options"/> permite hoy.
        /// </summary>
        public void SetRoutine(int day, RoutineBlock block, string? activityId)
        {
            Routine routine = World.Player.Routine ?? throw new InvalidOperationException("Todavía no hay horario (empieza a los 6 años).");
            if (activityId != null && !RoutineRules.Options(World, Content, World.Player, block).Any(a => a.Id == activityId))
            {
                throw new InvalidOperationException($"'{activityId}' no se puede poner ahí ahora.");
            }

            routine.Set(day, block, activityId);
        }

        /// <summary>El jugador deja su horario como está; la vida sigue con él hasta que lo cambie.</summary>
        public void ConfirmRoutine()
        {
            Routine routine = World.Player.Routine ?? throw new InvalidOperationException("Todavía no hay horario (empieza a los 6 años).");
            routine.Confirmed = true;
        }

        /// <summary>Entradas nuevas del diario del jugador desde la última vez que se pidieron.</summary>
        public IReadOnlyList<JournalEntry> TakeNewJournalEntries()
        {
            var entries = World.Journal.Skip(_journalRead).Where(e => e.CharacterId == World.PlayerId).ToList();
            _journalRead = World.Journal.Count;
            return entries;
        }

        /// <summary>
        /// Texto para el jugador en el idioma dado, con la forma de decirlo del país
        /// donde vive (por ejemplo "es" en Chile usa "es-CL").
        /// </summary>
        public string Text(string key, string language = ContentDatabase.BaseLanguage) =>
            Content.Text(key, Content.LanguageFor(World.Player.Residence?.CountryId, language));
    }
}
