namespace FinReconcile.Models;

public enum TransactionStatus
{
    Pending = 0,
    Reconciled = 1,
    Divergent = 2
}

public enum MatchType
{
    Exact = 1,
    Tolerance = 2,
    Manual = 3
}