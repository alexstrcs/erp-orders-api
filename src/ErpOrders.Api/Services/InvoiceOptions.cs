namespace ErpOrders.Api.Services;

/// <summary>Issuer data used to number invoices and build NF-e access keys ("Invoice" config section).</summary>
public class InvoiceOptions
{
    public const string SectionName = "Invoice";

    /// <summary>Issuer company CNPJ (digits only).</summary>
    public string IssuerCnpj { get; set; } = string.Empty;

    /// <summary>IBGE code of the issuer's state, e.g. 41 = Paraná.</summary>
    public int IssuerStateCode { get; set; } = 41;

    public int Series { get; set; } = 1;

    /// <summary>Flat tax rate applied to the invoice total (simplification of ICMS).</summary>
    public decimal TaxRate { get; set; } = 0.18m;
}
