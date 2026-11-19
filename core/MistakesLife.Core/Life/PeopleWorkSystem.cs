using System;
using System.Collections.Generic;
using System.Linq;
using MistakesLife.Core.Characters;
using MistakesLife.Core.Content;
using MistakesLife.Core.Economy;
using MistakesLife.Core.Events;
using MistakesLife.Core.Randomness;
using MistakesLife.Core.Simulation;
using MistakesLife.Core.Time;
using MistakesLife.Core.Worlds;

namespace MistakesLife.Core.Life
{
    /// <summary>
    /// Fase de personajes: en qué anda la gente que nadie juega (tu familia, tus amistades,
    /// tus conocidos), con los datos reales de su país ("trabajo_de_la_gente" en country.json).
    /// La primera vez que el juego la mira y después en cada cumpleaños, según su edad: trabaja,
    /// busca pega, estudia, está jubilada o no anda en nada de eso.
    ///
    /// Quien empieza a trabajar puede tener una de las pegas que existen enteras en el juego
    /// (una carrera u oficio con sus áreas, cargos y empresas), solo en la parte real de los
    /// ocupados que trabaja en ella; los demás "trabajan" sin que el juego les invente una
    /// pega. Quien ya tuvo una pega del juego vuelve a ella: su oficio no cambia de un año al otro.
    ///
    /// No toca al jugador (su vida la deciden sus decisiones), ni a su jefatura y sus colegas
    /// (su pega es la tuya), ni a quien está estudiando una carrera de verdad en el juego.
    /// </summary>
    public sealed class PeopleWorkSystem : IWeeklySystem
    {
        public const string SystemId = "sistema.trabajo_de_la_gente";

        private readonly ContentDatabase _content;

