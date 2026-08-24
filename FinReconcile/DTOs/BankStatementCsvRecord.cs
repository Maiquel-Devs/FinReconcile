using CsvHelper.Configuration.Attributes;

namespace FinReconcile.DTOs;

/// <summary>
/// Representa a estrutura de transferência de dados (DTO) de uma linha do arquivo CSV 
/// importado durante a leitura do extrato bancário.
/// </summary>
public class BankStatementCsvRecord
{
    /// <summary>
    /// Código ou identificador único da transação gerado pela instituição bancária.
    /// Mapeia a coluna 'reference' do CSV.
    /// </summary>
    [Name("reference")]
    public string Reference { get; init; } = string.Empty;

    /// <summary>
    /// Valor bruto da transação financeira.
    /// Mapeia a coluna 'amount' do CSV.
    /// </summary>
    [Name("amount")]
    public decimal Amount { get; init; }

    /// <summary>
    /// Tarifa ou taxa cobrada pela instituição bancária sobre a operação.
    /// Mapeia a coluna 'fee' do CSV.
    /// </summary>
    [Name("fee")]
    public decimal Fee { get; init; }

    /// <summary>
    /// Data e hora em que a transação ocorreu ou foi liquidada no banco.
    /// Mapeia a coluna 'date' do CSV.
    /// </summary>
    [Name("date")]
    public DateTime Date { get; init; }
}