namespace XtcSystems.KsefScenarioCheck.Core.Contracts;

public sealed class ContractException : Exception
{
    public ContractException(string code, string safeMessage)
        : base(safeMessage)
    {
        Code = code;
        SafeMessage = safeMessage;
    }

    public string Code { get; }

    public string SafeMessage { get; }
}
