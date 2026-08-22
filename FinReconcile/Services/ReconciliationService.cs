using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.DTOs;
using FinReconcile.Models;
using MatchType = FinReconcile.Models.MatchType;

namespace FinReconcile.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _context;
    private const decimal TOLERANCE_MARGIN = 0.05m; // Tolerância de até 5 centavos de divergência

    public ReconciliationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> ProcessStatementCsvAsync(Stream csvStream)
    {
        var batchId = Guid.NewGuid();
        var statements = new List<BankStatement>();

        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<BankStatementCsvRecord>();

        foreach (var record in records)
        {
            var netAmount = record.Amount - record.Fee;
            statements.Add(new BankStatement
            {
                BatchId = batchId,
                OriginalReference = record.Reference,
                Amount = record.Amount,
                FeeAmount = record.Fee,
                NetAmount = netAmount,
                StatementDate = record.Date,
                Status = TransactionStatus.Pending
            });
        }

        await _context.BankStatements.AddRangeAsync(statements);
        await _context.SaveChangesAsync();

        return batchId;
    }

    public async Task RunReconciliationAsync(Guid batchId)
    {
        var statements = await _context.BankStatements
            .Where(b => b.BatchId == batchId && b.Status == TransactionStatus.Pending)
            .ToListAsync();

        var pendingTransactions = await _context.InternalTransactions
            .Where(t => t.Status == TransactionStatus.Pending)
            .ToListAsync();

        var matches = new List<ReconciliationMatch>();

        foreach (var statement in statements)
        {
            // 1. Tenta Match Exato (Mesma Referência e Mesmo Valor Líquido)
            var exactMatch = pendingTransactions
                .FirstOrDefault(t => t.ExternalReference.Equals(statement.OriginalReference, StringComparison.OrdinalIgnoreCase)
                                     && t.Amount == statement.NetAmount);

            if (exactMatch != null)
            {
                statement.Status = TransactionStatus.Reconciled;
                exactMatch.Status = TransactionStatus.Reconciled;

                matches.Add(new ReconciliationMatch
                {
                    InternalTransactionId = exactMatch.Id,
                    BankStatementId = statement.Id,
                    MatchType = MatchType.Exact,
                    DifferenceAmount = 0.00m,
                    Note = "Match exato por referência e valor líquido."
                });

                pendingTransactions.Remove(exactMatch);
                continue;
            }

            // 2. Tenta Match por Tolerância (Mesma Referência, com divergência de até 5 centavos)
            var toleranceMatch = pendingTransactions
                .FirstOrDefault(t => t.ExternalReference.Equals(statement.OriginalReference, StringComparison.OrdinalIgnoreCase)
                                     && Math.Abs(t.Amount - statement.NetAmount) <= TOLERANCE_MARGIN);

            if (toleranceMatch != null)
            {
                var diff = Math.Abs(toleranceMatch.Amount - statement.NetAmount);
                statement.Status = TransactionStatus.Reconciled;
                toleranceMatch.Status = TransactionStatus.Reconciled;

                matches.Add(new ReconciliationMatch
                {
                    InternalTransactionId = toleranceMatch.Id,
                    BankStatementId = statement.Id,
                    MatchType = MatchType.Tolerance,
                    DifferenceAmount = diff,
                    Note = $"Conciliado com tolerância aceita de {diff:C}."
                });

                pendingTransactions.Remove(toleranceMatch);
                continue;
            }

            // 3. Se não encontrou no sistema interno, marca como divergente
            statement.Status = TransactionStatus.Divergent;
        }

        await _context.ReconciliationMatches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();
    }

    public async Task ManualMatchAsync(int internalTransactionId, int bankStatementId, string note)
    {
        var internalTx = await _context.InternalTransactions.FindAsync(internalTransactionId);
        var statement = await _context.BankStatements.FindAsync(bankStatementId);

        if (internalTx == null || statement == null)
        {
            throw new InvalidOperationException("Transação ou extrato não encontrado.");
        }

        internalTx.Status = TransactionStatus.Reconciled;
        statement.Status = TransactionStatus.Reconciled;

        var diff = Math.Abs(internalTx.Amount - statement.NetAmount);

        var match = new ReconciliationMatch
        {
            InternalTransactionId = internalTx.Id,
            BankStatementId = statement.Id,
            MatchType = MatchType.Manual,
            DifferenceAmount = diff,
            Note = string.IsNullOrWhiteSpace(note) ? "Conciliação manual forçada pelo operador." : note
        };

        await _context.ReconciliationMatches.AddAsync(match);
        await _context.SaveChangesAsync();
    }
}