using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.Models;
using FinReconcile.Services;
using Xunit;
using MatchType = FinReconcile.Models.MatchType;

namespace FinReconcile.UnitTests;

public class ReconciliationServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Garante isolamento por teste
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RunReconciliation_ShouldReconcile_WhenExactMatchExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var batchId = Guid.NewGuid();

        var internalTx = new InternalTransaction
        {
            Id = 1,
            ExternalReference = "PIX-100",
            Amount = 150.00m,
            Status = TransactionStatus.Pending
        };

        var statement = new BankStatement
        {
            Id = 1,
            BatchId = batchId,
            OriginalReference = "PIX-100",
            Amount = 150.00m,
            FeeAmount = 0.00m,
            NetAmount = 150.00m,
            Status = TransactionStatus.Pending
        };

        await context.InternalTransactions.AddAsync(internalTx);
        await context.BankStatements.AddAsync(statement);
        await context.SaveChangesAsync();

        var service = new ReconciliationService(context);

        // Act
        await service.RunReconciliationAsync(batchId);

        // Assert
        var updatedTx = await context.InternalTransactions.FindAsync(1);
        var updatedStatement = await context.BankStatements.FindAsync(1);
        var match = await context.ReconciliationMatches.FirstOrDefaultAsync();

        updatedTx!.Status.Should().Be(TransactionStatus.Reconciled);
        updatedStatement!.Status.Should().Be(TransactionStatus.Reconciled);
        match.Should().NotBeNull();
        match!.MatchType.Should().Be(MatchType.Exact);
        match.DifferenceAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task RunReconciliation_ShouldReconcile_WhenToleranceDifferenceIsWithin5Cents()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var batchId = Guid.NewGuid();

        var internalTx = new InternalTransaction
        {
            Id = 2,
            ExternalReference = "BOL-200",
            Amount = 100.00m,
            Status = TransactionStatus.Pending
        };

        var statement = new BankStatement
        {
            Id = 2,
            BatchId = batchId,
            OriginalReference = "BOL-200",
            Amount = 103.00m,
            FeeAmount = 3.03m, // Líquido = 99.97 (diferença de 0.03 <= 0.05)
            NetAmount = 99.97m,
            Status = TransactionStatus.Pending
        };

        await context.InternalTransactions.AddAsync(internalTx);
        await context.BankStatements.AddAsync(statement);
        await context.SaveChangesAsync();

        var service = new ReconciliationService(context);

        // Act
        await service.RunReconciliationAsync(batchId);

        // Assert
        var match = await context.ReconciliationMatches.FirstOrDefaultAsync();
        match.Should().NotBeNull();
        match!.MatchType.Should().Be(MatchType.Tolerance);
        match.DifferenceAmount.Should().Be(0.03m);
    }

    [Fact]
    public async Task RunReconciliation_ShouldMarkAsDivergent_WhenNoMatchingTransactionFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var batchId = Guid.NewGuid();

        var statement = new BankStatement
        {
            Id = 3,
            BatchId = batchId,
            OriginalReference = "DOC-NAO-EXISTENTE",
            Amount = 500.00m,
            NetAmount = 500.00m,
            Status = TransactionStatus.Pending
        };

        await context.BankStatements.AddAsync(statement);
        await context.SaveChangesAsync();

        var service = new ReconciliationService(context);

        // Act
        await service.RunReconciliationAsync(batchId);

        // Assert
        var updatedStatement = await context.BankStatements.FindAsync(3);
        var matchCount = await context.ReconciliationMatches.CountAsync();

        updatedStatement!.Status.Should().Be(TransactionStatus.Divergent);
        matchCount.Should().Be(0);
    }

    [Fact]
    public async Task ManualMatch_ShouldForceReconciliation_WithCustomNote()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        var internalTx = new InternalTransaction
        {
            Id = 10,
            ExternalReference = "TX-CUSTOM",
            Amount = 300.00m,
            Status = TransactionStatus.Pending
        };

        var statement = new BankStatement
        {
            Id = 20,
            OriginalReference = "ST-CUSTOM",
            Amount = 310.00m,
            NetAmount = 310.00m,
            Status = TransactionStatus.Divergent
        };

        await context.InternalTransactions.AddAsync(internalTx);
        await context.BankStatements.AddAsync(statement);
        await context.SaveChangesAsync();

        var service = new ReconciliationService(context);

        // Act
        await service.ManualMatchAsync(10, 20, "Aprovado pelo gestor financeiro.");

        // Assert
        var updatedTx = await context.InternalTransactions.FindAsync(10);
        var updatedStatement = await context.BankStatements.FindAsync(20);
        var match = await context.ReconciliationMatches.FirstOrDefaultAsync();

        updatedTx!.Status.Should().Be(TransactionStatus.Reconciled);
        updatedStatement!.Status.Should().Be(TransactionStatus.Reconciled);
        match!.MatchType.Should().Be(MatchType.Manual);
        match.Note.Should().Be("Aprovado pelo gestor financeiro.");
    }
}