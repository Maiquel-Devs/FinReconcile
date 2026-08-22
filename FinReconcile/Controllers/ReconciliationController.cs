using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;
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
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Por favor, selecione um arquivo CSV válido.";
            return RedirectToAction(nameof(Upload));
        }

        using var stream = file.OpenReadStream();
        var batchId = await _reconciliationService.ProcessStatementCsvAsync(stream);
        await _reconciliationService.RunReconciliationAsync(batchId);

        TempData["Success"] = "Extrato importado e processado com sucesso!";
        return RedirectToAction(nameof(Upload));
    }
}