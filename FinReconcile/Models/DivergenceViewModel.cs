namespace FinReconcile.Models;

public class DivergenceViewModel
{
    public List<InternalTransaction> UnmatchedInternalTransactions { get; set; } = new();
    public List<BankStatement> UnmatchedBankStatements { get; set; } = new();
}