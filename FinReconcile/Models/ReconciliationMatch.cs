using System.ComponentModel.DataAnnotations.Schema;

namespace FinReconcile.Models;

public class ReconciliationMatch
{
    public int Id { get; set; }

    public int InternalTransactionId { get; set; }
    public InternalTransaction InternalTransaction { get; set; } = null!;

    public int BankStatementId { get; set; }
    public BankStatement BankStatement { get; set; } = null!;

    public MatchType MatchType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DifferenceAmount { get; set; }

    public string? Note { get; set; }

    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
}