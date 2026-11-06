namespace ErpOrders.Api.Domain;

public class Customer
{
    private static readonly HashSet<string> BrazilianStates =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    ];

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>CPF or CNPJ, digits only.</summary>
    public string Document { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    /// <summary>Two-letter Brazilian state (UF).</summary>
    public string State { get; private set; } = string.Empty;

    private Customer() { } // EF Core

    public Customer(string name, string document, string email, string state)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");

        var normalizedDocument = TaxDocument.Normalize(document);
        if (!TaxDocument.IsValid(normalizedDocument))
            throw new DomainException("Document must be a valid CPF or CNPJ.");

        var normalizedState = (state ?? string.Empty).Trim().ToUpperInvariant();
        if (!BrazilianStates.Contains(normalizedState))
            throw new DomainException($"'{state}' is not a valid Brazilian state (UF).");

        Name = name.Trim();
        Document = normalizedDocument;
        Email = email.Trim().ToLowerInvariant();
        State = normalizedState;
    }
}
