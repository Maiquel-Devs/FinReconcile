using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinReconcile.Data;

namespace FinReconcile.Controllers;

public class TransactionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var transactions = await _context.InternalTransactions
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        return View(transactions);
    }
}