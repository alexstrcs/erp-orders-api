using ErpOrders.Api.Domain;

namespace ErpOrders.Tests.Domain;

public class NfeAccessKeyTests
{
    private const string IssuerCnpj = "11222333000181";

    [Fact]
    public void Builds_a_44_digit_key_with_every_field_in_place()
    {
        var key = NfeAccessKey.Create(41, new DateTime(2026, 11, 6), IssuerCnpj, series: 1, number: 123, randomCode: 12345678);

        Assert.Equal(44, key.Length);
        Assert.Equal("41", key[..2]);                // state (Paraná)
        Assert.Equal("2611", key[2..6]);             // year + month
        Assert.Equal(IssuerCnpj, key[6..20]);        // issuer CNPJ
        Assert.Equal("55", key[20..22]);             // NF-e model
        Assert.Equal("001", key[22..25]);            // series
        Assert.Equal("000000123", key[25..34]);      // invoice number
        Assert.Equal("1", key[34..35]);              // normal emission
        Assert.Equal("12345678", key[35..43]);       // random code
        Assert.True(NfeAccessKey.IsValid(key));
    }

    [Fact]
    public void Check_digit_uses_mod11_with_weights_2_to_9_from_the_right()
    {
        // "1234": 4*2 + 3*3 + 2*4 + 1*5 = 30; 30 % 11 = 8; DV = 11 - 8 = 3
        Assert.Equal(3, NfeAccessKey.CheckDigit("1234"));
        // "0000": remainder 0 -> DV 0
        Assert.Equal(0, NfeAccessKey.CheckDigit("0000"));
    }

    [Fact]
    public void Detects_a_tampered_key()
    {
        var key = NfeAccessKey.Create(41, new DateTime(2026, 1, 15), IssuerCnpj, 1, 1, 42);
        var lastDigit = key[^1] - '0';
        var tampered = key[..43] + ((lastDigit + 1) % 10);

        Assert.False(NfeAccessKey.IsValid(tampered));
    }

    [Theory]
    [InlineData(41, "11222333000180", 1, 1)]   // invalid CNPJ
    [InlineData(99, IssuerCnpj, 1, 1)]          // invalid state code
    [InlineData(41, IssuerCnpj, 1000, 1)]       // series out of range
    [InlineData(41, IssuerCnpj, 1, 0)]          // number out of range
    public void Rejects_invalid_input(int state, string cnpj, int series, int number) =>
        Assert.Throws<DomainException>(() =>
            NfeAccessKey.Create(state, DateTime.UtcNow, cnpj, series, number, 1));
}
