namespace KsefGuard;

public sealed record ScenarioDescriptor(string Id, string DescriptionEn, string DescriptionPl);

public static class ScenarioCatalog
{
    public static readonly IReadOnlyList<ScenarioDescriptor> All =
    [
        new("certificate-metadata", "Checks synthetic/public X.509 metadata against explicit fixture expectations.", "Sprawdza metadane syntetycznego/publicznego certyfikatu X.509 względem jawnych oczekiwań fixture."),
        new("qr-verification-link", "Checks a synthetic hash and declared KSeF-style verification link.", "Sprawdza syntetyczny skrót i zadeklarowany link weryfikacyjny w stylu KSeF."),
        new("offline24-timeline", "Checks ordering and pack-declared timing for a synthetic offline24 event sequence.", "Sprawdza kolejność i czas zadeklarowany w pakiecie dla syntetycznej sekwencji offline24."),
        new("retry-recovery-sequence", "Checks retry count, allowed intermediate outcomes, and terminal recovery outcome.", "Sprawdza liczbę prób, dozwolone wyniki pośrednie oraz końcowy wynik odzyskiwania."),
        new("unresolved-status", "Checks duplicate references, contradictory terminal states, and unresolved records.", "Sprawdza duplikaty referencji, sprzeczne stany końcowe i nierozstrzygnięte rekordy.")
    ];

    public static bool IsSupported(string type) => All.Any(item => string.Equals(item.Id, type, StringComparison.Ordinal));
}
