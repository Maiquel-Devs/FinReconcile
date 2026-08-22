using CsvHelper.Configuration.Attributes;

namespace FinReconcile.DTOs;

public class BankStatementCsvRecord
{
    [Name("reference")]
    public string Reference { get; set; } = string.Empty;

    [Name("amount")]
    public decimal Amount { get; set; }

    [Name("fee")]
    public decimal Fee { get; set; }

    [Name("date")]
    public DateTime Date { get; set; }
}