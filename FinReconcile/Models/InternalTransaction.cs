using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinReconcile.Models;

/// <summary>
/// Representa um lançamento financeiro originado no sistema interno (Livro-Razão).
/// Esta entidade contém a expectativa de recebimento/pagamento que será 
/// validada contra o extrato bancário.
/// </summary>
public class InternalTransaction
{
    /// <summary>
    /// Identificador único do registro no banco de dados.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código de referência gerado pela operação (ex: ID da transação de Cartão, 
    /// chave do PIX ou nosso número do Boleto). Utilizado como chave primária de busca na conciliação.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ExternalReference { get; set; } = string.Empty;

    /// <summary>
    /// Valor bruto esperado para a transação financeira.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Data e hora em que a transação foi originalmente registrada no sistema interno.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Status atual do lançamento (ex: Pendente, Conciliado, Divergente).
    /// </summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>
    /// Data e hora de criação do registro no banco de dados.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}