using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.Models;
using FinReconcile.Services;

namespace FinReconcile.Controllers;

/// <summary>
/// Gerencia as operações de conciliação bancária, incluindo upload de extratos, 
/// visualização de divergências e conciliação manual.
/// </summary>
public class ReconciliationController : Controller
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ApplicationDbContext _context;

    public ReconciliationController(IReconciliationService reconciliationService, ApplicationDbContext context)
    {
        _reconciliationService = reconciliationService;
        _context = context;
    }

    /// <summary>
    /// Exibe o histórico de transações conciliadas.
    /// </summary>
    /// <returns>View contendo a lista de conciliações realizadas.</returns>
    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var matches = await _context.ReconciliationMatches
            .AsNoTracking() // Otimização: Evita overhead do EF Core em consultas de leitura
            .Include(m => m.InternalTransaction)
            .Include(m => m.BankStatement)
            .OrderByDescending(m => m.ReconciledAt)
            .ToListAsync();

        return View(matches);
    }

    /// <summary>
    /// Processa o upload de um arquivo de extrato bancário (formato CSV) e executa a conciliação automática.
    /// </summary>
    /// <param name="file">Arquivo CSV contendo o extrato bancário.</param>
    /// <returns>Redireciona para a tela de upload com mensagem de sucesso ou erro.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (!IsValidFile(file))
        {
            TempData["Error"] = "Por favor, selecione um arquivo CSV válido.";
            return RedirectToAction(nameof(Upload));
        }

        if (!IsCsvExtension(file!.FileName))
        {
            TempData["Error"] = "Extensão inválida. Apenas arquivos com extensão .csv são permitidos.";
            return RedirectToAction(nameof(Upload));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var batchId = await _reconciliationService.ProcessStatementCsvAsync(stream);
            await _reconciliationService.RunReconciliationAsync(batchId);

            TempData["Success"] = "Extrato importado e processado com sucesso!";
        }
        catch (Exception)
        {
            TempData["Error"] = "O arquivo enviado não possui o layout de colunas esperado ou contém dados inválidos.";
        }

        return RedirectToAction(nameof(Upload));
    }

    /// <summary>
    /// Exibe as transações internas e do extrato que estão pendentes ou divergentes.
    /// </summary>
    /// <returns>View contendo as transações não conciliadas.</returns>
    [HttpGet]
    public async Task<IActionResult> Divergences()
    {
        // Execução sequencial para respeitar a restrição de thread-safety da mesma instância de DbContext
        var unmatchedInternalTransactions = await _context.InternalTransactions
            .AsNoTracking()
            .Where(t => t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.Divergent)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var unmatchedBankStatements = await _context.BankStatements
            .AsNoTracking()
            .Where(b => b.Status == TransactionStatus.Pending || b.Status == TransactionStatus.Divergent)
            .OrderByDescending(b => b.StatementDate)
            .ToListAsync();

        var viewModel = new DivergenceViewModel
        {
            UnmatchedInternalTransactions = unmatchedInternalTransactions,
            UnmatchedBankStatements = unmatchedBankStatements
        };

        return View(viewModel);
    }

    /// <summary>
    /// Executa a conciliação manual entre uma transação interna e uma transação do extrato.
    /// </summary>
    /// <param name="internalTransactionId">Identificador da transação interna.</param>
    /// <param name="bankStatementId">Identificador da transação do extrato bancário.</param>
    /// <param name="note">Anotação opcional sobre a conciliação manual.</param>
    /// <returns>Redireciona para a tela de divergências com o resultado da operação.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualReconcile(int internalTransactionId, int bankStatementId, string note)
    {
        try
        {
            await _reconciliationService.ManualMatchAsync(internalTransactionId, bankStatementId, note);
            TempData["Success"] = "Conciliação manual realizada com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao conciliar manualmente: {ex.Message}";
        }

        return RedirectToAction(nameof(Divergences));
    }

    #region Private Validation Methods

    private static bool IsValidFile(IFormFile? file) => file != null && file.Length > 0;

    private static bool IsCsvExtension(string fileName) 
        => Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    #endregion
}