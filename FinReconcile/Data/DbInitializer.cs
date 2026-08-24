using FinReconcile.Models;

namespace FinReconcile.Data;

/// <summary>
/// Responsável por popular o banco de dados com dados iniciais (Seed) 
/// para fins de demonstração, desenvolvimento ou testes.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Verifica se o banco de dados está vazio e, caso positivo, insere um lote 
    /// inicial de transações internas para simulação.
    /// </summary>
    /// <param name="context">Contexto de banco de dados da aplicação.</param>
    public static void Seed(ApplicationDbContext context)
    {
        if (context.InternalTransactions.Any())
            return;

        var transactions = GetInitialTransactions();

        context.InternalTransactions.AddRange(transactions);
        context.SaveChanges();
    }

    #region Private Methods

    private static InternalTransaction[] GetInitialTransactions()
    {
        var now = DateTime.UtcNow;

        return new InternalTransaction[]
        {
            new()
            {
                ExternalReference = "PIX-2026-001",
                Amount = 150.00m,
                TransactionDate = now.AddDays(-2),
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "CARD-2026-002",
                Amount = 250.50m,
                TransactionDate = now.AddDays(-1),
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "BOL-2026-003",
                Amount = 1200.00m,
                TransactionDate = now.AddDays(-1),
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "PIX-2026-004",
                Amount = 89.90m,
                TransactionDate = now,
                Status = TransactionStatus.Pending
            },
            new()
            {
                ExternalReference = "CARD-2026-005",
                Amount = 500.00m,
                TransactionDate = now,
                Status = TransactionStatus.Pending
            }
        };
    }

    #endregion
}