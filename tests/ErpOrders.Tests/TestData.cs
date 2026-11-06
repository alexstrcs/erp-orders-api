namespace ErpOrders.Tests;

/// <summary>Generates unique, valid test data so tests never collide on unique indexes.</summary>
internal static class TestData
{
    private static int _cpfSeed = Random.Shared.Next(100_000_000, 899_999_999);

    /// <summary>Returns a new valid CPF (11 digits) computed with the official check-digit rules.</summary>
    public static string NewCpf()
    {
        var baseNumber = Interlocked.Increment(ref _cpfSeed).ToString("D9");
        var digits = baseNumber.Select(c => c - '0').ToList();

        digits.Add(CpfCheckDigit(digits, 10));
        digits.Add(CpfCheckDigit(digits, 11));
        return string.Concat(digits);
    }

    public static string NewSku() => "SKU-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private static int CpfCheckDigit(IReadOnlyList<int> digits, int firstWeight)
    {
        var sum = 0;
        for (var i = 0; i < digits.Count; i++)
            sum += digits[i] * (firstWeight - i);
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
