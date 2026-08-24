using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinReconcile.Models;

/// <summary>
/// Representa uma transação financeira importada de um extrato bancário (CSV).
/// Utilizada como base de comparação para a conciliação com o Livro-Razão interno.
/// </summary>
public class BankStatement
{
    /// <summary>
    /// Identificador único do registro no banco de dados.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificador do lote de importação. Permite agrupar e rastrear todas as 
    /// transações que foram importadas a partir de um mesmo arquivo CSV.
    /// </summary>
    public Guid BatchId { get; set; }

    /// <summary>
    /// Código de identificação ou NSU (Número Sequencial Único) originado pelo banco.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OriginalReference { get; set; } = string.Empty;

    /// <summary>
    /// Valor bruto da transação financeira.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Valor da tarifa, taxa ou imposto retido pela instituição bancária sobre a operação.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal FeeAmount { get; set; }

    /// <summary>
    /// Valor líquido creditado ou debitado na conta bancária (Amount - FeeAmount).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Data e hora em que a transação foi efetivada e liquidada no banco.
    /// </summary>
    public DateTime StatementDate { get; set; }

    /// <summary>
    /// Status atual da transação no fluxo de conciliação (ex: Pendente, Conciliado, Divergente).
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Data e hora em que o registro foi gerado no sistema interno.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}