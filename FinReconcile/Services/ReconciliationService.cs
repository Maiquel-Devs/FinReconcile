using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.DTOs;
using FinReconcile.Models;
using MatchType = FinReconcile.Models.MatchType;

namespace FinReconcile.Services;

/// <summary>
/// Implementação do serviço de conciliação bancária. Responsável por processar arquivos,
/// aplicar regras de correspondência automáticas e efetivar conciliações manuais.
/// </summary>
public class ReconciliationService : IReconciliationService
{
    private readonly ApplicationDbContext _context;
    private const decimal TOLERANCE_MARGIN = 0.05m;

    public ReconciliationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Guid> ProcessStatementCsvAsync(Stream csvStream)
    {
        var batchId = Guid.NewGuid();
        
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);
        
        var records = csv.GetRecords<BankStatementCsvRecord>();

        // Mapeamento declarativo utilizando LINQ (substitui o antigo loop foreach)
        var statements = records.Select(record => new BankStatement
        {
            BatchId = batchId,
            OriginalReference = record.Reference,
            Amount = record.Amount,
            FeeAmount = record.Fee,
            NetAmount = record.Amount - record.Fee,
            StatementDate = record.Date,
            Status = TransactionStatus.Pending
        }).ToList();

        await _context.BankStatements.AddRangeAsync(statements);
        await _context.SaveChangesAsync();

        return batchId;
    }

    /// <inheritdoc />
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
            // 1. Tenta conciliar com base na regra exata
            if (TryProcessExactMatch(statement, pendingTransactions, matches))
                continue;

            // 2. Tenta conciliar aceitando divergência de centavos
            if (TryProcessToleranceMatch(statement, pendingTransactions, matches))
                continue;

            // 3. Se nenhuma regra for atendida, sinaliza para auditoria manual
            statement.Status = TransactionStatus.Divergent;
        }

        await _context.ReconciliationMatches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ManualMatchAsync(int internalTransactionId, int bankStatementId, string note)
    {
        var internalTx = await _context.InternalTransactions.FindAsync(internalTransactionId);
        var statement = await _context.BankStatements.FindAsync(bankStatementId);

        if (internalTx == null || statement == null)
        {
            throw new InvalidOperationException("Transação interna ou registro do extrato não localizados para conciliação.");
        }

        internalTx.Status = TransactionStatus.Reconciled;
        statement.Status = TransactionStatus.Reconciled;

        var match = CreateManualMatch(internalTx, statement, note);

        await _context.ReconciliationMatches.AddAsync(match);
        await _context.SaveChangesAsync();
    }

    #region Private Match Handlers

    private static bool TryProcessExactMatch(BankStatement statement, List<InternalTransaction> pendingTransactions, List<ReconciliationMatch> matches)
    {
        var exactMatchTx = pendingTransactions.FirstOrDefault(t => 
            t.ExternalReference.Equals(statement.OriginalReference, StringComparison.OrdinalIgnoreCase) && 
            t.Amount == statement.NetAmount);

        if (exactMatchTx == null) 
            return false;

        statement.Status = TransactionStatus.Reconciled;
        exactMatchTx.Status = TransactionStatus.Reconciled;

        matches.Add(new ReconciliationMatch
        {
            InternalTransactionId = exactMatchTx.Id,
            BankStatementId = statement.Id,
            MatchType = MatchType.Exact,
            DifferenceAmount = 0.00m,
            Note = "Match exato por referência e valor líquido."
        });

        pendingTransactions.Remove(exactMatchTx);
        return true;
    }

    private static bool TryProcessToleranceMatch(BankStatement statement, List<InternalTransaction> pendingTransactions, List<ReconciliationMatch> matches)
    {
        var toleranceMatchTx = pendingTransactions.FirstOrDefault(t => 
            t.ExternalReference.Equals(statement.OriginalReference, StringComparison.OrdinalIgnoreCase) && 
            Math.Abs(t.Amount - statement.NetAmount) <= TOLERANCE_MARGIN);

        if (toleranceMatchTx == null) 
            return false;

        var diff = Math.Abs(toleranceMatchTx.Amount - statement.NetAmount);
        
        statement.Status = TransactionStatus.Reconciled;
        toleranceMatchTx.Status = TransactionStatus.Reconciled;

        matches.Add(new ReconciliationMatch
        {
            InternalTransactionId = toleranceMatchTx.Id,
            BankStatementId = statement.Id,
            MatchType = MatchType.Tolerance,
            DifferenceAmount = diff,
            Note = $"Conciliado com tolerância aceita de {diff:C}."
        });

        pendingTransactions.Remove(toleranceMatchTx);
        return true;
    }

    private static ReconciliationMatch CreateManualMatch(InternalTransaction internalTx, BankStatement statement, string note)
    {
        var diff = Math.Abs(internalTx.Amount - statement.NetAmount);

        return new ReconciliationMatch
        {
            InternalTransactionId = internalTx.Id,
            BankStatementId = statement.Id,
            MatchType = MatchType.Manual,
            DifferenceAmount = diff,
            Note = string.IsNullOrWhiteSpace(note) ? "Conciliação manual forçada pelo operador." : note
        };
    }

    #endregion
}