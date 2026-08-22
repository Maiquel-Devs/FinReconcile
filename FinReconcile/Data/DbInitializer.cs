using FinReconcile.Models;

namespace FinReconcile.Data;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // Se já existirem transações, não insere novamente
        if (context.InternalTransactions.Any())
        {
            return;
        }

        var transactions = new List<InternalTransaction>
        {
            new()
            {
                ExternalReference = "PIX-2026-001",
                Amount = 150.00m,
                TransactionDate = DateTime.UtcNow.AddDays(-2),
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "CARD-2026-002",
                Amount = 250.50m,
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "BOL-2026-003",
                Amount = 1200.00m,
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "PIX-2026-004",
                Amount = 89.90m,
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "CARD-2026-005",
                Amount = 500.00m,
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatus.Pending
            }
        };

        context.InternalTransactions.AddRange(transactions);
        context.SaveChanges();
    }
}