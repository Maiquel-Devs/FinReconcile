using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinReconcile.Models;

public class InternalTransaction
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ExternalReference { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}