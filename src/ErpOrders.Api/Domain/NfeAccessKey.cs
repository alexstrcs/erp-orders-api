namespace ErpOrders.Api.Domain;

/// <summary>
/// Builds the 44-digit NF-e access key ("chave de acesso"):
/// cUF(2) + AAMM(4) + CNPJ(14) + model(2) + series(3) + number(9) + emission type(1) + cNF(8) + DV(1).
/// </summary>
public static class NfeAccessKey
{
    public const string NfeModel = "55";
    public const int NormalEmission = 1;

    public static string Create(int stateCode, DateTime issuedAt, string issuerCnpj,
        int series, int number, int randomCode)
    {
        var cnpj = TaxDocument.Normalize(issuerCnpj);
        if (!TaxDocument.IsValidCnpj(cnpj))
            throw new DomainException("Issuer CNPJ is invalid.");
        if (stateCode is < 11 or > 53)
            throw new DomainException("Invalid IBGE state code.");
        if (series is < 0 or > 999)
            throw new DomainException("Series must be between 0 and 999.");
        if (number is < 1 or > 999_999_999)
            throw new DomainException("Invoice number must be between 1 and 999999999.");
        if (randomCode is < 0 or > 99_999_999)
            throw new DomainException("Random code must have at most 8 digits.");

        var body = string.Concat(
            stateCode.ToString("D2"),
            issuedAt.ToString("yyMM"),
            cnpj,
            NfeModel,
            series.ToString("D3"),
            number.ToString("D9"),
            NormalEmission.ToString(),
            randomCode.ToString("D8"));

        return body + CheckDigit(body);
    }

    /// <summary>Mod-11 check digit with weights 2..9 applied from right to left.</summary>
    public static int CheckDigit(string body)
    {
        var sum = 0;
        var weight = 2;
        for (var i = body.Length - 1; i >= 0; i--)
        {
            sum += (body[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    public static bool IsValid(string? key) =>
        key is { Length: 44 }
        && key.All(char.IsAsciiDigit)
        && key[^1] - '0' == CheckDigit(key[..43]);
}
