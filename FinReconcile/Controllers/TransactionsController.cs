using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;

namespace FinReconcile.Controllers;

/// <summary>
/// Gerencia a visualização e listagem das transações financeiras internas.
/// </summary>
public class TransactionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Exibe a lista geral de todas as transações internas registradas no sistema,
    /// ordenadas das mais recentes para as mais antigas.
    /// </summary>
    /// <returns>View contendo a listagem das transações.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var transactions = await _context.InternalTransactions
            .AsNoTracking() // Otimização: Evita overhead de memória no EF Core para listagens (somente leitura)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        return View(transactions);
    }
}