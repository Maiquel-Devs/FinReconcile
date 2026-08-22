using Microsoft.EntityFrameworkCore;
using FinReconcile.Models;

namespace FinReconcile.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<InternalTransaction> InternalTransactions => Set<InternalTransaction>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Índices para acelerar consultas frequentes por status e referência
        modelBuilder.Entity<InternalTransaction>()
            .HasIndex(t => t.ExternalReference);

        modelBuilder.Entity<BankStatement>()
            .HasIndex(b => b.OriginalReference);

        modelBuilder.Entity<BankStatement>()
            .HasIndex(b => b.BatchId);
    }
}