        public PeopleWorkSystem(ContentDatabase content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Id => SystemId;

        public WeekPhase Phase => WeekPhase.Characters;

        public void Tick(World world, WeekContext context)
        {
            Character player = world.Player;
            HashSet<int> team = TeamOf(world, player);
            GameDate nextWeek = context.Date.AddWeeks(1);
            foreach (Character person in world.Characters.Where(c => c.IsAlive && c.Id != player.Id && c.Residence != null).OrderBy(c => c.Id).ToList())
            {
                if (team.Contains(person.Id) || person.Studies != null)
                {
                    continue;
                }

                PeopleWorkDefinition? rules = _content.GetCountry(person.Residence!.CountryId).PeopleWork;
                int age = person.AgeInYears(context.Date);
                if (rules == null || age < rules.MinAge)
                {
                    continue;
                }

                // Cumple años al terminar esta semana: se vuelve a mirar en qué anda.
                bool birthday = person.AgeInWeeks(nextWeek) % GameDate.WeeksPerYear == 0;
                if (person.WorkStatus != null && !birthday)
                {
                    continue;
                }

                Look(world, _content, rules, person, age, context.RandomFor(SystemId + "." + person.Id));
            }
        }

        /// <summary>
        /// Mira en qué anda esa persona a esa edad. Con los datos del tramo de su edad: trabaja
        /// (la tasa de ocupación), busca pega (los desocupados), estudia (de los que no trabajan
        /// ni buscan, los que hacen falta para la tasa de asistencia a la educación superior), está
        /// jubilada (desde la edad legal de su país) o nada de eso.
        /// </summary>
        public static void Look(World world, ContentDatabase content, PeopleWorkDefinition rules, Character person, int age, GameRandom random)
        {
            PeopleWorkAgeDefinition? group = rules.AgeGroup(age);
            double employed = group?.Employed ?? 0;
            double unemployed = group?.Unemployed ?? 0;
            double roll = random.NextDouble();
            if (roll < employed)
            {
                bool wasWorking = person.WorkStatus == WorkStatus.Working;
                person.WorkStatus = WorkStatus.Working;
                // Quien ya trabajaba en otra pega sigue en ella; quien perdió la suya del juego
                // (por ejemplo, porque quebró su empresa) vuelve a su oficio.
                if (person.Job == null && (!wasWorking || person.LastPosition != null))
                {
                    StartJob(world, content, rules, person, random);
                }

                return;
            }

            person.Job = null;
            if (roll < employed + unemployed)
            {
                person.WorkStatus = WorkStatus.Unemployed;
                return;
            }

            PeopleStudyDefinition? study = rules.HigherEducation;
            double inactive = 1 - employed - unemployed;
            if (study != null && age >= study.From && age <= study.To && inactive > 0 && random.Chance(Math.Min(1, study.Share / inactive)))
            {
                person.WorkStatus = WorkStatus.Studying;
                return;
            }

            person.WorkStatus = age >= RetirementAge(content, person) ? WorkStatus.Retired : WorkStatus.Inactive;
        }

        /// <summary>
        /// Empieza a trabajar: si ya tuvo una pega del juego, vuelve a esa; si no, le toca una de
        /// las pegas del país en la parte real de los ocupados (por sexo, si la pega lo dice) o,
        /// si no le toca ninguna, trabaja en otra que el juego todavía no tiene.
        /// </summary>
        private static void StartJob(World world, ContentDatabase content, PeopleWorkDefinition rules, Character person, GameRandom random)
        {
            string? previous = person.LastPosition == null ? null : content.GetCareerArea(person.LastPosition.AreaId).CareerId;
            if (previous != null)
            {
                Hire(world, content, person, content.GetCareer(previous), random);
                return;
            }

            double roll = random.NextDouble();
            double sum = 0;
            foreach (PeopleJobDefinition job in rules.Jobs)
            {
                sum += job.Share * SexFactor(rules, job, person);
                if (roll < sum)
                {
                    Hire(world, content, person, content.GetCareer(job.CareerId), random);
                    return;
                }
            }
        }

        /// <summary>
        /// Cuánto más (o menos) seguido tiene esa pega alguien de su sexo: la parte de mujeres
        /// de la pega frente a la parte de mujeres de todos los ocupados. 1 si la pega no lo dice.
        /// </summary>
        public static double SexFactor(PeopleWorkDefinition rules, PeopleJobDefinition job, Character person)
        {
            if (!job.Women.HasValue)
            {
                return 1;
            }

            return person.Sex == Sex.Female
                ? job.Women.Value / rules.WomenShare
                : (1 - job.Women.Value) / (1 - rules.WomenShare);
        }

        /// <summary>
        /// Entra a trabajar en esa carrera u oficio como cualquiera: con su título (si la carrera
        /// lo pide y no lo tiene, uno de sus niveles), en un área que puede, en una empresa de su
        /// carrera y en el cargo que le da su experiencia, con el sueldo de ese cargo.
        /// </summary>
        private static void Hire(World world, ContentDatabase content, Character person, CareerDefinition career, GameRandom random)
        {
            if (!career.IsOccupation && career.Levels.Count > 0 && !person.Degrees.Any(d => content.GetCareerLevel(d).CareerId == career.Id))
            {
                person.AddDegree(random.Pick(career.Levels).Id);
            }

            var scope = new EventScope(world, content, person, null, readOnly: false);
            List<CareerAreaDefinition> areas = career.Areas.Where(a => CareerLadder.CanEnter(content, person, a, scope)).ToList();
            if (areas.Count == 0)
            {
                return;
            }

            CareerAreaDefinition area = person.LastPosition != null && areas.Any(a => a.Id == person.LastPosition.AreaId)
                ? areas.First(a => a.Id == person.LastPosition.AreaId)
                : random.Pick(areas);
            Company? company = CompanyRules.Hire(world, content, person, career);
            CompanySizeDefinition? size = company == null ? null : content.GetCountry(company.CountryId).Economy?.Companies?.Size(company.SizeId);
            CareerPositionDefinition position = CompanyRules.Cap(CareerLadder.StartingPosition(content, person, area), size?.MaxPositionDepth);
            decimal salary = CareerLadder.Salary(scope.SalaryBasisOf(person, career), area, position);
            if (size != null)
            {
                double relative = career.IsOccupation ? Math.Max(1, size.SalaryRelative) : size.SalaryRelative;
                salary = Math.Round(salary * (decimal)relative, MidpointRounding.AwayFromZero);
            }

            double priceIndex = world.EconomyOf(person.Residence!.CountryId)?.PriceIndex ?? 1;
            Job job = CareerLadder.Place(person, JobKind.Employee, salary, priceIndex, area, position);
            job.CompanyId = company?.Id;
        }

        /// <summary>La edad legal para jubilarse en su país, según su sexo (65 si el país no la define).</summary>
        private static int RetirementAge(ContentDatabase content, Character person)
        {
            CountryDefinition country = content.GetCountry(person.Residence!.CountryId);
            string law = person.Sex == Sex.Female ? "ley.edad_jubilacion_mujeres" : "ley.edad_jubilacion_hombres";
            return country.TryGetLaw(law, out Scripting.Value value) ? (int)value.AsNumber(law) : 65;
        }

        /// <summary>Tu jefatura y tus colegas de ahora: su pega es la tuya, no se les mira aparte.</summary>
        private static HashSet<int> TeamOf(World world, Character player)
        {
            var team = new HashSet<int>();
            if (Work.WorkRules.InTeam(player))
            {
                Character? boss = Work.WorkRules.Boss(world, player);
                if (boss != null)
                {
                    team.Add(boss.Id);
                }

                foreach (Character mate in Work.WorkRules.Colleagues(world, player))
                {
                    team.Add(mate.Id);
                }
            }

            return team;
        }
    }
}
