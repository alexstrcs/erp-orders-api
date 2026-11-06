using ErpOrders.Api.Domain;

namespace ErpOrders.Tests.Domain;

public class TaxDocumentTests
{
    [Theory]
    [InlineData("529.982.247-25")] // CPF
    [InlineData("52998224725")]
    [InlineData("11.222.333/0001-81")] // CNPJ
    [InlineData("11222333000181")]
    public void Accepts_valid_documents_with_or_without_punctuation(string document) =>
        Assert.True(TaxDocument.IsValid(document));

    [Theory]
    [InlineData("529.982.247-26")] // wrong CPF check digit
    [InlineData("11.222.333/0001-80")] // wrong CNPJ check digit
    [InlineData("111.111.111-11")] // repeated digits pass the math but are invalid
    [InlineData("00000000000000")]
    [InlineData("1234")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_invalid_documents(string? document) =>
        Assert.False(TaxDocument.IsValid(document));

    [Fact]
    public void Normalize_keeps_only_digits() =>
        Assert.Equal("11222333000181", TaxDocument.Normalize("11.222.333/0001-81"));

    [Fact]
    public void Generated_test_cpfs_are_valid()
    {
        for (var i = 0; i < 50; i++)
            Assert.True(TaxDocument.IsValidCpf(TestData.NewCpf()));
    }
}
