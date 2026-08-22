using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinReconcile.Models;

public class BankStatement
{
    public int Id { get; set; }

    public Guid BatchId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OriginalReference { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FeeAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    public DateTime StatementDate { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}