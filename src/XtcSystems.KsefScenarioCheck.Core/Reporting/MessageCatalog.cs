namespace XtcSystems.KsefScenarioCheck.Core.Reporting;

public static class MessageCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["KSC.EVENT.PRESENT"] = "The modeled synthetic event is present.",
        ["KSC.EVENT.MISSING"] = "The modeled synthetic event is missing.",
        ["KSC.OFFLINE_MARKER.PRESENT"] = "The synthetic observation contains the pack-required offline-mode marker.",
        ["KSC.OFFLINE_MARKER.MISSING"] = "The synthetic observation does not contain the pack-required offline-mode marker.",
        ["KSC.OFFLINE_MARKER.TRUE"] = "The synthetic offline-mode marker matches the modeled value.",
        ["KSC.OFFLINE_MARKER.FALSE"] = "The synthetic offline-mode marker differs from the modeled value.",
        ["KSC.ISSUE_DATE.MATCHED"] = "The observation's synthetic issue-date field matches the modeled scenario input.",
        ["KSC.ISSUE_DATE.DIFFERED"] = "The observation's synthetic issue-date field differs from the modeled scenario input.",
        ["KSC.SYNTHETIC_WINDOW.INSIDE"] = "The timestamp is inside the pack-declared synthetic window.",
        ["KSC.SYNTHETIC_WINDOW.OUTSIDE"] = "The timestamp is outside the pack-declared synthetic window.",
        ["KSC.SEQUENCE.ORDERED"] = "The synthetic events are in the modeled order.",
        ["KSC.SEQUENCE.UNORDERED"] = "The synthetic events are not in the modeled order.",
        ["KSC.PASS.MATCHED"] = "The supplied observation matches the modeled scenario.",
        ["KSC.FAIL.DIFFERED"] = "The supplied observation differs from the modeled scenario.",
        ["KSC.PROFILE.UNSUPPORTED"] = "The supplied profile is unsupported.",
        ["KSC.PACK.STALE"] = "The selected pack is stale for the supplied evaluation time.",
        ["KSC.PACK.UNVERIFIED"] = "The selected local pack is unverified.",
        ["KSC.RULE.NEEDS_REVIEW"] = "The modeled assertion requires review and was not evaluated.",
        ["KSC.INPUT.INVALID"] = "The supplied input is invalid."
    };

    public static string Get(string code)
        => Messages.TryGetValue(code, out string? value)
            ? value
            : "The modeled assertion produced a bounded result.";
}
