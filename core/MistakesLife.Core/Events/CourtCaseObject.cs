using MistakesLife.Core.Content;
using MistakesLife.Core.Courts;
using MistakesLife.Core.Scripting;

namespace MistakesLife.Core.Events
{
    /// <summary>
    /// Resultado de causa('causa.x'): la causa de ese tipo del personaje (la abierta,
    /// o la última que terminó).
    /// Datos: existe, abierta, etapa (el id de la etapa, o ''), semanas_en_etapa,
    /// lista (se cumplió el plazo de la etapa), resultado (o nada), monto (en dinero de hoy, se puede
    /// cambiar; se reajusta solo con los precios), rit, contraparte (la otra persona, o nada).
    /// Acciones: avanzar(), ir_a('etapa'), cerrar('resultado').
    /// </summary>
    public sealed class CourtCaseObject : IScriptObject
    {
        private readonly EventScope _scope;
        private readonly CourtCaseDefinition _definition;

        public CourtCaseObject(EventScope scope, CourtCaseDefinition definition)
        {
            _scope = scope;
            _definition = definition;
        }

        private CourtCase? Target => _scope.World.CourtCaseOf(_scope.Subject.Id, _definition.Id);

        public Value Get(string member, IScriptScope scope)
        {
            CourtCase? courtCase = Target;
            switch (member)
            {
                case "existe":
                    return Value.FromBool(courtCase != null);
                case "abierta":
                    return Value.FromBool(courtCase != null && courtCase.IsOpen);
                case "etapa":
                    return Value.FromText(courtCase != null && courtCase.IsOpen ? _definition.Stages[courtCase.StageIndex].Id : string.Empty);
                case "semanas_en_etapa":
                    return Value.FromNumber(courtCase != null && courtCase.IsOpen ? _scope.World.Date.WeeksSince(courtCase.StageStarted) : 0);
                case "lista":
                    return Value.FromBool(courtCase != null && IsReady(courtCase));
                case "resultado":
                    return courtCase?.Outcome == null ? Value.Null : Value.FromText(courtCase.Outcome);
                case "monto":
                    // Se guarda en dinero del inicio del mundo: así se reajusta solo con los precios (como una pensión de alimentos).
                    return Value.FromNumber((double)Economy.PayrollCalculator.ToToday(courtCase?.Amount ?? 0, PriceIndex()));
                case "rit":
                    return courtCase == null ? Value.Null : Value.FromText(courtCase.Rit);
                case "contraparte":
                    if (courtCase?.OtherPartyId != null && _scope.World.TryGetCharacter(courtCase.OtherPartyId.Value, out Characters.Character? other))
                    {
                        return Value.FromObject(new CharacterObject(other, _scope.Subject));
                    }

                    return Value.Null;
                default:
                    throw new ScriptException($"causa('{_definition.Id}').{member} no existe.");
            }
        }

        public void Set(string member, Value value, IScriptScope scope)
        {
            _scope.RequireWritable($"causa('{_definition.Id}').{member}");
            if (member != "monto")
            {
                throw new ScriptException($"causa('{_definition.Id}').{member} no se puede cambiar.");
            }

            CourtCase courtCase = OpenCase("monto");
            double today = System.Math.Max(0, System.Math.Round(value.AsNumber("monto")));
            courtCase.Amount = (decimal)(today / PriceIndex());
        }

        public Value Call(string method, Value[] args, IScriptScope scope)
        {
            _scope.RequireWritable($"causa('{_definition.Id}').{method}");
            switch (method)
            {
                case "avanzar":
                    NoArgs(method, args);
                    CourtSystem.Advance(_scope.World, _definition, OpenCase(method));
                    return Value.Null;
                case "ir_a":
                    string stageId = EventScope.SingleText($"causa('{_definition.Id}').ir_a", args);
                    int index = _definition.StageIndex(stageId);
                    if (index < 0)
                    {
                        throw new ScriptException($"causa('{_definition.Id}').ir_a('{stageId}'): esa etapa no existe.");
                    }

                    OpenCase(method).MoveTo(index, _scope.World.Date);
                    return Value.Null;
                case "cerrar":
                    string outcome = EventScope.SingleText($"causa('{_definition.Id}').cerrar", args);
                    if (!_definition.Outcomes.Contains(outcome))
                    {
                        throw new ScriptException($"causa('{_definition.Id}').cerrar('{outcome}'): esa causa no tiene ese resultado.");
                    }

                    OpenCase(method).Close(outcome, _scope.World.Date);
                    return Value.Null;
                default:
                    throw new ScriptException($"causa('{_definition.Id}').{method}() no existe.");
            }
        }

        /// <summary>Si la causa está abierta y ya se cumplió el plazo de su etapa actual.</summary>
        internal bool IsReady(CourtCase courtCase) =>
            courtCase.IsOpen && _scope.World.Date.WeeksSince(courtCase.StageStarted) >= _definition.Stages[courtCase.StageIndex].Weeks;

        /// <summary>El índice de precios del país donde vive el dueño de la causa (1 al inicio del mundo).</summary>
        private double PriceIndex()
        {
            string? country = _scope.Subject.Residence?.CountryId;
            double index = country == null ? 1 : _scope.World.EconomyOf(country)?.PriceIndex ?? 1;
            return index > 0 ? index : 1;
        }

        private CourtCase OpenCase(string what)
        {
            CourtCase? courtCase = Target;
            if (courtCase == null || !courtCase.IsOpen)
            {
                throw new ScriptException($"causa('{_definition.Id}').{what}: no hay una causa abierta; revisa causa('{_definition.Id}').abierta antes.");
            }

            return courtCase;
        }

        private void NoArgs(string method, Value[] args)
        {
            if (args.Length != 0)
            {
                throw new ScriptException($"causa('{_definition.Id}').{method}() no recibe valores.");
            }
        }

        public override string ToString() => $"causa('{_definition.Id}')";
    }
}
