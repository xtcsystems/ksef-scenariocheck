using XtcSystems.KsefScenarioCheck.Core.Contracts;
using XtcSystems.KsefScenarioCheck.Core.Evaluation;
using XtcSystems.KsefScenarioCheck.Core.Reporting;

namespace XtcSystems.KsefScenarioCheck.Core;

public sealed class ScenarioCheckEngine
{
    private readonly ScenarioEvaluator _evaluator = new();

    public ValidatedObservation ValidateObservation(byte[] bytes)
        => ContractValidator.ValidateObservation(bytes);

    public ValidatedPack ValidatePack(byte[] bytes)
        => ContractValidator.ValidatePack(bytes);

    public ScenarioCheckReport Evaluate(
        ValidatedObservation observation,
        ValidatedPack pack,
        string evaluatedAt,
        string engineVersion)
    {
        DateTimeOffset instant = ContractValidator.ParseUtc(evaluatedAt, "KSC.INPUT.INVALID");
        return _evaluator.Evaluate(observation, pack, evaluatedAt, instant, engineVersion);
    }
}
