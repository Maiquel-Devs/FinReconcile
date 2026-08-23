using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
using FinReconcile.Models;
using FinReconcile.Services;

namespace FinReconcile.Controllers;

public class ReconciliationController : Controller
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ApplicationDbContext _context;

    public ReconciliationController(IReconciliationService reconciliationService, ApplicationDbContext context)
    {
        _reconciliationService = reconciliationService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var matches = await _context.ReconciliationMatches
            .Include(m => m.InternalTransaction)
            .Include(m => m.BankStatement)
            .OrderByDescending(m => m.ReconciledAt)
            .ToListAsync();

        return View(matches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        // 1. Validação de presença e tamanho
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Por favor, selecione um arquivo CSV válido.";
            return RedirectToAction(nameof(Upload));
        }

        // 2. Validação estrita de extensão (.csv apenas)
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".csv")
        {
            TempData["Error"] = "Extensão inválida. Apenas arquivos com extensão .csv são permitidos.";
            return RedirectToAction(nameof(Upload));
        }

        // 3. Processamento seguro sem vazamento de Stack Trace
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

    [HttpGet]
    public async Task<IActionResult> Divergences()
    {
        var unmatchedTx = await _context.InternalTransactions
            .Where(t => t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.Divergent)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var unmatchedStatements = await _context.BankStatements
            .Where(b => b.Status == TransactionStatus.Pending || b.Status == TransactionStatus.Divergent)
            .OrderByDescending(b => b.StatementDate)
            .ToListAsync();

        var viewModel = new DivergenceViewModel
        {
            UnmatchedInternalTransactions = unmatchedTx,
            UnmatchedBankStatements = unmatchedStatements
        };

        return View(viewModel);
    }

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
}