using Microsoft.EntityFrameworkCore;
using FinReconcile.Models;

namespace FinReconcile.Data;

/// <summary>
/// Contexto de banco de dados principal da aplicação, responsável pelo mapeamento 
/// das entidades de conciliação financeira utilizando o Entity Framework Core.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Tabela que armazena os lançamentos financeiros internos do sistema (Livro-Razão).
    /// </summary>
    public DbSet<InternalTransaction> InternalTransactions => Set<InternalTransaction>();
    
    /// <summary>
    /// Tabela que armazena os registros importados a partir de extratos bancários (CSV).
    /// </summary>
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    
    /// <summary>
    /// Tabela de resolução que registra os vínculos efetivados entre transações internas e bancárias.
    /// </summary>
    public DbSet<ReconciliationMatch> ReconciliationMatches => Set<ReconciliationMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Agrupamento de configurações para Transações Internas
        modelBuilder.Entity<InternalTransaction>(entity =>
        {
            entity.HasIndex(t => t.ExternalReference)
                  .HasDatabaseName("IX_InternalTransactions_ExternalReference");
        });

        // Agrupamento de configurações para Extratos Bancários
        modelBuilder.Entity<BankStatement>(entity =>
        {
            entity.HasIndex(b => b.OriginalReference)
                  .HasDatabaseName("IX_BankStatements_OriginalReference");
                  
            entity.HasIndex(b => b.BatchId)
                  .HasDatabaseName("IX_BankStatements_BatchId");
        });
    }
}