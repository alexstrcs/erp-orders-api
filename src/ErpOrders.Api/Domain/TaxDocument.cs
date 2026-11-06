namespace ErpOrders.Api.Domain;

/// <summary>
/// Validation for Brazilian taxpayer documents: CPF (individuals, 11 digits)
/// and CNPJ (companies, 14 digits), using the official mod-11 check digits.
/// </summary>
public static class TaxDocument
{
    private static readonly int[] CnpjWeights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CnpjWeights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Removes punctuation such as ".", "-" and "/".</summary>
    public static string Normalize(string? value) =>
        new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);
        return digits.Length switch
        {
            11 => IsValidCpf(digits),
            14 => IsValidCnpj(digits),
            _ => false,
        };
    }

    public static bool IsValidCpf(string digits)
    {
        if (digits.Length != 11 || AllSameDigit(digits)) return false;

        var d1 = CheckDigit(digits, 9, i => 10 - i);
        var d2 = CheckDigit(digits, 10, i => 11 - i);
        return digits[9] - '0' == d1 && digits[10] - '0' == d2;
    }

    public static bool IsValidCnpj(string digits)
    {
        if (digits.Length != 14 || AllSameDigit(digits)) return false;

        var d1 = CheckDigit(digits, 12, i => CnpjWeights1[i]);
        var d2 = CheckDigit(digits, 13, i => CnpjWeights2[i]);
        return digits[12] - '0' == d1 && digits[13] - '0' == d2;
    }

    private static int CheckDigit(string digits, int length, Func<int, int> weight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (digits[i] - '0') * weight(i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool AllSameDigit(string digits) => digits.All(c => c == digits[0]);
}
